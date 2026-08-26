using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UGF.GameFramework.Data;
using UnityEditor.UIElements;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 表结构（供关系编辑器使用）
    /// </summary>
    public class TableSchema
    {
        public string TableName;
        public List<ExcelFieldInfo> Fields = new List<ExcelFieldInfo>();

        public ExcelFieldInfo GetPrimaryKey()
        {
            return Fields.Find(f => f.IsPrimaryKey);
        }

        public ExcelFieldInfo GetField(string name)
        {
            return Fields.Find(f => f.Name == name);
        }
    }

    /// <summary>
    /// 端口关联的字段信息
    /// </summary>
    public class PortInfo
    {
        public string TableName;
        public ExcelFieldInfo Field;

        public PortInfo(string tableName, ExcelFieldInfo field)
        {
            TableName = tableName;
            Field = field;
        }
    }

    /// <summary>
    /// 关系节点视图（一张数据表）
    /// </summary>
    public class TableNodeView : Node
    {
        public TableSchema Schema;
        public readonly Dictionary<string, Port> InputPorts = new Dictionary<string, Port>();
        public readonly Dictionary<string, Port> OutputPorts = new Dictionary<string, Port>();

        public TableNodeView(TableSchema schema)
        {
            Schema = schema;
            title = schema.TableName;
            capabilities |= Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable | Capabilities.Collapsible;

            foreach (var field in schema.Fields)
            {
                var label = field.IsPrimaryKey ? $"{field.Name} [PK]" : field.Name;

                var input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(object));
                input.portName = label;
                input.userData = new PortInfo(schema.TableName, field);
                inputContainer.Add(input);
                InputPorts[field.Name] = input;

                var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(object));
                output.portName = label;
                output.userData = new PortInfo(schema.TableName, field);
                outputContainer.Add(output);
                OutputPorts[field.Name] = output;
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        public Port GetInputPort(string fieldName)
        {
            return InputPorts.TryGetValue(fieldName, out var port) ? port : null;
        }

        public Port GetOutputPort(string fieldName)
        {
            return OutputPorts.TryGetValue(fieldName, out var port) ? port : null;
        }
    }

    /// <summary>
    /// 表关系图视图
    /// </summary>
    public class RelationGraphView : GraphView
    {
        public Action<Edge> OnRelationCreated;
        public Action<Edge> OnRelationRemoved;

        public RelationGraphView()
        {
            style.flexGrow = 1;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            Insert(0, new GridBackground());

            graphViewChanged = OnGraphViewChanged;
        }

        /// <summary>
        /// 端口兼容性过滤：目标端口需与起始端口方向相反（input/output 互补）。
        /// 覆盖 GraphView 的虚方法，避免默认空实现导致连线被 EdgeConnector 拒绝。
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();
            if (startPort == null)
                return result;

            foreach (var port in ports)
            {
                if (port == startPort)
                    continue;
                if (port.direction == startPort.direction)
                    continue;
                result.Add(port);
            }
            return result;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            if (changes.edgesToCreate != null && changes.edgesToCreate.Count > 0)
            {
                Debug.Log($"[RelationEditor] 检测到 {changes.edgesToCreate.Count} 条连线创建");
                var validEdges = new List<Edge>();
                foreach (var edge in changes.edgesToCreate)
                {
                    if (edge != null && ValidateConnection(edge))
                    {
                        validEdges.Add(edge);
                        OnRelationCreated?.Invoke(edge);
                    }
                    else
                    {
                        // 非法连接：从本次创建中过滤掉，不会加入画布
                        Debug.LogWarning($"关系连线被拒绝：{DescribeEdge(edge)}");
                    }
                }
                changes.edgesToCreate = validEdges;
            }

            if (changes.elementsToRemove != null)
            {
                foreach (var element in changes.elementsToRemove.ToList())
                {
                    if (element is Edge removedEdge)
                    {
                        OnRelationRemoved?.Invoke(removedEdge);
                    }
                }
            }

            return changes;
        }

        private bool ValidateConnection(Edge edge)
        {
            var outputPort = edge.output as Port;
            var inputPort = edge.input as Port;
            if (outputPort == null || inputPort == null)
                return false;

            var outInfo = outputPort.userData as PortInfo;
            var inInfo = inputPort.userData as PortInfo;
            if (outInfo == null || inInfo == null)
                return false;

            if (string.Equals(outInfo.TableName, inInfo.TableName, StringComparison.Ordinal))
                return false;

            if (!string.Equals(outInfo.Field.Type, inInfo.Field.Type, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"字段类型不一致: {outInfo.TableName}.{outInfo.Field.Name}({outInfo.Field.Type}) -> {inInfo.TableName}.{inInfo.Field.Name}({inInfo.Field.Type})");
                return false;
            }

            return true;
        }

        private string DescribeEdge(Edge edge)
        {
            if (edge == null)
                return "null edge";
            var outPort = edge.output as Port;
            var inPort = edge.input as Port;
            var outInfo = outPort?.userData as PortInfo;
            var inInfo = inPort?.userData as PortInfo;
            return $"output={outInfo?.TableName}.{outInfo?.Field.Name} input={inInfo?.TableName}.{inInfo?.Field.Name}";
        }
    }

    /// <summary>
    /// 表关系可视化编辑器窗口
    /// </summary>
    public class RelationGraphWindow : EditorWindow
    {
        private const string DefaultRelationDirectory = "Assets/StreamingAssets/DataRelations";
        private const string DefaultRelationFileName = "relations.json";

        private RelationGraphView m_GraphView;
        private TableRelationSet m_RelationSet;
        private string m_RelationSetPath = string.Empty;
        private Dictionary<string, TableSchema> m_Schemas = new Dictionary<string, TableSchema>(StringComparer.Ordinal);
        private IMGUIContainer m_InspectorContainer;
        private IMGUIContainer m_StatusContainer;
        private string m_StatusMessage = string.Empty;

        [MenuItem("UGF/GameFramework/数据表关系编辑器")]
        public static void Open()
        {
            var window = GetWindow<RelationGraphWindow>("数据表关系编辑器");
            window.minSize = new Vector2(900, 600);
        }

        private void OnEnable()
        {
            m_RelationSet = null;
            m_RelationSetPath = string.Empty;
            m_Schemas.Clear();
            m_StatusMessage = "从工具栏「扫描Excel目录」加载数据表节点。";

            BuildUi();
        }

        private void OnDisable()
        {
            m_GraphView = null;
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();

            // 工具栏
            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(ScanExcelDirectory) { text = "扫描Excel目录" });
            toolbar.Add(new ToolbarButton(AddNodeManually) { text = "添加节点" });
            toolbar.Add(new ToolbarButton(ValidateRelations) { text = "校验" });
            toolbar.Add(new ToolbarButton(AutoLayout) { text = "自动布局" });
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarButton(SaveAsset) { text = "保存资产" });
            toolbar.Add(new ToolbarButton(LoadAsset) { text = "加载资产" });
            toolbar.Add(new ToolbarButton(LoadConfig) { text = "加载配置" });
            toolbar.Add(new ToolbarButton(ExportConfig) { text = "导出配置" });
            toolbar.Add(new ToolbarButton(GenerateCode) { text = "生成代码" });
            rootVisualElement.Add(toolbar);

            // 主区域：GraphView + Inspector 侧栏
            var body = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };

            m_GraphView = new RelationGraphView();
            m_GraphView.OnRelationCreated += OnRelationCreated;
            m_GraphView.OnRelationRemoved += OnRelationRemoved;
            body.Add(m_GraphView);

            var inspector = new VisualElement { style = { width = 300, borderLeftWidth = 1, borderLeftColor = new Color(0.2f, 0.2f, 0.2f) } };
            m_InspectorContainer = new IMGUIContainer(OnInspectorGui);
            inspector.Add(m_InspectorContainer);
            body.Add(inspector);

            rootVisualElement.Add(body);

            // 底部状态栏
            m_StatusContainer = new IMGUIContainer(OnStatusGui);
            rootVisualElement.Add(m_StatusContainer);
        }

        // ==================== 工具栏操作 ====================

        private void ScanExcelDirectory()
        {
            var settings = DataTableBuilderSettings.Instance;
            var directory = settings != null ? settings.ExcelDirectory : string.Empty;
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                directory = EditorUtility.OpenFolderPanel("选择Excel目录", "Assets", "");
                if (string.IsNullOrEmpty(directory))
                    return;
            }

            var files = Directory.GetFiles(directory, "*.xlsx", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith("~$", StringComparison.Ordinal))
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
                .ToList();

            if (files.Count == 0)
            {
                m_StatusMessage = $"目录中未找到Excel文件: {directory}";
                return;
            }

            ClearGraph();
            m_Schemas.Clear();

            int index = 0;
            const int columns = 4;
            float spacingX = 320;
            float spacingY = 220;
            foreach (var file in files)
            {
                var tableName = Path.GetFileNameWithoutExtension(file);
                var fields = ExcelParser.PreviewFields(file);
                if (fields.Count == 0)
                    continue;

                var schema = new TableSchema { TableName = tableName };
                schema.Fields.AddRange(fields);
                m_Schemas[tableName] = schema;

                var node = new TableNodeView(schema);
                node.SetPosition(new Rect((index % columns) * spacingX, (index / columns) * spacingY, 0, 0));
                m_GraphView.AddElement(node);
                index++;
            }

            EnsureRelationSet();
            SyncNodesToRelationSet();
            m_StatusMessage = $"已加载 {index} 张表: {directory}";
        }

        private void AddNodeManually()
        {
            var tableName = EditorInputDialog.Show("添加节点", "表名（Excel 文件名）:", "NewTable");
            if (string.IsNullOrEmpty(tableName))
                return;

            var fields = ExcelParser.PreviewFields(FindExcelFile(tableName));
            if (fields.Count == 0)
            {
                m_StatusMessage = $"未找到表 {tableName} 或表头为空";
                return;
            }

            var schema = new TableSchema { TableName = tableName };
            schema.Fields.AddRange(fields);
            m_Schemas[tableName] = schema;

            var node = new TableNodeView(schema);
            node.SetPosition(new Rect(100 + m_Schemas.Count * 30, 100 + m_Schemas.Count * 30, 0, 0));
            m_GraphView.AddElement(node);

            EnsureRelationSet();
            SyncNodesToRelationSet();
        }

        private string FindExcelFile(string tableName)
        {
            var settings = DataTableBuilderSettings.Instance;
            if (settings != null && Directory.Exists(settings.ExcelDirectory))
            {
                var files = Directory.GetFiles(settings.ExcelDirectory, "*.xlsx", SearchOption.AllDirectories);
                var match = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == tableName);
                if (match != null)
                    return match;
            }
            return tableName + ".xlsx";
        }

        private void ValidateRelations()
        {
            if (m_RelationSet == null)
            {
                m_StatusMessage = "尚未加载关系集";
                return;
            }

            var errors = new List<string>();
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (var relation in m_RelationSet.Relations)
            {
                if (string.IsNullOrEmpty(relation.Name))
                    errors.Add($"存在未命名关系（{relation.SourceTable} -> {relation.TargetTable}）");
                else if (!names.Add(relation.Name))
                    errors.Add($"关系名重复: {relation.Name}");
                else if (m_RelationSet.HasRelationName(relation.Name, relation.Name))
                    errors.Add($"关系名冲突: {relation.Name}");

                if (!m_Schemas.TryGetValue(relation.SourceTable, out var srcSchema))
                {
                    errors.Add($"关系 {relation.Name}: 源表不存在 {relation.SourceTable}");
                }
                else if (!string.IsNullOrEmpty(relation.SourceField) && srcSchema.GetField(relation.SourceField) == null)
                {
                    errors.Add($"关系 {relation.Name}: 源字段不存在 {relation.SourceTable}.{relation.SourceField}");
                }

                if (!m_Schemas.TryGetValue(relation.TargetTable, out var tgtSchema))
                {
                    errors.Add($"关系 {relation.Name}: 目标表不存在 {relation.TargetTable}");
                }
                else if (!string.IsNullOrEmpty(relation.TargetField) && tgtSchema.GetField(relation.TargetField) == null)
                {
                    errors.Add($"关系 {relation.Name}: 目标字段不存在 {relation.TargetTable}.{relation.TargetField}");
                }

                if (relation.RelationType == RelationType.ManyToMany)
                {
                    if (string.IsNullOrEmpty(relation.JoinTable) || !m_Schemas.ContainsKey(relation.JoinTable))
                        errors.Add($"关系 {relation.Name}: 多对多需要有效的中间表");
                }
            }

            m_StatusMessage = errors.Count == 0
                ? $"校验通过：共 {m_RelationSet.Relations.Count} 条关系"
                : "校验发现 " + errors.Count + " 个问题:\n" + string.Join("\n", errors.Take(20));
        }

        private void AutoLayout()
        {
            int index = 0;
            const int columns = 4;
            foreach (var element in m_GraphView.nodes.ToList())
            {
                if (element is TableNodeView node)
                {
                    node.SetPosition(new Rect((index % columns) * 320, (index / columns) * 220, 0, 0));
                    index++;
                }
            }
            SyncNodesToRelationSet();
        }

        private void SaveAsset()
        {
            if (m_RelationSet == null)
            {
                EditorUtility.DisplayDialog("保存关系", "请先扫描Excel目录创建关系集", "确定");
                return;
            }

            if (string.IsNullOrEmpty(m_RelationSetPath))
            {
                var path = EditorUtility.SaveFilePanelInProject("保存表关系集", "TableRelationSet", "asset", "保存表关系集资产");
                if (string.IsNullOrEmpty(path))
                    return;
                m_RelationSetPath = path;
            }

            AssetDatabase.CreateAsset(m_RelationSet, m_RelationSetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            m_StatusMessage = $"已保存资产: {m_RelationSetPath}";
        }

        private void LoadAsset()
        {
            var path = EditorUtility.OpenFilePanel("加载表关系集", "Assets", "asset");
            if (string.IsNullOrEmpty(path))
                return;

            var relativePath = path.StartsWith(Application.dataPath)
                ? "Assets" + path.Substring(Application.dataPath.Length)
                : path;

            var asset = AssetDatabase.LoadAssetAtPath<TableRelationSet>(relativePath);
            if (asset == null)
            {
                m_StatusMessage = $"加载失败（不是有效的表关系集）: {relativePath}";
                return;
            }

            ClearGraph();
            m_RelationSet = asset;
            m_RelationSetPath = relativePath;

            // 重建节点与连线
            foreach (var nodeInfo in m_RelationSet.Nodes)
            {
                if (m_Schemas.TryGetValue(nodeInfo.TableName, out var schema))
                {
                    var node = new TableNodeView(schema);
                    node.SetPosition(new Rect(nodeInfo.Position, Vector2.zero));
                    m_GraphView.AddElement(node);
                }
            }

            foreach (var relation in m_RelationSet.Relations)
            {
                CreateEdgeForRelation(relation);
            }

            m_StatusMessage = $"已加载资产: {relativePath}";
        }

        /// <summary>
        /// 加载运行时关系配置（relations.json），重建关系集与画布
        /// </summary>
        private void LoadConfig()
        {
            var settings = DataTableBuilderSettings.Instance;
            var directory = settings != null && !string.IsNullOrEmpty(settings.RelationOutputDirectory)
                ? settings.RelationOutputDirectory
                : DefaultRelationDirectory;
            var path = Path.Combine(directory, DefaultRelationFileName);

            if (!File.Exists(path))
            {
                var selected = EditorUtility.OpenFilePanel("加载关系配置 JSON", directory, "json");
                if (string.IsNullOrEmpty(selected))
                    return;
                path = selected;
            }

            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var config = TableRelationConfig.FromJson(json);
            if (config == null || config.relations.Count == 0)
            {
                m_StatusMessage = $"配置为空或无有效关系: {path}";
                return;
            }

            if (m_Schemas.Count == 0)
            {
                m_StatusMessage = "请先「扫描Excel目录」加载表结构，再加载关系配置。";
                return;
            }

            // 构建关系集（JSON 无布局信息，节点使用自动布局）
            var set = CreateInstance<TableRelationSet>();
            set.Relations.AddRange(config.relations);

            ClearGraph();
            m_RelationSet = set;
            m_RelationSetPath = string.Empty;

            // 收集关系涉及的表并创建节点
            var involvedTables = set.Relations
                .SelectMany(r => new[] { r.SourceTable, r.TargetTable, r.JoinTable })
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var tableName in involvedTables)
            {
                if (m_Schemas.TryGetValue(tableName, out var schema))
                {
                    m_GraphView.AddElement(new TableNodeView(schema));
                }
            }

            AutoLayout();

            // 重建连线（字段/表缺失的关系跳过并警告）
            int skipped = 0;
            foreach (var relation in set.Relations)
            {
                if (!CreateEdgeForRelation(relation))
                {
                    skipped++;
                    Debug.LogWarning($"关系 {relation.Name} 因表或字段缺失无法重建连线: {relation.SourceTable}.{relation.SourceField} -> {relation.TargetTable}.{relation.TargetField}");
                }
            }

            m_StatusMessage = skipped == 0
                ? $"已加载关系配置: {path}（{set.Relations.Count} 条关系）"
                : $"已加载关系配置: {path}（{set.Relations.Count} 条关系，{skipped} 条因表/字段缺失被跳过）";
        }

        private void ExportConfig()
        {
            if (m_RelationSet == null)
            {
                EditorUtility.DisplayDialog("导出配置", "请先扫描Excel目录创建关系集", "确定");
                return;
            }

            SyncRelationsFromGraph();

            var json = TableRelationConfig.FromRelationSet(m_RelationSet).ToJson();
            var settings = DataTableBuilderSettings.Instance;
            var directory = settings != null && !string.IsNullOrEmpty(settings.RelationOutputDirectory)
                ? settings.RelationOutputDirectory
                : DefaultRelationDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, DefaultRelationFileName);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();

            m_StatusMessage = $"已导出运行时配置: {path}";
        }

        private void GenerateCode()
        {
            if (m_RelationSet == null)
            {
                EditorUtility.DisplayDialog("生成代码", "请先扫描Excel目录创建关系集", "确定");
                return;
            }

            SyncRelationsFromGraph();

            var settings = DataTableBuilderSettings.Instance;
            var ns = settings != null ? settings.Namespace : "GameData";
            var output = settings != null ? settings.CodeOutputDirectory : "Assets/Scripts/Generated";

            if (DataRelationCodeGenerator.GenerateRelationExtension(m_RelationSet, ns, output))
            {
                AssetDatabase.Refresh();
                m_StatusMessage = $"已生成关系访问扩展代码: {output}/DataRelationExtension.g.cs";
            }
        }

        // ==================== 图形同步 ====================

        private void EnsureRelationSet()
        {
            if (m_RelationSet == null)
            {
                m_RelationSet = CreateInstance<TableRelationSet>();
                m_RelationSetPath = string.Empty;
            }
        }

        private void SyncNodesToRelationSet()
        {
            if (m_RelationSet == null)
                return;

            m_RelationSet.Nodes.Clear();
            foreach (var element in m_GraphView.nodes.ToList())
            {
                if (element is TableNodeView node)
                {
                    m_RelationSet.Nodes.Add(new RelationNode
                    {
                        TableName = node.Schema.TableName,
                        Position = node.GetPosition().position
                    });
                }
            }
        }

        // ==================== 关系创建 / 删除回调 ====================

        private void OnRelationCreated(Edge edge)
        {
            if (m_RelationSet == null)
                return;
            if (edge.userData is TableRelation)
                return;

            var relation = CreateRelationFromEdge(edge);
            edge.userData = relation;
            m_RelationSet.Relations.Add(relation);
        }

        private void OnRelationRemoved(Edge edge)
        {
            if (m_RelationSet == null)
                return;
            if (edge.userData is TableRelation relation)
            {
                m_RelationSet.Relations.Remove(relation);
                edge.userData = null;
            }
        }

        private TableRelation CreateRelationFromEdge(Edge edge)
        {
            var outInfo = (edge.output as Port)?.userData as PortInfo;
            var inInfo = (edge.input as Port)?.userData as PortInfo;
            return new TableRelation
            {
                Name = GenerateRelationName(outInfo?.TableName, inInfo?.TableName),
                RelationType = RelationType.OneToMany,
                SourceTable = outInfo?.TableName ?? string.Empty,
                SourceField = outInfo?.Field.Name ?? string.Empty,
                TargetTable = inInfo?.TableName ?? string.Empty,
                TargetField = inInfo?.Field.Name ?? string.Empty,
            };
        }

        private string GenerateRelationName(string sourceTable, string targetTable)
        {
            var baseName = string.IsNullOrEmpty(sourceTable) ? "Table" : sourceTable;
            if (!string.IsNullOrEmpty(targetTable))
                baseName += "_" + targetTable;
            int index = 1;
            string name;
            do
            {
                name = baseName + "_" + index;
                index++;
            } while (m_RelationSet != null && m_RelationSet.HasRelationName(name));
            return name;
        }

        private void SyncRelationsFromGraph()
        {
            if (m_RelationSet == null)
                return;

            m_RelationSet.Relations.Clear();
            foreach (var element in m_GraphView.edges.ToList())
            {
                if (element is Edge edge && edge.userData is TableRelation relation)
                {
                    m_RelationSet.Relations.Add(relation);
                }
            }
        }

        private bool CreateEdgeForRelation(TableRelation relation)
        {
            if (!m_Schemas.TryGetValue(relation.SourceTable, out var srcSchema) ||
                !m_Schemas.TryGetValue(relation.TargetTable, out var tgtSchema))
                return false;

            // 查找或创建节点
            var srcNode = FindNode(srcSchema.TableName);
            var tgtNode = FindNode(tgtSchema.TableName);
            if (srcNode == null)
            {
                srcNode = new TableNodeView(srcSchema);
                m_GraphView.AddElement(srcNode);
            }
            if (tgtNode == null)
            {
                tgtNode = new TableNodeView(tgtSchema);
                m_GraphView.AddElement(tgtNode);
            }

            var outputPort = srcNode.GetOutputPort(relation.SourceField);
            var inputPort = tgtNode.GetInputPort(relation.TargetField);
            if (outputPort == null || inputPort == null)
                return false;

            var edge = new Edge { userData = relation };
            edge.output = outputPort;
            edge.input = inputPort;
            edge.SetEnabled(true);
            m_GraphView.AddElement(edge);
            outputPort.Connect(edge);
            inputPort.Connect(edge);
            return true;
        }

        private TableNodeView FindNode(string tableName)
        {
            foreach (var element in m_GraphView.nodes.ToList())
            {
                if (element is TableNodeView node && node.Schema.TableName == tableName)
                    return node;
            }
            return null;
        }

        private void ClearGraph()
        {
            foreach (var element in m_GraphView.graphElements.ToList())
            {
                m_GraphView.RemoveElement(element);
            }
        }

        // ==================== Inspector / 状态栏 ====================

        private void OnInspectorGui()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);

            var selection = m_GraphView?.selection;
            var selectedEdge = selection?.OfType<Edge>().FirstOrDefault();
            var selectedNode = selection?.OfType<TableNodeView>().FirstOrDefault();

            if (selectedEdge != null && selectedEdge.userData is TableRelation)
            {
                DrawRelationInspector(selectedEdge);
            }
            else if (selectedNode != null)
            {
                DrawNodeInspector(selectedNode);
            }
            else
            {
                EditorGUILayout.HelpBox("选中连线编辑关系属性，或选中节点查看字段信息。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRelationInspector(Edge edge)
        {
            var relation = edge.userData as TableRelation;
            if (relation == null)
            {
                EditorGUILayout.HelpBox("该连线未绑定关系定义，请重新连接。", MessageType.Warning);
                return;
            }

            relation.Name = EditorGUILayout.TextField("关系名", relation.Name);

            var newType = (RelationType)EditorGUILayout.EnumPopup("关系类型", relation.RelationType);
            if (newType != relation.RelationType)
            {
                relation.RelationType = newType;
            }

            EditorGUILayout.LabelField("源表", relation.SourceTable);
            EditorGUILayout.LabelField("源字段", relation.SourceField);
            EditorGUILayout.LabelField("目标表", relation.TargetTable);
            EditorGUILayout.LabelField("目标字段", relation.TargetField);

            if (relation.RelationType == RelationType.ManyToMany)
            {
                DrawJoinTableInspector(relation);
            }

            relation.Description = EditorGUILayout.TextField("说明", relation.Description);

            if (GUILayout.Button("删除关系"))
            {
                m_GraphView.RemoveElement(edge);
            }
        }

        private void DrawJoinTableInspector(TableRelation relation)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("多对多（中间表）", EditorStyles.boldLabel);

            var tableNames = m_Schemas.Keys.ToList();
            if (tableNames.Count == 0)
            {
                EditorGUILayout.HelpBox("无可用中间表", MessageType.Warning);
                return;
            }

            // 中间表
            var joinIndex = Mathf.Max(0, tableNames.IndexOf(relation.JoinTable));
            joinIndex = EditorGUILayout.Popup("中间表", joinIndex, tableNames.ToArray());
            relation.JoinTable = tableNames[joinIndex];

            if (m_Schemas.TryGetValue(relation.JoinTable, out var joinSchema))
            {
                var fieldNames = joinSchema.Fields.Select(f => f.Name).ToArray();
                var joinSourceIndex = Mathf.Max(0, Array.IndexOf(fieldNames, relation.JoinSourceField));
                joinSourceIndex = EditorGUILayout.Popup("源外键字段", joinSourceIndex, fieldNames);
                relation.JoinSourceField = fieldNames[joinSourceIndex];

                var joinTargetIndex = Mathf.Max(0, Array.IndexOf(fieldNames, relation.JoinTargetField));
                joinTargetIndex = EditorGUILayout.Popup("目标外键字段", joinTargetIndex, fieldNames);
                relation.JoinTargetField = fieldNames[joinTargetIndex];
            }
        }

        private void DrawNodeInspector(TableNodeView node)
        {
            EditorGUILayout.LabelField("表", node.Schema.TableName, EditorStyles.boldLabel);

            var pk = node.Schema.GetPrimaryKey();
            if (pk != null)
            {
                EditorGUILayout.LabelField("主键", $"{pk.Name} ({pk.Type})");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("字段", EditorStyles.boldLabel);
            foreach (var field in node.Schema.Fields)
            {
                EditorGUILayout.LabelField(field.Name, field.Type + (field.IsPrimaryKey ? " [PK]" : string.Empty));
            }
        }

        private void OnStatusGui()
        {
            EditorGUILayout.HelpBox(m_StatusMessage ?? string.Empty, MessageType.None);
        }
    }

    /// <summary>
    /// 简易输入对话框（编辑器内获取字符串输入）
    /// </summary>
    internal static class EditorInputDialog
    {
        public static string Show(string title, string label, string defaultValue)
        {
            string result = null;
            var window = EditorWindow.GetWindow<InputDialogWindow>(true, title);
            window.Init(label, defaultValue, v =>
            {
                result = v;
                window.Close();
            });
            window.ShowModal();
            return result;
        }

        private class InputDialogWindow : EditorWindow
        {
            private string m_Label;
            private string m_Value;
            private Action<string> m_OnConfirm;

            public void Init(string label, string defaultValue, Action<string> onConfirm)
            {
                m_Label = label;
                m_Value = defaultValue;
                m_OnConfirm = onConfirm;
                minSize = maxSize = new Vector2(360, 90);
            }

            private void OnGUI()
            {
                EditorGUILayout.LabelField(m_Label);
                m_Value = EditorGUILayout.TextField(m_Value);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("确定"))
                {
                    m_OnConfirm?.Invoke(m_Value);
                }
                if (GUILayout.Button("取消"))
                {
                    Close();
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}

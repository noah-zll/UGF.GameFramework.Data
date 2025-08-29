using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 数据表查看器
    /// </summary>
    public class BinaryDataViewer : EditorWindow
    {
        private string selectedFilePath = string.Empty;
        private BinaryDataDeserializer.DataTableInfo dataTableInfo;
        private Vector2 scrollPosition;
        private Vector2 recordScrollPosition;
        private Vector2 fieldScrollPosition;
        
        // 搜索和筛选
        private string searchText = string.Empty;
        private string selectedSearchField = "全部字段";
        private List<BinaryDataDeserializer.DataRecord> filteredRecords = new List<BinaryDataDeserializer.DataRecord>();
        
        // 记录详情
        private int selectedRecordIndex = -1;
        private BinaryDataDeserializer.DataRecord selectedRecord;
        
        // 分页相关
        private int currentPage = 0;
        private int recordsPerPage = 50;
        private int totalPages = 1;
        
        // 显示选项
        private bool showRecordList = true;
        private bool showRecordDetails = true;
        
        [MenuItem("UGF/GameFramework/数据表查看器")]
        public static void ShowWindow()
        {
            var window = GetWindow<BinaryDataViewer>("数据表查看器");
            window.minSize = new Vector2(1200, 800);
            window.Show();
        }
        
        /// <summary>
        /// 打开指定的数据表文件
        /// </summary>
        public static void OpenFile(string filePath)
        {
            var window = GetWindow<BinaryDataViewer>("数据表查看器");
            window.LoadDataTableFile(filePath);
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            DrawFileSelection();
            DrawTableInfo();
            DrawSearchAndFilter();
            
            if (dataTableInfo != null && dataTableInfo.IsValid)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 左侧：记录列表
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
                DrawRecordList();
                EditorGUILayout.EndVertical();
                
                // 右侧：记录详情
                EditorGUILayout.BeginVertical();
                DrawRecordDetails();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制标题
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("数据表查看器", titleStyle);
            EditorGUILayout.LabelField("查看和分析GameFramework二进制数据表文件", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
        }
        
        /// <summary>
        /// 绘制文件选择区域
        /// </summary>
        private void DrawFileSelection()
        {
            EditorGUILayout.LabelField("文件选择", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("数据表文件:", GUILayout.Width(80));
            selectedFilePath = EditorGUILayout.TextField(selectedFilePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("选择数据表文件", "Assets/StreamingAssets/DataTables", "bytes");
                if (!string.IsNullOrEmpty(path))
                {
                    selectedFilePath = path;
                    LoadDataTableFile(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath);
            if (GUILayout.Button("加载文件", GUILayout.Height(25)))
            {
                LoadDataTableFile(selectedFilePath);
            }
            
            if (GUILayout.Button("刷新", GUILayout.Height(25), GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath))
                {
                    LoadDataTableFile(selectedFilePath);
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        /// <summary>
        /// 加载数据表文件
        /// </summary>
        private void LoadDataTableFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.LogError("文件路径无效或文件不存在");
                return;
            }
            
            try
            {
                selectedFilePath = filePath;
                
                // 首先验证文件格式
                if (!BinaryDataSerializer.ValidateBinaryFile(filePath))
                {
                    Debug.LogError($"文件格式验证失败: {filePath}");
                    Debug.LogError("文件可能不是有效的二进制数据表文件或版本不匹配");
                    return;
                }
                
                // 输出文件信息用于调试
                var fileInfo = BinaryDataSerializer.ReadBinaryFileInfo(filePath);
                Debug.Log($"二进制文件信息:\n{fileInfo}");
                
                var fileData = File.ReadAllBytes(filePath);
                Debug.Log($"文件大小: {fileData.Length} 字节");
                
                // 反序列化数据表
                dataTableInfo = BinaryDataDeserializer.DeserializeDataTable(fileData);
                
                if (dataTableInfo.IsValid)
                {
                    // 初始化筛选记录
                    filteredRecords = new List<BinaryDataDeserializer.DataRecord>(dataTableInfo.Records);
                    UpdatePagination();
                    
                    // 重置选择状态
                    selectedRecordIndex = -1;
                    selectedRecord = null;
                    currentPage = 0;
                    
                    Debug.Log($"成功加载数据表: {dataTableInfo.TableName}, 记录数: {dataTableInfo.Records.Count}");
                }
                else
                {
                    Debug.LogError("无法解析数据表文件 - 反序列化失败");
                    Debug.LogError("请检查文件是否损坏或格式是否正确");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载数据表文件时发生错误: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
                dataTableInfo = null;
            }
        }
        
        /// <summary>
        /// 绘制表格信息
        /// </summary>
        private void DrawTableInfo()
        {
            if (dataTableInfo == null || !dataTableInfo.IsValid) return;
            
            EditorGUILayout.LabelField("表格信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"表名: {dataTableInfo.TableName}");
            EditorGUILayout.LabelField($"记录总数: {dataTableInfo.Records.Count}");
            EditorGUILayout.LabelField($"筛选后记录数: {filteredRecords.Count}");
            
            if (dataTableInfo.Records.Count > 0)
            {
                var firstRecord = dataTableInfo.Records[0];
                EditorGUILayout.LabelField($"字段数量: {dataTableInfo.Fields.Count}");
                
                if (dataTableInfo.Fields.Count > 0)
                {
                    EditorGUILayout.LabelField("字段列表:", EditorStyles.boldLabel);
                    var fieldNames = string.Join(", ", firstRecord.Values.Keys);
                    EditorGUILayout.LabelField(fieldNames, EditorStyles.wordWrappedLabel);
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        /// <summary>
        /// 绘制搜索和筛选
        /// </summary>
        private void DrawSearchAndFilter()
        {
            if (dataTableInfo == null || !dataTableInfo.IsValid) return;
            
            EditorGUILayout.LabelField("搜索和筛选", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            // 搜索字段选择
            var fieldOptions = new List<string> { "全部字段" };
            if (dataTableInfo.Records.Count > 0)
            {
                fieldOptions.AddRange(dataTableInfo.Records[0].Values.Keys);
            }
            
            EditorGUILayout.LabelField("搜索字段:", GUILayout.Width(70));
            var selectedIndex = fieldOptions.IndexOf(selectedSearchField);
            if (selectedIndex < 0) selectedIndex = 0;
            selectedIndex = EditorGUILayout.Popup(selectedIndex, fieldOptions.ToArray(), GUILayout.Width(120));
            selectedSearchField = fieldOptions[selectedIndex];
            
            // 搜索文本
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            var newSearchText = EditorGUILayout.TextField(searchText);
            
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                ApplyFilter();
            }
            
            if (GUILayout.Button("清除", GUILayout.Width(50)))
            {
                searchText = string.Empty;
                selectedSearchField = "全部字段";
                ApplyFilter();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 分页设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("每页记录数:", GUILayout.Width(80));
            var newRecordsPerPage = EditorGUILayout.IntSlider(recordsPerPage, 10, 200, GUILayout.Width(200));
            if (newRecordsPerPage != recordsPerPage)
            {
                recordsPerPage = newRecordsPerPage;
                UpdatePagination();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        /// <summary>
        /// 绘制记录列表
        /// </summary>
        private void DrawRecordList()
        {
            EditorGUILayout.LabelField($"记录列表 (第 {currentPage + 1}/{totalPages} 页)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // 分页控制
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("上一页", GUILayout.Width(60)))
            {
                currentPage--;
            }
            GUI.enabled = true;
            
            EditorGUILayout.LabelField($"第 {currentPage + 1} 页，共 {totalPages} 页", EditorStyles.centeredGreyMiniLabel);
            
            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button("下一页", GUILayout.Width(60)))
            {
                currentPage++;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 记录列表
            recordScrollPosition = EditorGUILayout.BeginScrollView(recordScrollPosition, GUILayout.Height(400));
            
            var startIndex = currentPage * recordsPerPage;
            var endIndex = Math.Min(startIndex + recordsPerPage, filteredRecords.Count);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                var record = filteredRecords[i];
                var isSelected = selectedRecordIndex == i;
                
                var style = isSelected ? EditorStyles.selectionRect : EditorStyles.label;
                
                // 显示记录的主要信息
                var displayText = GetRecordDisplayText(record);
                
                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                if (GUI.Button(rect, displayText, style))
                {
                    selectedRecordIndex = i;
                    selectedRecord = record;
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 应用筛选
        /// </summary>
        private void ApplyFilter()
        {
            if (dataTableInfo == null || !dataTableInfo.IsValid)
            {
                filteredRecords.Clear();
                return;
            }
            
            filteredRecords.Clear();
            
            foreach (var record in dataTableInfo.Records)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    filteredRecords.Add(record);
                    continue;
                }
                
                bool matches = false;
                
                if (selectedSearchField == "全部字段")
                {
                    // 搜索所有字段
                    foreach (var field in record.Values)
                    {
                        if (field.Value.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matches = true;
                            break;
                        }
                    }
                }
                else
                {
                    // 搜索指定字段
                    if (record.Values.ContainsKey(selectedSearchField))
                    {
                        var fieldValue = record.Values[selectedSearchField].ToString();
                        matches = fieldValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                
                if (matches)
                {
                    filteredRecords.Add(record);
                }
            }
            
            UpdatePagination();
            currentPage = 0;
            selectedRecordIndex = -1;
            selectedRecord = null;
        }
        
        /// <summary>
        /// 更新分页信息
        /// </summary>
        private void UpdatePagination()
        {
            totalPages = Mathf.CeilToInt((float)filteredRecords.Count / recordsPerPage);
            if (totalPages == 0) totalPages = 1;
        }
        
        /// <summary>
        /// 获取记录显示文本
        /// </summary>
        private string GetRecordDisplayText(BinaryDataDeserializer.DataRecord record)
        {
            var sb = new StringBuilder();
            sb.Append($"[{record.Index}] ");
            
            // 显示前几个字段的值
            int fieldCount = 0;
            foreach (var field in record.Values)
            {
                if (fieldCount >= 3) break;
                
                if (fieldCount > 0) sb.Append(", ");
                sb.Append($"{field.Key}: {field.Value}");
                fieldCount++;
            }
            
            if (record.Values.Count > 3)
            {
                sb.Append("...");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 绘制记录详情
        /// </summary>
        private void DrawRecordDetails()
        {
            if (selectedRecord == null)
            {
                EditorGUILayout.LabelField("记录详情", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("请选择一条记录查看详情", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }
            
            EditorGUILayout.LabelField($"记录 {selectedRecord.Index} 详情", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // 基本信息
            EditorGUILayout.LabelField("基本信息:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"索引: {selectedRecord.Index}");
            EditorGUILayout.LabelField($"字段数量: {selectedRecord.Values.Count}");
            
            EditorGUILayout.Space(10);
            
            // 字段详情
            EditorGUILayout.LabelField("字段详情:", EditorStyles.boldLabel);
            
            fieldScrollPosition = EditorGUILayout.BeginScrollView(fieldScrollPosition, GUILayout.Height(300));
            
            foreach (var field in selectedRecord.Values)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                EditorGUILayout.LabelField(field.Key, EditorStyles.boldLabel, GUILayout.Width(120));
                
                var valueStr = field.Value?.ToString() ?? "null";
                var valueType = field.Value?.GetType().Name ?? "Unknown";
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"值: {valueStr}");
                EditorGUILayout.LabelField($"类型: {valueType}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        

    }
}
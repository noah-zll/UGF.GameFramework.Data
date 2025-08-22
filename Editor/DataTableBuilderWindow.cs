using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UGF.GameFramework.Data;
using OfficeOpenXml;
using System.Text;
using System.Reflection;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// Excel文件排序类型
    /// </summary>
    public enum ExcelFileSortType
    {
        Name,           // 按文件名排序
        Size,           // 按文件大小排序
        ModifiedTime,   // 按修改时间排序
        Status          // 按状态排序
    }
    
    /// <summary>
    /// 构建结果信息
    /// </summary>
    [Serializable]
    public class BuildResultInfo
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public BuildOperationType OperationType { get; set; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 文件名或表名
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 详细信息
        /// </summary>
        public string Details { get; set; }
        
        /// <summary>
        /// 生成的文件列表
        /// </summary>
        public List<string> GeneratedFiles { get; set; }
        
        /// <summary>
        /// 构建时间
        /// </summary>
        public DateTime BuildTime { get; set; }
        
        /// <summary>
        /// 耗时（毫秒）
        /// </summary>
        public long ElapsedMs { get; set; }
        
        /// <summary>
        /// 数据统计
        /// </summary>
        public BuildStatistics Statistics { get; set; }
        
        public BuildResultInfo()
        {
            GeneratedFiles = new List<string>();
            BuildTime = DateTime.Now;
            Statistics = new BuildStatistics();
        }
    }
    
    /// <summary>
    /// 构建操作类型
    /// </summary>
    public enum BuildOperationType
    {
        Parse,          // 解析Excel
        GenerateCode,   // 生成代码
        BuildData,      // 构建数据
        BuildAll,       // 全部构建
        GenerateEnum,   // 生成枚举
        Validation,     // 验证
        Other          // 其他
    }
    
    /// <summary>
    /// 构建统计信息
    /// </summary>
    [Serializable]
    public class BuildStatistics
    {
        /// <summary>
        /// 字段数量
        /// </summary>
        public int FieldCount { get; set; }
        
        /// <summary>
        /// 数据行数量
        /// </summary>
        public int DataRowCount { get; set; }
        
        /// <summary>
        /// 代码文件大小
        /// </summary>
        public long CodeFileSize { get; set; }
        
        /// <summary>
        /// 数据文件大小
        /// </summary>
        public long DataFileSize { get; set; }
        
        /// <summary>
        /// 生成的文件数量
        /// </summary>
        public int GeneratedFileCount { get; set; }
    }
    
    /// <summary>
    /// 数据表构建器窗口 - 重新整理版本
    /// </summary>
    public class DataTableBuilderWindow : EditorWindow
    {
        #region 私有字段
        
        // 主要设置
        private DataTableBuilderSettings m_Settings;
        private Vector2 m_MainScrollPosition;
        
        // 路径配置
        private string m_ExcelDirectory = "Assets/Configs/Excel";
        private string m_CodeOutputPath = "Assets/Scripts/DataTables";
        private string m_DataOutputPath = "Assets/StreamingAssets/DataTables";
        private string m_EnumOutputPath = "Assets/Scripts/Enums";
        private string m_Namespace = "GameData";
        
        // Excel文件管理
        private List<ExcelFileInfo> m_ExcelFiles = new List<ExcelFileInfo>();
        private Dictionary<string, bool> m_ExcelFileSelection = new Dictionary<string, bool>();
        private Vector2 m_ExcelListScrollPosition;
        private ExcelFileSortType m_ExcelFileSortType = ExcelFileSortType.Name;
        
        // 单文件处理
        private string m_SingleExcelPath = "";
        private string m_SingleSheetName = "";
        private ExcelTableInfo m_CurrentTableInfo;
        
        // 类型定义表
        private string m_TypeDefinitionFilePath = "";
        private bool m_GenerateFromTypeDefinition = false;
        private TypeDefinitionParseResult m_TypeDefinitionParseResult;
        private string m_TypeDefinitionOutputPath = "Assets/Scripts/Generated/TypeDefinitions";
        
        // 构建选项
        private bool m_AutoRefresh = true;
        private bool m_VerboseLogging = false;
        
        // UI状态
        private bool m_ShowExcelList = true;
        private bool m_ShowSingleFileSection = false;
        private bool m_ShowTypeDefinitionSection = false;
        private bool m_ShowPreview = false;
        private bool m_ShowBuildResults = false;
        
        // 构建结果
        private List<BuildResultInfo> m_BuildResults = new List<BuildResultInfo>();
        private Vector2 m_BuildResultsScrollPosition;
        
        // 用户交互和反馈
        private bool m_IsBuilding = false;
        private float m_BuildProgress = 0f;
        private string m_BuildProgressText = "";
        private string m_StatusMessage = "就绪";
        private DateTime m_LastStatusUpdate = DateTime.Now;
        private bool m_ShowConfirmDialog = false;
        private string m_ConfirmDialogTitle = "";
        private string m_ConfirmDialogMessage = "";
        private System.Action m_ConfirmDialogAction = null;
        
        #endregion
        
        #region 构建结果管理
        
        /// <summary>
        /// 添加构建结果
        /// </summary>
        private void AddBuildResult(BuildOperationType operationType, bool isSuccess, string fileName, string message, 
            string details = null, List<string> generatedFiles = null, long elapsedMs = 0, BuildStatistics statistics = null)
        {
            var result = new BuildResultInfo
            {
                OperationType = operationType,
                IsSuccess = isSuccess,
                FileName = fileName,
                Message = message,
                Details = details,
                GeneratedFiles = generatedFiles ?? new List<string>(),
                ElapsedMs = elapsedMs,
                Statistics = statistics ?? new BuildStatistics()
            };
            
            m_BuildResults.Add(result);
            
            // 自动显示构建结果
            if (m_BuildResults.Count == 1)
            {
                m_ShowBuildResults = true;
            }
        }
        
        /// <summary>
        /// 添加简单构建结果
        /// </summary>
        private void AddBuildResult(BuildOperationType operationType, bool isSuccess, string message)
        {
            AddBuildResult(operationType, isSuccess, "", message);
        }
        
        /// <summary>
        /// 清空构建结果
        /// </summary>
        private void ClearBuildResults()
        {
            m_BuildResults.Clear();
            m_ShowBuildResults = false;
        }
        
        #endregion
        
        #region 用户交互和反馈
        
        /// <summary>
        /// 更新状态消息
        /// </summary>
        private void UpdateStatus(string message)
        {
            m_StatusMessage = message;
            m_LastStatusUpdate = DateTime.Now;
            Repaint();
        }
        
        /// <summary>
        /// 开始构建进度
        /// </summary>
        private void StartBuildProgress(string text)
        {
            m_IsBuilding = true;
            m_BuildProgress = 0f;
            m_BuildProgressText = text;
            UpdateStatus("构建中...");
        }
        
        /// <summary>
        /// 更新构建进度
        /// </summary>
        private void UpdateBuildProgress(float progress, string text = null)
        {
            m_BuildProgress = Mathf.Clamp01(progress);
            if (!string.IsNullOrEmpty(text))
            {
                m_BuildProgressText = text;
            }
            Repaint();
        }
        
        /// <summary>
        /// 完成构建进度
        /// </summary>
        private void CompleteBuildProgress()
        {
            m_IsBuilding = false;
            m_BuildProgress = 1f;
            UpdateStatus("构建完成");
            
            // 2秒后重置状态
            EditorApplication.delayCall += () =>
            {
                if (!m_IsBuilding)
                {
                    UpdateStatus("就绪");
                }
            };
        }
        
        /// <summary>
        /// 显示确认对话框
        /// </summary>
        private void ShowConfirmDialog(string title, string message, System.Action onConfirm)
        {
            m_ShowConfirmDialog = true;
            m_ConfirmDialogTitle = title;
            m_ConfirmDialogMessage = message;
            m_ConfirmDialogAction = onConfirm;
        }
        
        /// <summary>
        /// 绘制进度条
        /// </summary>
        private void DrawProgressBar()
        {
            if (m_IsBuilding)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.LabelField(m_BuildProgressText, EditorStyles.boldLabel);
                
                var rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, m_BuildProgress, $"{(m_BuildProgress * 100):F0}%");
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }
        
        /// <summary>
        /// 绘制状态栏
        /// </summary>
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // 状态消息
            var statusStyle = new GUIStyle(EditorStyles.toolbarTextField)
            {
                normal = { textColor = Color.gray }
            };
            EditorGUILayout.LabelField($"状态: {m_StatusMessage}", statusStyle);
            
            GUILayout.FlexibleSpace();
            
            // 文件统计
            var selectedCount = m_ExcelFileSelection.Count(kvp => kvp.Value);
            EditorGUILayout.LabelField($"已选择: {selectedCount}/{m_ExcelFiles.Count}", EditorStyles.toolbarTextField, GUILayout.Width(100));
            
            // 构建结果统计
            if (m_BuildResults.Count > 0)
            {
                var successCount = m_BuildResults.Count(r => r.IsSuccess);
                var failureCount = m_BuildResults.Count - successCount;
                var resultText = $"成功: {successCount} 失败: {failureCount}";
                EditorGUILayout.LabelField(resultText, EditorStyles.toolbarTextField, GUILayout.Width(120));
            }
            
            // 时间显示
            EditorGUILayout.LabelField(DateTime.Now.ToString("HH:mm:ss"), EditorStyles.toolbarTextField, GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 处理确认对话框
        /// </summary>
        private void HandleConfirmDialog()
        {
            if (m_ShowConfirmDialog)
            {
                if (EditorUtility.DisplayDialog(m_ConfirmDialogTitle, m_ConfirmDialogMessage, "确定", "取消"))
                {
                    m_ConfirmDialogAction?.Invoke();
                }
                
                m_ShowConfirmDialog = false;
                m_ConfirmDialogAction = null;
            }
        }
        
        /// <summary>
        /// 处理键盘快捷键
        /// </summary>
        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                // Ctrl+R: 刷新文件列表
                if (e.control && e.keyCode == KeyCode.R)
                {
                    RefreshExcelFiles();
                    e.Use();
                }
                
                // Ctrl+A: 全选文件
                if (e.control && e.keyCode == KeyCode.A)
                {
                    SelectAllFiles();
                    e.Use();
                }
                
                // Ctrl+D: 取消全选
                if (e.control && e.keyCode == KeyCode.D)
                {
                    DeselectAllFiles();
                    e.Use();
                }
                
                // F5: 全部构建
                if (e.keyCode == KeyCode.F5)
                {
                    if (HasSelectedFiles())
                    {
                        ShowConfirmDialog("确认构建", "确定要构建所有选中的文件吗？", BuildAllSelectedFiles);
                    }
                    e.Use();
                }
                
                // Escape: 清空构建结果
                if (e.keyCode == KeyCode.Escape)
                {
                    if (m_BuildResults.Count > 0)
                    {
                        ClearBuildResults();
                    }
                    e.Use();
                }
            }
        }
        
        /// <summary>
        /// 检查是否有选中的文件
        /// </summary>
        private bool HasSelectedFiles()
        {
            return m_ExcelFileSelection.Any(kvp => kvp.Value);
        }
        
        /// <summary>
        /// 全选文件
        /// </summary>
        private void SelectAllFiles()
        {
            foreach (var file in m_ExcelFiles)
            {
                m_ExcelFileSelection[file.FilePath] = true;
            }
            UpdateStatus("已全选所有文件");
        }
        
        /// <summary>
        /// 取消全选文件
        /// </summary>
        private void DeselectAllFiles()
        {
            foreach (var key in m_ExcelFileSelection.Keys.ToList())
            {
                m_ExcelFileSelection[key] = false;
            }
            UpdateStatus("已取消全选");
        }
        
        #endregion
        
        #region 窗口管理
        
        [MenuItem("UGF/GameFramework/数据表构建器", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<DataTableBuilderWindow>("数据表构建器");
            window.titleContent = new GUIContent("数据表构建器", "GameFramework数据表构建工具");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadSettings();
            RefreshExcelFiles();
            UpdateStatus("就绪");
        }
        
        private void OnDisable()
        {
            SaveSettings();
        }
        
        #endregion
        
        #region 设置管理
        
        private void LoadSettings()
        {
            // 查找现有设置文件
            string[] guids = AssetDatabase.FindAssets("t:DataTableBuilderSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_Settings = AssetDatabase.LoadAssetAtPath<DataTableBuilderSettings>(path);
            }
            
            // 如果没有找到设置文件，创建默认设置
            if (m_Settings == null)
            {
                CreateDefaultSettings();
            }
            
            // 验证设置有效性
            ValidateAndFixSettings();
            
            // 同步设置到窗口变量
            SyncSettingsToWindow();
        }
        
        private void CreateDefaultSettings()
        {
            m_Settings = CreateInstance<DataTableBuilderSettings>();
            
            // 设置默认值
            m_Settings.ExcelDirectory = "Assets/Configs/Excel";
            m_Settings.CodeOutputDirectory = "Assets/Scripts/DataTables";
            m_Settings.DataOutputDirectory = "Assets/StreamingAssets/DataTables";
            
            // 确保目录存在
            string settingsDir = "Assets/Editor/Settings";
            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);
                
            string settingsPath = Path.Combine(settingsDir, "DataTableBuilderSettings.asset");
            AssetDatabase.CreateAsset(m_Settings, settingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private void SyncSettingsToWindow()
        {
            if (m_Settings == null) return;
            
            m_ExcelDirectory = m_Settings.ExcelDirectory ?? "Assets/Configs/Excel";
            m_CodeOutputPath = m_Settings.CodeOutputDirectory ?? "Assets/Scripts/DataTables";
            m_DataOutputPath = m_Settings.DataOutputDirectory ?? "Assets/StreamingAssets/DataTables";
            m_Namespace = m_Settings.Namespace ?? "GameData";
            m_AutoRefresh = m_Settings.AutoRefresh;
            m_VerboseLogging = m_Settings.VerboseLogging;
            
            // 同步选中的Excel文件列表
            if (m_Settings.SelectedExcelFiles != null && m_ExcelFiles != null)
            {
                foreach (var excelFile in m_ExcelFiles)
                {
                    excelFile.IsSelected = m_Settings.IsExcelFileSelected(excelFile.FilePath);
                }
            }
        }
        
        private void ValidateAndFixSettings()
        {
            if (m_Settings == null) return;
            
            // 调用设置对象的验证方法
            m_Settings.ValidateSettings();
            
            // 确保目录存在
            if (!string.IsNullOrEmpty(m_Settings.ExcelDirectory) && !Directory.Exists(m_Settings.ExcelDirectory))
            {
                try
                {
                    Directory.CreateDirectory(m_Settings.ExcelDirectory);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"无法创建Excel目录 {m_Settings.ExcelDirectory}: {ex.Message}");
                }
            }
            
            if (!string.IsNullOrEmpty(m_Settings.CodeOutputDirectory) && !Directory.Exists(m_Settings.CodeOutputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(m_Settings.CodeOutputDirectory);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"无法创建代码输出目录 {m_Settings.CodeOutputDirectory}: {ex.Message}");
                }
            }
            
            if (!string.IsNullOrEmpty(m_Settings.DataOutputDirectory) && !Directory.Exists(m_Settings.DataOutputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(m_Settings.DataOutputDirectory);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"无法创建数据输出目录 {m_Settings.DataOutputDirectory}: {ex.Message}");
                }
            }
        }
        
        private void SaveSettings()
        {
            if (m_Settings == null) return;
            
            m_Settings.ExcelDirectory = m_ExcelDirectory;
            m_Settings.CodeOutputDirectory = m_CodeOutputPath;
            m_Settings.DataOutputDirectory = m_DataOutputPath;
            m_Settings.Namespace = m_Namespace;
            m_Settings.AutoRefresh = m_AutoRefresh;
            m_Settings.VerboseLogging = m_VerboseLogging;
            
            // 保存选中的Excel文件列表
            m_Settings.ClearSelectedExcelFiles();
            if (m_ExcelFiles != null)
            {
                foreach (var excelFile in m_ExcelFiles)
                {
                    if (excelFile.IsSelected)
                    {
                        m_Settings.AddSelectedExcelFile(excelFile.FilePath);
                    }
                }
            }
            
            EditorUtility.SetDirty(m_Settings);
            AssetDatabase.SaveAssets();
        }
        
        #endregion
        
        #region Excel文件管理
        
        private void RefreshExcelFiles()
        {
            UpdateStatus("正在刷新文件列表...");
            
            m_ExcelFiles.Clear();
            m_ExcelFileSelection.Clear();
            
            if (!Directory.Exists(m_ExcelDirectory))
            {
                UpdateStatus("Excel目录不存在");
                return;
            }
            
            try
            {
                string[] files = Directory.GetFiles(m_ExcelDirectory, "*.xlsx", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(m_ExcelDirectory, "*.xls", SearchOption.AllDirectories))
                    .Where(f => !Path.GetFileName(f).StartsWith("~$")) // 排除临时文件
                    .ToArray();
                    
                foreach (string file in files)
                {
                    try
                    {
                        var fileInfo = CreateExcelFileInfo(file);
                        m_ExcelFiles.Add(fileInfo);
                        
                        string relativePath = GetRelativePath(file);
                        m_ExcelFileSelection[relativePath] = false; // 默认不选中，后续通过SyncSettingsToWindow恢复
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"无法解析Excel文件 {file}: {ex.Message}");
                    }
                }
                
                // 排序文件列表
                SortExcelFiles();
                
                // 刷新完成后，从设置中恢复选中状态
                SyncSettingsToWindow();
                
                UpdateStatus($"已找到 {m_ExcelFiles.Count} 个Excel文件");
            }
            catch (Exception ex)
            {
                Debug.LogError($"刷新Excel文件列表失败: {ex.Message}");
                UpdateStatus("刷新文件列表失败");
            }
        }
        
        private ExcelFileInfo CreateExcelFileInfo(string filePath)
        {
            var fileInfo = new ExcelFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileNameWithoutExtension(filePath),
                RelativePath = GetRelativePath(filePath),
                LastModified = File.GetLastWriteTime(filePath),
                FileSize = new FileInfo(filePath).Length
            };
            
            // 尝试获取工作表信息
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    fileInfo.WorksheetCount = package.Workbook.Worksheets.Count;
                    fileInfo.WorksheetNames = package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
                    fileInfo.HasError = false;
                    fileInfo.Status = "正常";
                }
            }
            catch (Exception ex)
            {
                fileInfo.HasError = true;
                fileInfo.Status = "错误";
                fileInfo.ErrorMessage = ex.Message;
            }
            
            return fileInfo;
        }
        
        private void SortExcelFiles()
        {
            if (m_ExcelFiles == null || m_ExcelFiles.Count == 0)
                return;
                
            switch (m_ExcelFileSortType)
            {
                case ExcelFileSortType.Name:
                    m_ExcelFiles.Sort((a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));
                    break;
                    
                case ExcelFileSortType.Size:
                    m_ExcelFiles.Sort((a, b) => a.FileSize.CompareTo(b.FileSize));
                    break;
                    
                case ExcelFileSortType.ModifiedTime:
                    m_ExcelFiles.Sort((a, b) => b.LastModified.CompareTo(a.LastModified)); // 最新的在前
                    break;
                    
                case ExcelFileSortType.Status:
                    m_ExcelFiles.Sort((a, b) => 
                    {
                        // 错误文件在前，已处理在后，待处理在中间
                        int statusA = a.HasError ? 0 : (a.IsProcessed ? 2 : 1);
                        int statusB = b.HasError ? 0 : (b.IsProcessed ? 2 : 1);
                        int result = statusA.CompareTo(statusB);
                        return result != 0 ? result : string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
                    });
                    break;
            }
        }
        
        #endregion
        
        #region UI绘制
        
        private void OnGUI()
        {
            // 处理键盘快捷键
            HandleKeyboardShortcuts();
            
            // 处理确认对话框
            HandleConfirmDialog();
            
            // 绘制进度条
            DrawProgressBar();
            
            m_MainScrollPosition = EditorGUILayout.BeginScrollView(m_MainScrollPosition);
            
            DrawHeader();
            DrawSettingsSection();
            DrawExcelListSection();
            DrawSingleFileSection();
            DrawTypeDefinitionSection();
            DrawPreviewSection();
            // DrawBuildSection();
            DrawBuildResultsSection();
            
            EditorGUILayout.EndScrollView();
            
            // 绘制状态栏
            DrawStatusBar();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(20);
            
            // 标题
            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("GameFramework 数据表构建器", titleStyle);
            
            // 副标题
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 12
            };
            
            EditorGUILayout.LabelField("将Excel文件转换为GameFramework支持的二进制数据表", subtitleStyle);
            
            EditorGUILayout.Space(5);
            
            // 分隔线
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawSettingsSection()
        {
            EditorGUILayout.BeginVertical("box");
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            EditorGUILayout.LabelField("📁 路径配置", headerStyle);
            
            EditorGUILayout.Space(5);
            
            // Excel目录
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Excel目录:", GUILayout.Width(100));
            string newExcelDirectory = EditorGUILayout.TextField(m_ExcelDirectory);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择Excel目录", m_ExcelDirectory, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    newExcelDirectory = GetRelativePath(selectedPath);
                }
            }
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshExcelFiles();
            }
            EditorGUILayout.EndHorizontal();
            
            if (newExcelDirectory != m_ExcelDirectory)
            {
                m_ExcelDirectory = newExcelDirectory;
                SaveSettings();
                if (m_AutoRefresh)
                {
                    RefreshExcelFiles();
                }
            }
            
            // 代码输出目录
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("代码输出:", GUILayout.Width(100));
            string newCodePath = EditorGUILayout.TextField(m_CodeOutputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择代码输出目录", m_CodeOutputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    newCodePath = GetRelativePath(selectedPath);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (newCodePath != m_CodeOutputPath)
            {
                m_CodeOutputPath = newCodePath;
                SaveSettings();
            }
            
            // 数据输出目录
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("数据输出:", GUILayout.Width(100));
            string newDataPath = EditorGUILayout.TextField(m_DataOutputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择数据输出目录", m_DataOutputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    newDataPath = GetRelativePath(selectedPath);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (newDataPath != m_DataOutputPath)
            {
                m_DataOutputPath = newDataPath;
                SaveSettings();
            }
            
            EditorGUILayout.Space(5);
            
            // 其他设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("命名空间:", GUILayout.Width(100));
            m_Namespace = EditorGUILayout.TextField(m_Namespace);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            m_AutoRefresh = EditorGUILayout.Toggle("自动刷新", m_AutoRefresh, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawBuildResultsSection()
        {
            if (m_BuildResults.Count > 0)
            {
                // 统计信息
                int successCount = m_BuildResults.Count(r => r.IsSuccess);
                int failureCount = m_BuildResults.Count - successCount;
                
                EditorGUILayout.BeginHorizontal();
                m_ShowBuildResults = EditorGUILayout.Foldout(m_ShowBuildResults, 
                    $"构建结果 ({m_BuildResults.Count}) - 成功: {successCount}, 失败: {failureCount}", true);
                
                if (GUILayout.Button("清空", GUILayout.Width(60)))
                {
                    ClearBuildResults();
                }
                
                if (GUILayout.Button("导出日志", GUILayout.Width(80)))
                {
                    ExportBuildLog();
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (m_ShowBuildResults)
                {
                    EditorGUILayout.BeginVertical("box");
                    m_BuildResultsScrollPosition = EditorGUILayout.BeginScrollView(m_BuildResultsScrollPosition, GUILayout.Height(200));
                    
                    foreach (var result in m_BuildResults)
                    {
                        DrawBuildResultItem(result);
                    }
                    
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.Space();
            }
        }
        
        private void DrawBuildResultItem(BuildResultInfo result)
        {
            EditorGUILayout.BeginVertical("box");
            
            // 主要信息行
            EditorGUILayout.BeginHorizontal();
            
            // 状态图标和操作类型
            Color originalColor = GUI.color;
            if (result.IsSuccess)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓", GUILayout.Width(20));
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("✗", GUILayout.Width(20));
            }
            GUI.color = originalColor;
            
            // 操作类型标签
            var operationTypeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = result.IsSuccess ? Color.green : Color.red }
            };
            EditorGUILayout.LabelField($"[{result.OperationType}]", operationTypeStyle, GUILayout.Width(80));
            
            // 文件名
            if (!string.IsNullOrEmpty(result.FileName))
            {
                EditorGUILayout.LabelField(result.FileName, EditorStyles.boldLabel, GUILayout.Width(150));
            }
            
            // 消息
            EditorGUILayout.LabelField(result.Message, GUILayout.ExpandWidth(true));
            
            // 时间和耗时
            var timeInfo = $"{result.BuildTime:HH:mm:ss}";
            if (result.ElapsedMs > 0)
            {
                timeInfo += $" ({result.ElapsedMs}ms)";
            }
            EditorGUILayout.LabelField(timeInfo, EditorStyles.miniLabel, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
            
            // 详细信息（可折叠）
            if (!string.IsNullOrEmpty(result.Details) || result.GeneratedFiles.Count > 0 || HasStatistics(result.Statistics))
            {
                EditorGUI.indentLevel++;
                
                // 详细信息
                if (!string.IsNullOrEmpty(result.Details))
                {
                    EditorGUILayout.LabelField("详细信息:", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(result.Details, EditorStyles.wordWrappedMiniLabel);
                }
                
                // 生成的文件
                if (result.GeneratedFiles.Count > 0)
                {
                    EditorGUILayout.LabelField($"生成文件 ({result.GeneratedFiles.Count}):", EditorStyles.miniLabel);
                    foreach (var file in result.GeneratedFiles.Take(5)) // 最多显示5个文件
                    {
                        EditorGUILayout.LabelField($"  • {Path.GetFileName(file)}", EditorStyles.miniLabel);
                    }
                    if (result.GeneratedFiles.Count > 5)
                    {
                        EditorGUILayout.LabelField($"  ... 还有 {result.GeneratedFiles.Count - 5} 个文件", EditorStyles.miniLabel);
                    }
                }
                
                // 统计信息
                if (HasStatistics(result.Statistics))
                {
                    EditorGUILayout.LabelField("统计信息:", EditorStyles.miniLabel);
                    if (result.Statistics.FieldCount > 0)
                        EditorGUILayout.LabelField($"  字段数: {result.Statistics.FieldCount}", EditorStyles.miniLabel);
                    if (result.Statistics.DataRowCount > 0)
                        EditorGUILayout.LabelField($"  数据行数: {result.Statistics.DataRowCount}", EditorStyles.miniLabel);
                    if (result.Statistics.CodeFileSize > 0)
                        EditorGUILayout.LabelField($"  代码文件大小: {FormatFileSize(result.Statistics.CodeFileSize)}", EditorStyles.miniLabel);
                    if (result.Statistics.DataFileSize > 0)
                        EditorGUILayout.LabelField($"  数据文件大小: {FormatFileSize(result.Statistics.DataFileSize)}", EditorStyles.miniLabel);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private bool HasStatistics(BuildStatistics stats)
        {
            return stats != null && (stats.FieldCount > 0 || stats.DataRowCount > 0 || 
                   stats.CodeFileSize > 0 || stats.DataFileSize > 0 || stats.GeneratedFileCount > 0);
        }
        
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
        
        private void ExportBuildLog()
        {
            try
            {
                var logPath = EditorUtility.SaveFilePanel("导出构建日志", "", "BuildLog", "txt");
                if (string.IsNullOrEmpty(logPath)) return;
                
                var sb = new StringBuilder();
                sb.AppendLine($"数据表构建日志 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(new string('=', 50));
                sb.AppendLine();
                
                foreach (var result in m_BuildResults)
                {
                    sb.AppendLine($"[{result.BuildTime:HH:mm:ss}] [{result.OperationType}] {(result.IsSuccess ? "成功" : "失败")}");
                    sb.AppendLine($"文件: {result.FileName}");
                    sb.AppendLine($"消息: {result.Message}");
                    
                    if (!string.IsNullOrEmpty(result.Details))
                    {
                        sb.AppendLine($"详细: {result.Details}");
                    }
                    
                    if (result.GeneratedFiles.Count > 0)
                    {
                        sb.AppendLine($"生成文件: {string.Join(", ", result.GeneratedFiles.Select(Path.GetFileName))}");
                    }
                    
                    if (result.ElapsedMs > 0)
                    {
                        sb.AppendLine($"耗时: {result.ElapsedMs}ms");
                    }
                    
                    sb.AppendLine();
                }
                
                File.WriteAllText(logPath, sb.ToString());
                EditorUtility.DisplayDialog("导出成功", $"构建日志已导出到:\n{logPath}", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出构建日志时发生错误:\n{ex.Message}", "确定");
            }
        }
        
        private void DrawExcelListSection()
        {
            EditorGUILayout.BeginVertical("box");
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📋 Excel文件列表 ({m_ExcelFiles.Count})", headerStyle);
            GUILayout.FlexibleSpace();
            
            // 排序选项
            EditorGUILayout.LabelField("排序:", GUILayout.Width(35));
            var newSortType = (ExcelFileSortType)EditorGUILayout.EnumPopup(m_ExcelFileSortType, GUILayout.Width(80));
            if (newSortType != m_ExcelFileSortType)
            {
                m_ExcelFileSortType = newSortType;
                SortExcelFiles();
            }
            
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                foreach (var excelFile in m_ExcelFiles)
                {
                    excelFile.IsSelected = true;
                    m_ExcelFileSelection[excelFile.FilePath] = true;
                    m_Settings.AddSelectedExcelFile(excelFile.FilePath);
                }
                SaveSettings();
            }
            
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                foreach (var excelFile in m_ExcelFiles)
                {
                    excelFile.IsSelected = false;
                    m_ExcelFileSelection[excelFile.FilePath] = false;
                    m_Settings.RemoveSelectedExcelFile(excelFile.FilePath);
                }
                SaveSettings();
            }
            
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshExcelFiles();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (m_ExcelFiles.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到Excel文件，请检查Excel目录设置", MessageType.Info);
            }
            else
            {                
                EditorGUILayout.Space(5);
                
                // 表头
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("选择", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.LabelField("文件名", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("大小", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("修改时间", EditorStyles.boldLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField("状态", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                // 分隔线
                var rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, Color.gray);
                
                // 文件列表
                m_ExcelListScrollPosition = EditorGUILayout.BeginScrollView(m_ExcelListScrollPosition, GUILayout.Height(200));
                
                foreach (var excelFile in m_ExcelFiles)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // 选择框
                    bool newSelected = EditorGUILayout.Toggle(excelFile.IsSelected, GUILayout.Width(40));
                    if (newSelected != excelFile.IsSelected)
                    {
                        excelFile.IsSelected = newSelected;
                        m_ExcelFileSelection[excelFile.FilePath] = newSelected;
                        if (newSelected)
                            m_Settings.AddSelectedExcelFile(excelFile.FilePath);
                        else
                            m_Settings.RemoveSelectedExcelFile(excelFile.FilePath);
                        SaveSettings();
                    }
                    
                    // 文件名（可点击）
                    if (GUILayout.Button(excelFile.FileName, EditorStyles.linkLabel, GUILayout.Width(200)))
                    {
                        m_SingleExcelPath = excelFile.FilePath;
                        m_SingleSheetName = "";
                        m_ShowSingleFileSection = true;
                    }
                    
                    // 文件信息
                    EditorGUILayout.LabelField(excelFile.FileSizeString, GUILayout.Width(80));
                    EditorGUILayout.LabelField(excelFile.LastModifiedString, GUILayout.Width(120));
                    
                    // 状态指示
                    Color originalColor = GUI.color;
                    if (excelFile.HasError)
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("错误", GUILayout.Width(60));
                    }
                    else if (excelFile.IsProcessed)
                    {
                        GUI.color = Color.green;
                        EditorGUILayout.LabelField("已处理", GUILayout.Width(60));
                    }
                    else
                    {
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField("待处理", GUILayout.Width(60));
                    }
                    GUI.color = originalColor;
                    
                    // 操作按钮
                    if (GUILayout.Button("预览", GUILayout.Width(50)))
                    {
                        PreviewExcelFile(excelFile.FilePath);
                    }
                    
                    if (GUILayout.Button("生成", GUILayout.Width(50)))
                    {
                        GenerateCodeForSingleFile(excelFile.FilePath);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space(5);
            
            // 批量操作按钮
            EditorGUILayout.BeginHorizontal();
            
            int selectedCount = m_ExcelFiles.Count(f => f.IsSelected);
            GUI.enabled = selectedCount > 0;
            
            if (GUILayout.Button($"生成选中代码 ({selectedCount})", GUILayout.Height(30)))
            {
                GenerateCodeForSelectedFiles();
            }
            
            if (GUILayout.Button($"构建选中数据 ({selectedCount})", GUILayout.Height(30)))
            {
                BuildDataForSelectedFiles();
            }
            
            if (GUILayout.Button($"全部构建 ({selectedCount})", GUILayout.Height(30)))
            {
                BuildAllSelectedFiles();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawSingleFileSection()
        {
            if (!m_ShowSingleFileSection)
                return;
                
            EditorGUILayout.BeginVertical("box");
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📄 单个文件处理", headerStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("关闭", GUILayout.Width(60)))
            {
                m_ShowSingleFileSection = false;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Excel文件选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Excel文件:", GUILayout.Width(100));
            m_SingleExcelPath = EditorGUILayout.TextField(m_SingleExcelPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("选择Excel文件", "", "xlsx");
                if (!string.IsNullOrEmpty(path))
                {
                    m_SingleExcelPath = path;
                    m_CurrentTableInfo = null;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 工作表名称
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("工作表名:", GUILayout.Width(100));
            m_SingleSheetName = EditorGUILayout.TextField(m_SingleSheetName);
            EditorGUILayout.LabelField("(留空使用第一个工作表)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !string.IsNullOrEmpty(m_SingleExcelPath) && File.Exists(m_SingleExcelPath);
            
            if (GUILayout.Button("解析Excel", GUILayout.Height(25)))
            {
                ParseSingleExcelFile();
            }
            
            if (GUILayout.Button("生成代码", GUILayout.Height(25)))
            {
                GenerateCodeForSingleFile(m_SingleExcelPath);
            }
            
            if (GUILayout.Button("构建数据", GUILayout.Height(25)))
            {
                BuildDataForSingleFile(m_SingleExcelPath);
            }
            
            if (GUILayout.Button("全部处理", GUILayout.Height(25)))
            {
                GenerateCodeForSingleFile(m_SingleExcelPath);
                BuildDataForSingleFile(m_SingleExcelPath);
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // 显示表格信息
            if (m_CurrentTableInfo != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("表格信息:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"类名: {m_CurrentTableInfo.ClassName}");
                EditorGUILayout.LabelField($"字段数量: {m_CurrentTableInfo.Fields?.Count ?? 0}");
                EditorGUILayout.LabelField($"数据行数: {m_CurrentTableInfo.Rows?.Count ?? 0}");
                
                if (m_CurrentTableInfo.Fields != null && m_CurrentTableInfo.Fields.Count > 0)
                {
                    EditorGUILayout.LabelField("字段列表:", EditorStyles.boldLabel);
                    foreach (var field in m_CurrentTableInfo.Fields.Take(5))
                    {
                        EditorGUILayout.LabelField($"  {field.Name} ({field.Type})");
                    }
                    
                    if (m_CurrentTableInfo.Fields.Count > 5)
                    {
                        EditorGUILayout.LabelField($"  ... 还有 {m_CurrentTableInfo.Fields.Count - 5} 个字段");
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void ParseSingleExcelFile()
        {
            try
            {
                if (string.IsNullOrEmpty(m_SingleExcelPath) || !File.Exists(m_SingleExcelPath))
                {
                    AddBuildResult(BuildOperationType.Parse, false, "Excel文件路径无效");
                    return;
                }
                
                AddBuildResult(BuildOperationType.Parse, true, Path.GetFileName(m_SingleExcelPath), "开始解析文件");
                
                m_CurrentTableInfo = ExcelParser.ParseExcel(m_SingleExcelPath, m_SingleSheetName);
                
                if (m_CurrentTableInfo != null)
                {
                    AddBuildResult(BuildOperationType.Parse, true, m_CurrentTableInfo.ClassName, "解析成功");
                }
                else
                {
                    AddBuildResult(BuildOperationType.Parse, false, Path.GetFileName(m_SingleExcelPath), "解析失败");
                }
            }
            catch (Exception ex)
            {
                AddBuildResult(BuildOperationType.Parse, false, Path.GetFileName(m_SingleExcelPath), "解析时发生错误", ex.Message);
                m_CurrentTableInfo = null;
            }
        }

        /// <summary>
        /// 获取相对路径
        /// </summary>
        private string GetRelativePath(string absolutePath)
        {
            var projectPath = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");
            if (absolutePath.StartsWith(projectPath))
            {
                return absolutePath.Substring(projectPath.Length + 1).Replace("\\", "/");
            }
            return absolutePath;
        }
        
        private string GetFileSizeString(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024:F1} KB";
            else
                return $"{bytes / (1024 * 1024):F1} MB";
        }
        
        /// <summary>
        /// 绘制类型定义区域
        /// </summary>
        private void DrawTypeDefinitionSection()
        {
            m_ShowTypeDefinitionSection = EditorGUILayout.Foldout(m_ShowTypeDefinitionSection, "类型定义表操作", true, EditorStyles.foldoutHeader);
            
            if (!m_ShowTypeDefinitionSection)
                return;
                
            EditorGUILayout.BeginVertical("box");
            
            // 类型定义文件选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("类型定义文件:", GUILayout.Width(100));
            
            EditorGUI.BeginChangeCheck();
            m_TypeDefinitionFilePath = EditorGUILayout.TextField(m_TypeDefinitionFilePath);
            if (EditorGUI.EndChangeCheck())
            {
                // 文件路径改变时清空解析结果
                m_TypeDefinitionParseResult = null;
            }
            
            if (GUILayout.Button("选择", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("选择类型定义Excel文件", m_ExcelDirectory, "xlsx");
                if (!string.IsNullOrEmpty(path))
                {
                    m_TypeDefinitionFilePath = path;
                    m_TypeDefinitionParseResult = null;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 解析和生成选项
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !string.IsNullOrEmpty(m_TypeDefinitionFilePath) && File.Exists(m_TypeDefinitionFilePath);
            if (GUILayout.Button("解析类型定义", GUILayout.Height(25)))
            {
                ParseTypeDefinitionFile();
            }
            
            GUI.enabled = m_TypeDefinitionParseResult != null && 
                         (m_TypeDefinitionParseResult.Enums.Count > 0 || 
                          m_TypeDefinitionParseResult.Classes.Count > 0 || 
                          m_TypeDefinitionParseResult.Structs.Count > 0);
            if (GUILayout.Button("生成代码", GUILayout.Height(25)))
            {
                GenerateTypeDefinitionCode();
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 显示解析结果
            if (m_TypeDefinitionParseResult != null)
            {
                DrawTypeDefinitionParseResult();
            }
            else if (!string.IsNullOrEmpty(m_TypeDefinitionFilePath))
            {
                EditorGUILayout.HelpBox("请点击\"解析类型定义\"按钮来解析选中的文件", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个包含类型定义的Excel文件", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        /// <summary>
        /// 解析类型定义文件
        /// </summary>
        private void ParseTypeDefinitionFile()
        {
            if (string.IsNullOrEmpty(m_TypeDefinitionFilePath) || !File.Exists(m_TypeDefinitionFilePath))
            {
                UpdateStatus("类型定义文件不存在");
                return;
            }
            
            try
            {
                StartBuildProgress("解析类型定义文件...");
                
                m_TypeDefinitionParseResult = TypeDefinitionParser.ParseTypeDefinitionFile(m_TypeDefinitionFilePath, m_Namespace);
                
                if (m_TypeDefinitionParseResult != null)
                {
                    var totalCount = m_TypeDefinitionParseResult.Enums.Count + 
                                   m_TypeDefinitionParseResult.Classes.Count + 
                                   m_TypeDefinitionParseResult.Structs.Count;
                    
                    UpdateStatus($"解析完成，共找到 {totalCount} 个类型定义");
                    AddBuildResult(BuildOperationType.Parse, true, Path.GetFileName(m_TypeDefinitionFilePath), 
                                 $"成功解析类型定义：枚举 {m_TypeDefinitionParseResult.Enums.Count} 个，类 {m_TypeDefinitionParseResult.Classes.Count} 个，结构体 {m_TypeDefinitionParseResult.Structs.Count} 个");
                }
                else
                {
                    UpdateStatus("解析失败");
                    AddBuildResult(BuildOperationType.Parse, false, Path.GetFileName(m_TypeDefinitionFilePath), "解析类型定义失败");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"解析失败: {ex.Message}");
                AddBuildResult(BuildOperationType.Parse, false, Path.GetFileName(m_TypeDefinitionFilePath), $"解析异常: {ex.Message}");
                Debug.LogError($"解析类型定义文件失败: {ex}");
            }
            finally
            {
                CompleteBuildProgress();
            }
        }
        
        /// <summary>
        /// 生成类型定义代码
        /// </summary>
        private void GenerateTypeDefinitionCode()
        {
            if (m_TypeDefinitionParseResult == null)
            {
                UpdateStatus("请先解析类型定义文件");
                return;
            }
            
            try
            {
                StartBuildProgress("生成类型定义代码...");
                
                var generatedFiles = new List<string>();
                var statistics = new BuildStatistics();
                
                // 生成枚举
                if (m_TypeDefinitionParseResult.Enums.Count > 0)
                {
                    UpdateBuildProgress(0.2f, "生成枚举代码...");
                    foreach (var enumDef in m_TypeDefinitionParseResult.Enums)
                    {
                        var enumCode = EnumCodeGenerator.GenerateEnumCodeFromDefinition(enumDef, m_Namespace);
                        var enumFilePath = Path.Combine(m_EnumOutputPath, $"{enumDef.Name}.cs");
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(enumFilePath));
                        File.WriteAllText(enumFilePath, enumCode, Encoding.UTF8);
                        generatedFiles.Add(enumFilePath);
                        statistics.CodeFileSize += new FileInfo(enumFilePath).Length;
                    }
                }
                
                // 生成类
                if (m_TypeDefinitionParseResult.Classes.Count > 0)
                {
                    UpdateBuildProgress(0.6f, "生成类代码...");
                    foreach (var classDef in m_TypeDefinitionParseResult.Classes)
                    {
                        var classCode = ClassCodeGenerator.GenerateClassCode(classDef, m_Namespace);
                        var classFilePath = Path.Combine(m_TypeDefinitionOutputPath, $"{classDef.Name}.cs");
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(classFilePath));
                        File.WriteAllText(classFilePath, classCode, Encoding.UTF8);
                        generatedFiles.Add(classFilePath);
                        statistics.CodeFileSize += new FileInfo(classFilePath).Length;
                    }
                }
                
                // 生成结构体（如果有的话）
                if (m_TypeDefinitionParseResult.Structs.Count > 0)
                {
                    UpdateBuildProgress(0.8f, "生成结构体代码...");
                    // 使用StructCodeGenerator生成结构体代码
                    foreach (var structDef in m_TypeDefinitionParseResult.Structs)
                    {
                        var structCode = StructCodeGenerator.GenerateStructCode(structDef, m_Namespace);
                        var structFilePath = Path.Combine(m_TypeDefinitionOutputPath, $"{structDef.Name}.cs");
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(structFilePath));
                        File.WriteAllText(structFilePath, structCode, Encoding.UTF8);
                        generatedFiles.Add(structFilePath);
                        statistics.CodeFileSize += new FileInfo(structFilePath).Length;
                    }
                }
                
                statistics.GeneratedFileCount = generatedFiles.Count;
                
                UpdateStatus($"代码生成完成，共生成 {generatedFiles.Count} 个文件");
                AddBuildResult(BuildOperationType.GenerateCode, true, Path.GetFileName(m_TypeDefinitionFilePath), 
                             $"成功生成 {generatedFiles.Count} 个代码文件", 
                             string.Join("\n", generatedFiles.Select(f => Path.GetFileName(f))), 
                             generatedFiles, 0, statistics);
                
                // 刷新资源
                if (m_AutoRefresh)
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"代码生成失败: {ex.Message}");
                AddBuildResult(BuildOperationType.GenerateCode, false, Path.GetFileName(m_TypeDefinitionFilePath), $"代码生成异常: {ex.Message}");
                Debug.LogError($"生成类型定义代码失败: {ex}");
            }
            finally
            {
                CompleteBuildProgress();
            }
        }
        
        /// <summary>
        /// 绘制类型定义解析结果
        /// </summary>
        private void DrawTypeDefinitionParseResult()
        {
            if (m_TypeDefinitionParseResult == null)
                return;
                
            EditorGUILayout.LabelField("解析结果:", EditorStyles.boldLabel);
            
            // 统计信息
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"枚举: {m_TypeDefinitionParseResult.Enums.Count}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"类: {m_TypeDefinitionParseResult.Classes.Count}", GUILayout.Width(60));
            EditorGUILayout.LabelField($"结构体: {m_TypeDefinitionParseResult.Structs.Count}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 详细信息（可折叠）
            if (m_TypeDefinitionParseResult.Enums.Count > 0)
            {
                EditorGUILayout.LabelField("枚举列表:", EditorStyles.boldLabel);
                foreach (var enumDef in m_TypeDefinitionParseResult.Enums.Take(5))
                {
                    EditorGUILayout.LabelField($"  • {enumDef.Name} ({enumDef.Values.Count} 个值)");
                }
                if (m_TypeDefinitionParseResult.Enums.Count > 5)
                {
                    EditorGUILayout.LabelField($"  ... 还有 {m_TypeDefinitionParseResult.Enums.Count - 5} 个枚举");
                }
            }
            
            if (m_TypeDefinitionParseResult.Classes.Count > 0)
            {
                EditorGUILayout.LabelField("类列表:", EditorStyles.boldLabel);
                foreach (var classDef in m_TypeDefinitionParseResult.Classes.Take(5))
                {
                    EditorGUILayout.LabelField($"  • {classDef.Name} ({classDef.Properties.Count} 个属性)");
                }
                if (m_TypeDefinitionParseResult.Classes.Count > 5)
                {
                    EditorGUILayout.LabelField($"  ... 还有 {m_TypeDefinitionParseResult.Classes.Count - 5} 个类");
                }
            }
            
            if (m_TypeDefinitionParseResult.Structs.Count > 0)
            {
                EditorGUILayout.LabelField("结构体列表:", EditorStyles.boldLabel);
                foreach (var structDef in m_TypeDefinitionParseResult.Structs.Take(5))
                {
                    EditorGUILayout.LabelField($"  • {structDef.Name} ({structDef.Fields.Count} 个字段)");
                }
                if (m_TypeDefinitionParseResult.Structs.Count > 5)
                {
                    EditorGUILayout.LabelField($"  ... 还有 {m_TypeDefinitionParseResult.Structs.Count - 5} 个结构体");
                }
            }
        }
        
        /// <summary>
        /// 绘制构建区域
        /// </summary>
        private void DrawBuildSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("构建操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = m_CurrentTableInfo != null;
            if (GUILayout.Button("生成代码", GUILayout.Height(30)))
            {
                GenerateCodeForSingleFile(m_SingleExcelPath);
            }
            
            if (GUILayout.Button("构建数据", GUILayout.Height(30)))
            {
                BuildDataForSingleFile(m_SingleExcelPath);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全部构建", GUILayout.Height(25)))
            {
                BuildAllSelectedFiles();
            }
            
            if (GUILayout.Button("清理输出", GUILayout.Height(25)))
            {
                ClearBuildResults();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        /// <summary>
        /// 绘制预览区域
        /// </summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("数据预览", EditorStyles.boldLabel);
            
            if (m_CurrentTableInfo != null && m_CurrentTableInfo.Rows != null && m_CurrentTableInfo.Rows.Count > 0)
            {
                EditorGUILayout.LabelField($"数据行数: {m_CurrentTableInfo.Rows.Count}");
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("前5行数据:", EditorStyles.boldLabel);
                
                var previewRows = m_CurrentTableInfo.Rows.Take(5);
                foreach (var row in previewRows)
                {
                    var rowText = string.Join(", ", row.Take(5).Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    if (row.Count > 5)
                    {
                        rowText += $", ... (+{row.Count - 5} 个字段)";
                    }
                    EditorGUILayout.LabelField($"  {rowText}");
                }
                
                if (m_CurrentTableInfo.Rows.Count > 5)
                {
                    EditorGUILayout.LabelField($"  ... 还有 {m_CurrentTableInfo.Rows.Count - 5} 行数据");
                }
            }
            else
            {
                EditorGUILayout.LabelField("暂无数据预览");
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void GenerateCodeForSingleFile(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                AddBuildResult(BuildOperationType.GenerateCode, true, fileName, "开始生成代码");
                
                var tableInfo = ExcelParser.ParseExcel(filePath, "");
                
                if (tableInfo != null)
                {
                    // 简单的代码生成实现
                    string code = GenerateSimpleDataModel(tableInfo);
                    
                    string outputPath = Path.Combine(m_CodeOutputPath, $"{tableInfo.ClassName}.cs");
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    File.WriteAllText(outputPath, code);
                    
                    var generatedFiles = new List<string> { outputPath };
                    var statistics = new BuildStatistics
                    {
                        FieldCount = tableInfo.Fields.Count,
                        CodeFileSize = new FileInfo(outputPath).Length,
                        GeneratedFileCount = 1
                    };
                    
                    AddBuildResult(BuildOperationType.GenerateCode, true, tableInfo.ClassName, "代码生成成功", 
                        $"输出路径: {outputPath}", generatedFiles, 0, statistics);
                }
                else
                {
                    AddBuildResult(BuildOperationType.GenerateCode, false, fileName, "解析Excel文件失败");
                }
            }
            catch (Exception ex)
            {
                AddBuildResult(BuildOperationType.GenerateCode, false, Path.GetFileName(filePath), "生成代码时发生错误", ex.Message);
            }
        }
        
        /// <summary>
        /// 预览Excel文件
        /// </summary>
        private void PreviewExcelFile(string filePath)
        {
            try
            {
                UpdateStatus($"正在预览文件: {Path.GetFileName(filePath)}");
                
                // 解析Excel文件
                var tableInfo = ExcelParser.ParseExcel(filePath, "");
                if (tableInfo != null)
                {
                    // 设置当前预览的表格信息
                    m_CurrentTableInfo = tableInfo;
                    
                    // 如果ClassName为空，则根据表名生成
                    if (string.IsNullOrEmpty(m_CurrentTableInfo.ClassName))
                    {
                        m_CurrentTableInfo.ClassName = m_CurrentTableInfo.TableName + "Data";
                    }
                    
                    UpdateStatus($"预览完成，表格: {tableInfo.TableName}");
                }
                else
                {
                    m_CurrentTableInfo = null;
                    UpdateStatus("预览失败: 无法解析Excel文件");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"预览文件失败: {ex.Message}");
                UpdateStatus($"预览文件失败: {ex.Message}");
                m_CurrentTableInfo = null;
            }
        }
        
        private void GenerateCodeForSelectedFiles()
        {
            var selectedFiles = m_ExcelFileSelection.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            
            if (selectedFiles.Count == 0)
            {
                AddBuildResult(BuildOperationType.GenerateCode, false, "没有选中的文件");
                return;
            }
            
            StartBuildProgress("正在批量生成代码...");
            AddBuildResult(BuildOperationType.GenerateCode, true, $"{selectedFiles.Count} 个文件", "开始批量生成代码");
            
            try
            {
                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    var file = selectedFiles[i];
                    var fileName = Path.GetFileName(file);
                    var progress = (float)i / selectedFiles.Count;
                    UpdateBuildProgress(progress, $"正在处理: {fileName}");
                    
                    GenerateCodeForSingleFile(file);
                }
                
                AddBuildResult(BuildOperationType.GenerateCode, true, $"{selectedFiles.Count} 个文件", "代码生成完成");
            }
            finally
            {
                CompleteBuildProgress();
            }
        }
        
        private void BuildDataForSelectedFiles()
        {
            var selectedFiles = m_ExcelFileSelection.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            
            if (selectedFiles.Count == 0)
            {
                AddBuildResult(BuildOperationType.BuildData, false, "没有选中的文件");
                return;
            }
            
            StartBuildProgress("正在批量构建数据...");
            AddBuildResult(BuildOperationType.BuildData, true, $"{selectedFiles.Count} 个文件", "开始批量构建数据");
            
            try
            {
                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    var file = selectedFiles[i];
                    var fileName = Path.GetFileName(file);
                    var progress = (float)i / selectedFiles.Count;
                    UpdateBuildProgress(progress, $"正在处理: {fileName}");
                    
                    BuildDataForSingleFile(file);
                }
                
                AddBuildResult(BuildOperationType.BuildData, true, $"{selectedFiles.Count} 个文件", "数据构建完成");
            }
            finally
            {
                CompleteBuildProgress();
            }
        }
        
        private void BuildAllSelectedFiles()
        {
            var selectedFiles = m_ExcelFileSelection.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            
            if (selectedFiles.Count == 0)
            {
                AddBuildResult(BuildOperationType.BuildAll, false, "没有选中的文件");
                return;
            }
            
            StartBuildProgress("正在全部构建...");
            AddBuildResult(BuildOperationType.BuildAll, true, $"{selectedFiles.Count} 个文件", "开始全部构建");
            
            try
            {
                // 先生成代码
                UpdateBuildProgress(0.1f, "正在生成代码...");
                GenerateCodeForSelectedFiles();
                
                // 再构建数据
                UpdateBuildProgress(0.6f, "正在构建数据...");
                BuildDataForSelectedFiles();
                
                AddBuildResult(BuildOperationType.BuildAll, true, $"{selectedFiles.Count} 个文件", "全部构建完成");
            }
            finally
            {
                CompleteBuildProgress();
            }
        }
        
        private void BuildDataForSingleFile(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                AddBuildResult(BuildOperationType.BuildData, true, fileName, "开始构建数据");
                
                var tableInfo = ExcelParser.ParseExcel(filePath, "");
                
                if (tableInfo != null)
                {
                    // 简化的数据构建实现，避免命名空间错误
                    // var builderSettings = new DataBuilderSettings
                    // {
                    //     OutputDirectory = m_DataOutputPath
                    // };
                    // var builder = new DataBuilder(builderSettings);
                    // 需要先获取数据类型，这里暂时使用简单的序列化方法
                    var dataList = new List<object>();
                    // 将ExcelTableData转换为字节数组的简单实现
                    var jsonData = JsonUtility.ToJson(tableInfo);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    
                    string outputPath = Path.Combine(m_DataOutputPath, $"{tableInfo.ClassName}.bytes");
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    File.WriteAllBytes(outputPath, data);
                    
                    var generatedFiles = new List<string> { outputPath };
                    var statistics = new BuildStatistics
                    {
                        DataRowCount = tableInfo.Rows?.Count ?? 0,
                        DataFileSize = data.Length,
                        GeneratedFileCount = 1
                    };
                    
                    AddBuildResult(BuildOperationType.BuildData, true, tableInfo.ClassName, "数据构建成功", 
                        $"输出路径: {outputPath}", generatedFiles, 0, statistics);
                }
                else
                {
                    AddBuildResult(BuildOperationType.BuildData, false, fileName, "解析Excel文件失败");
                }
            }
            catch (Exception ex)
            {
                AddBuildResult(BuildOperationType.BuildData, false, Path.GetFileName(filePath), "构建数据时发生错误", ex.Message);
            }
        }
        
        /// <summary>
        /// 生成简单的数据模型代码
        /// </summary>
        /// <param name="tableInfo">表格信息</param>
        /// <returns>生成的代码</returns>
        private string GenerateSimpleDataModel(ExcelTableInfo tableInfo)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace {m_Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {tableInfo.ClassName}");
            sb.AppendLine("    {");
            
            if (tableInfo.Fields != null)
            {
                foreach (var field in tableInfo.Fields)
                {
                    sb.AppendLine($"        public {field.Type} {field.Name} {{ get; set; }}");
                }
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Excel文件信息
    /// </summary>
    public class ExcelFileInfo
    {
        public string FilePath;
        public string FileName;
        public string RelativePath;
        public DateTime LastModified;
        public int WorksheetCount;
        public List<string> WorksheetNames;
        public bool IsSelected;
        public bool HasError;
        public bool IsProcessed;
        public string Status;
        public string ErrorMessage;
        public ExcelParseResult ParseResult;
        public long FileSize;
        
        /// <summary>
        /// 获取格式化的最后修改时间字符串
        /// </summary>
        public string LastModifiedString => LastModified.ToString("yyyy-MM-dd HH:mm");
        
        public ExcelFileInfo()
        {
            WorksheetNames = new List<string>();
        }
        
        /// <summary>
        /// 获取格式化的文件大小字符串
        /// </summary>
        public string FileSizeString
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024:F1} KB";
                else
                    return $"{FileSize / (1024 * 1024):F1} MB";
            }
        }
    }
    
    /// <summary>
    /// Excel解析结果
    /// </summary>
    public class ExcelParseResult
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// 是否解析成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 解析的表数据
        /// </summary>
        public List<ExcelTableData> Tables { get; set; } = new List<ExcelTableData>();
    }
    
    /// <summary>
    /// Excel表数据
    /// </summary>
    public class ExcelTableData
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// 列数据
        /// </summary>
        public List<ExcelColumnData> Columns { get; set; }
        
        /// <summary>
        /// 行数据
        /// </summary>
        public List<ExcelRowData> Rows { get; set; }
        
        public ExcelTableData()
        {
            Columns = new List<ExcelColumnData>();
            Rows = new List<ExcelRowData>();
        }
    }
    
    /// <summary>
    /// Excel列数据
    /// </summary>
    public class ExcelColumnData
    {
        /// <summary>
        /// 列索引
        /// </summary>
        public int Index { get; set; }
        
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }
        
        /// <summary>
        /// 字段类型
        /// </summary>
        public string FieldType { get; set; }
        
        /// <summary>
        /// 序列化标记
        /// </summary>
        public string SerializationFlag { get; set; }
        
        /// <summary>
        /// 解析后的字段类型
        /// </summary>
        public FieldTypeInfo ParsedFieldType { get; set; }
        
        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsKey { get; set; }
        
        /// <summary>
        /// 是否为索引
        /// </summary>
        public bool IsIndex { get; set; }
        
        /// <summary>
        /// 是否忽略
        /// </summary>
        public bool IsIgnore { get; set; }
        
        /// <summary>
        /// 是否本地化
        /// </summary>
        public bool IsLocalized { get; set; }
    }
    
    /// <summary>
    /// Excel行数据
    /// </summary>
    public class ExcelRowData
    {
        /// <summary>
        /// 行索引
        /// </summary>
        public int Index { get; set; }
        
        /// <summary>
        /// 单元格值
        /// </summary>
        public List<string> Values { get; set; }
        
        public ExcelRowData()
        {
            Values = new List<string>();
        }
    }
    
    /// <summary>
    /// 字段类型信息
    /// </summary>
    public class FieldTypeInfo
    {
        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName { get; set; }
        
        /// <summary>
        /// 是否为数组类型
        /// </summary>
        public bool IsArray { get; set; }
    }
}
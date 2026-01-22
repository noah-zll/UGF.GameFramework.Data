using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace UGF.GameFramework.Data
{
    /// <summary>
    /// 数据表构建器设置
    /// </summary>
    [CreateAssetMenu(fileName = "DataTableBuilderSettings", menuName = "UGF/GameFramework/Data/数据表构建器设置")]
    public class DataTableBuilderSettings : ScriptableObject
    {
        [Header("路径配置")]
        [SerializeField, Tooltip("Excel文件目录")]
        private string m_ExcelDirectory = "Assets/Configs/Excel";
        
        [SerializeField, Tooltip("代码输出目录")]
        private string m_CodeOutputDirectory = "Assets/Scripts/Generated";
        
        [SerializeField, Tooltip("数据输出目录")]
        private string m_DataOutputDirectory = "Assets/StreamingAssets/Data";

        private static DataTableBuilderSettings _instance;

        public static DataTableBuilderSettings Instance
        {
            get
            {
                if (_instance == null) LoadOrCreateSettings();
                return _instance;
            }
        }
        
        [Header("代码生成配置")]
        [SerializeField, Tooltip("命名空间")]
        private string m_Namespace = "GameData";
        
        [Header("功能开关")]
        [SerializeField, Tooltip("自动刷新资源")]
        private bool m_AutoRefresh = true;
        
        [SerializeField, Tooltip("显示详细日志")]
        private bool m_VerboseLogging = false;
        
        [Header("Excel文件管理")]
        [SerializeField, Tooltip("选中的Excel文件列表")]
        private List<string> m_SelectedExcelFiles = new List<string>();
        
        [Header("类型定义配置")]
        [SerializeField, Tooltip("类型定义文件路径")]
        private string m_TypeDefinitionFilePath = "";
        
        [Header("类型和文件选择状态")]
        [SerializeField, Tooltip("选中的类型定义类型（枚举、类、结构体、常量）")]
        private List<string> m_SelectedTypeDefinitionTypes = new List<string>();
        
        [SerializeField, Tooltip("选中的类型定义文件")]
        private List<string> m_SelectedTypeDefinitionFiles = new List<string>();
        
        [Header("文件处理记录")]
        [SerializeField, Tooltip("文件处理时间记录")]
        private List<string> m_ProcessedFileKeys = new List<string>();
        
        [SerializeField, Tooltip("文件处理时间值")]
        private List<string> m_ProcessedFileValues = new List<string>();
        
        /// <summary>
        /// Excel文件目录
        /// </summary>
        public string ExcelDirectory
        {
            get => m_ExcelDirectory;
            set => m_ExcelDirectory = value;
        }
        
        /// <summary>
        /// 代码输出目录
        /// </summary>
        public string CodeOutputDirectory
        {
            get => m_CodeOutputDirectory;
            set => m_CodeOutputDirectory = value;
        }
        
        /// <summary>
        /// 数据输出目录
        /// </summary>
        public string DataOutputDirectory
        {
            get => m_DataOutputDirectory;
            set => m_DataOutputDirectory = value;
        }
        
        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace
        {
            get => m_Namespace;
            set => m_Namespace = value;
        }
        
        /// <summary>
        /// 自动刷新资源
        /// </summary>
        public bool AutoRefresh
        {
            get => m_AutoRefresh;
            set => m_AutoRefresh = value;
        }
        
        /// <summary>
        /// 显示详细日志
        /// </summary>
        public bool VerboseLogging
        {
            get => m_VerboseLogging;
            set => m_VerboseLogging = value;
        }
        
        /// <summary>
        /// 选中的Excel文件列表
        /// </summary>
        public List<string> SelectedExcelFiles
        {
            get => m_SelectedExcelFiles;
            set => m_SelectedExcelFiles = value;
        }
        
        /// <summary>
        /// 类型定义文件路径
        /// </summary>
        public string TypeDefinitionFilePath
        {
            get => m_TypeDefinitionFilePath;
            set => m_TypeDefinitionFilePath = value;
        }
        
        /// <summary>
        /// 选中的类型定义类型列表
        /// </summary>
        public List<string> SelectedTypeDefinitionTypes
        {
            get => m_SelectedTypeDefinitionTypes;
            set => m_SelectedTypeDefinitionTypes = value;
        }
        
        /// <summary>
        /// 选中的类型定义文件列表
        /// </summary>
        public List<string> SelectedTypeDefinitionFiles
        {
            get => m_SelectedTypeDefinitionFiles;
            set => m_SelectedTypeDefinitionFiles = value;
        }
        
        /// <summary>
        /// 获取文件处理时间字典
        /// </summary>
        private Dictionary<string, DateTime> ProcessedFileTimes
        {
            get
            {
                var dict = new Dictionary<string, DateTime>();
                for (int i = 0; i < m_ProcessedFileKeys.Count && i < m_ProcessedFileValues.Count; i++)
                {
                    if (DateTime.TryParse(m_ProcessedFileValues[i], out DateTime time))
                    {
                        dict[m_ProcessedFileKeys[i]] = time;
                    }
                }
                return dict;
            }
        }

        private static void LoadOrCreateSettings()
        {
            LoadSettings();
            if (_instance == null) CreateDefaultSettings();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        /// <returns>是否成功加载设置</returns>
        private static void LoadSettings()
        {
            _instance = Resources.Load<DataTableBuilderSettings>("DataTableBuilderSettings");
#if UNITY_EDITOR
            if (_instance == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:DataTableBuilderSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _instance = AssetDatabase.LoadAssetAtPath<DataTableBuilderSettings>(path);
                }
            }
#endif
        }

        private static void CreateDefaultSettings()
        {
            _instance = CreateInstance<DataTableBuilderSettings>();
            
            // 设置默认值
            _instance.ExcelDirectory = "Assets/Configs/Excel";
            _instance.CodeOutputDirectory = "Assets/Scripts/Generated";
            _instance.DataOutputDirectory = "Assets/StreamingAssets/DataTables";
            
            // 确保目录存在
            string settingsDir = "Assets/Settings/UGF";
            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);
                
            string settingsPath = Path.Combine(settingsDir, "DataTableBuilderSettings.asset");
            AssetDatabase.CreateAsset(_instance, settingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 验证设置
        /// </summary>
        public void ValidateSettings()
        {
            if (string.IsNullOrEmpty(m_ExcelDirectory))
                m_ExcelDirectory = "Assets/Configs/Excel";
                
            if (string.IsNullOrEmpty(m_CodeOutputDirectory))
                m_CodeOutputDirectory = "Assets/Scripts/Generated";
                
            if (string.IsNullOrEmpty(m_DataOutputDirectory))
                m_DataOutputDirectory = "Assets/StreamingAssets/Data";
                
            if (string.IsNullOrEmpty(m_Namespace))
                m_Namespace = "GameData";
                
            if (m_SelectedExcelFiles == null)
                m_SelectedExcelFiles = new List<string>();
                
            if (m_ProcessedFileKeys == null)
                m_ProcessedFileKeys = new List<string>();
                
            if (m_ProcessedFileValues == null)
                m_ProcessedFileValues = new List<string>();
                
            if (m_SelectedTypeDefinitionTypes == null)
                m_SelectedTypeDefinitionTypes = new List<string>();
                
            if (m_SelectedTypeDefinitionFiles == null)
                m_SelectedTypeDefinitionFiles = new List<string>();
                
            // TypeDefinitionFilePath可以为空，不需要默认值
        }
        
        /// <summary>
        /// 添加选中的Excel文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void AddSelectedExcelFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && !m_SelectedExcelFiles.Contains(filePath))
            {
                m_SelectedExcelFiles.Add(filePath);
            }
        }
        
        /// <summary>
        /// 移除选中的Excel文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void RemoveSelectedExcelFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                m_SelectedExcelFiles.Remove(filePath);
            }
        }
        
        /// <summary>
        /// 清空选中的Excel文件
        /// </summary>
        public void ClearSelectedExcelFiles()
        {
            m_SelectedExcelFiles.Clear();
        }
        
        /// <summary>
        /// 检查Excel文件是否被选中
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否被选中</returns>
        public bool IsExcelFileSelected(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && m_SelectedExcelFiles.Contains(filePath);
        }
        
        /// <summary>
        /// 记录文件处理时间
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="processTime">处理时间</param>
        public void SetFileProcessTime(string filePath, DateTime processTime)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            
            int index = m_ProcessedFileKeys.IndexOf(filePath);
            if (index >= 0)
            {
                // 更新现有记录
                m_ProcessedFileValues[index] = processTime.ToString("O");
            }
            else
            {
                // 添加新记录
                m_ProcessedFileKeys.Add(filePath);
                m_ProcessedFileValues.Add(processTime.ToString("O"));
            }
        }
        
        /// <summary>
        /// 获取文件处理时间
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>处理时间，如果未处理过则返回null</returns>
        public DateTime? GetFileProcessTime(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            
            int index = m_ProcessedFileKeys.IndexOf(filePath);
            if (index >= 0 && index < m_ProcessedFileValues.Count)
            {
                if (DateTime.TryParse(m_ProcessedFileValues[index], out DateTime time))
                {
                    return time;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 检查文件是否需要重新处理（基于修改时间）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileModifyTime">文件修改时间</param>
        /// <returns>是否需要重新处理</returns>
        public bool NeedsReprocess(string filePath, DateTime fileModifyTime)
        {
            var processTime = GetFileProcessTime(filePath);
            return processTime == null || fileModifyTime > processTime.Value;
        }
        
        /// <summary>
        /// 清理无效的文件处理记录
        /// </summary>
        public void CleanupProcessedFiles()
        {
            for (int i = m_ProcessedFileKeys.Count - 1; i >= 0; i--)
            {
                if (i >= m_ProcessedFileValues.Count || 
                    string.IsNullOrEmpty(m_ProcessedFileKeys[i]) ||
                    !DateTime.TryParse(m_ProcessedFileValues[i], out _))
                {
                    m_ProcessedFileKeys.RemoveAt(i);
                    if (i < m_ProcessedFileValues.Count)
                        m_ProcessedFileValues.RemoveAt(i);
                }
            }
        }
        
        #region 类型定义选择管理
        
        /// <summary>
        /// 检查类型定义类型是否被选中
        /// </summary>
        /// <param name="typeName">类型名称（Enum、Class、Struct、Constant）</param>
        /// <returns>是否被选中</returns>
        public bool IsTypeDefinitionTypeSelected(string typeName)
        {
            return !string.IsNullOrEmpty(typeName) && m_SelectedTypeDefinitionTypes.Contains(typeName);
        }
        
        /// <summary>
        /// 设置类型定义类型选择状态
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="selected">是否选中</param>
        public void SetTypeDefinitionTypeSelected(string typeName, bool selected)
        {
            if (string.IsNullOrEmpty(typeName)) return;
            
            bool isCurrentlySelected = m_SelectedTypeDefinitionTypes.Contains(typeName);
            
            if (selected && !isCurrentlySelected)
            {
                m_SelectedTypeDefinitionTypes.Add(typeName);
            }
            else if (!selected && isCurrentlySelected)
            {
                m_SelectedTypeDefinitionTypes.Remove(typeName);
            }
        }
        
        /// <summary>
        /// 检查类型定义文件是否被选中
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <returns>是否被选中</returns>
        public bool IsTypeDefinitionFileSelected(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) && m_SelectedTypeDefinitionFiles.Contains(fileName);
        }
        
        /// <summary>
        /// 设置类型定义文件选择状态
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="selected">是否选中</param>
        public void SetTypeDefinitionFileSelected(string fileName, bool selected)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            
            bool isCurrentlySelected = m_SelectedTypeDefinitionFiles.Contains(fileName);
            
            if (selected && !isCurrentlySelected)
            {
                m_SelectedTypeDefinitionFiles.Add(fileName);
            }
            else if (!selected && isCurrentlySelected)
            {
                m_SelectedTypeDefinitionFiles.Remove(fileName);
            }
        }
        
        /// <summary>
        /// 清空类型定义类型选择
        /// </summary>
        public void ClearTypeDefinitionTypeSelection()
        {
            m_SelectedTypeDefinitionTypes.Clear();
        }
        
        /// <summary>
        /// 清空类型定义文件选择
        /// </summary>
        public void ClearTypeDefinitionFileSelection()
        {
            m_SelectedTypeDefinitionFiles.Clear();
        }
        
        #endregion
        
        private void OnValidate()
        {
            ValidateSettings();
        }
    }
}
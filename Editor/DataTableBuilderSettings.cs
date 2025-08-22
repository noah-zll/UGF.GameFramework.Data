using UnityEngine;
using System.Collections.Generic;

namespace UGF.GameFramework.Data.Editor
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
        
        private void OnValidate()
        {
            ValidateSettings();
        }
    }
}
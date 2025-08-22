using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 数据表构建器
    /// </summary>
    public static class DataTableBuilder
    {
        /// <summary>
        /// 构建配置
        /// </summary>
        [Serializable]
        public class BuildConfig
        {
            /// <summary>
            /// Excel文件路径
            /// </summary>
            public string ExcelFilePath { get; set; }
            
            /// <summary>
            /// 工作表名称（为空则使用第一个工作表）
            /// </summary>
            public string SheetName { get; set; }
            
            /// <summary>
            /// 命名空间
            /// </summary>
            public string NamespaceName { get; set; } = "GameData";
            
            /// <summary>
            /// 代码输出路径
            /// </summary>
            public string CodeOutputPath { get; set; } = "Assets/Scripts/DataTables";
            
            /// <summary>
            /// 数据输出路径
            /// </summary>
            public string DataOutputPath { get; set; } = "Assets/StreamingAssets/DataTables";
            
            /// <summary>
            /// 是否生成代码
            /// </summary>
            public bool GenerateCode { get; set; } = true;
            
            /// <summary>
            /// 是否生成数据
            /// </summary>
            public bool GenerateData { get; set; } = true;
            
            /// <summary>
            /// 是否覆盖已存在的文件
            /// </summary>
            public bool OverwriteExisting { get; set; } = true;
            
            /// <summary>
            /// 是否自动生成枚举定义
            /// </summary>
            public bool GenerateEnums { get; set; } = true;
            
            /// <summary>
            /// 枚举代码输出路径
            /// </summary>
            public string EnumOutputPath { get; set; } = "Assets/Scripts/Enums";
        }
        
        /// <summary>
        /// 构建结果
        /// </summary>
        [Serializable]
        public class BuildResult
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; set; }
            
            /// <summary>
            /// 错误信息
            /// </summary>
            public string ErrorMessage { get; set; }
            
            /// <summary>
            /// 生成的文件列表
            /// </summary>
            public List<string> GeneratedFiles { get; set; }
            
            /// <summary>
            /// 表格信息
            /// </summary>
            public ExcelTableInfo TableInfo { get; set; }
            
            public BuildResult()
            {
                GeneratedFiles = new List<string>();
            }
        }
        
        /// <summary>
        /// 构建单个数据表
        /// </summary>
        /// <param name="config">构建配置</param>
        /// <returns>构建结果</returns>
        public static BuildResult BuildDataTable(BuildConfig config)
        {
            var result = new BuildResult();
            
            try
            {
                // 验证配置
                if (!ValidateConfig(config, out string validationError))
                {
                    result.ErrorMessage = validationError;
                    return result;
                }
                
                // 解析Excel文件
                Debug.Log($"开始解析Excel文件: {config.ExcelFilePath}");
                var tableInfo = ExcelParser.ParseExcel(config.ExcelFilePath, config.SheetName);
                result.TableInfo = tableInfo;
                
                Debug.Log($"Excel解析完成，表名: {tableInfo.TableName}, 字段数: {tableInfo.Fields.Count}, 数据行数: {tableInfo.Rows.Count}");
                
                // 生成枚举定义
                if (config.GenerateEnums)
                {
                    var enumFiles = GenerateEnumFiles(tableInfo, config);
                    result.GeneratedFiles.AddRange(enumFiles);
                    if (enumFiles.Count > 0)
                    {
                        Debug.Log($"已生成 {enumFiles.Count} 个枚举定义文件");
                    }
                }
                
                // 生成代码
                if (config.GenerateCode)
                {
                    var codeFilePath = GenerateCodeFile(tableInfo, config);
                    result.GeneratedFiles.Add(codeFilePath);
                    Debug.Log($"DataRow类已生成: {codeFilePath}");
                }
                
                // 生成数据
                if (config.GenerateData)
                {
                    var dataFilePath = GenerateDataFile(tableInfo, config);
                    result.GeneratedFiles.Add(dataFilePath);
                    Debug.Log($"二进制数据已生成: {dataFilePath}");
                }
                
                result.Success = true;
                Debug.Log($"数据表构建完成: {tableInfo.TableName}");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                Debug.LogError($"构建数据表失败: {ex}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 批量构建数据表
        /// </summary>
        /// <param name="configs">构建配置列表</param>
        /// <returns>构建结果列表</returns>
        public static List<BuildResult> BuildDataTables(List<BuildConfig> configs)
        {
            var results = new List<BuildResult>();
            
            foreach (var config in configs)
            {
                var result = BuildDataTable(config);
                results.Add(result);
                
                if (!result.Success)
                {
                    Debug.LogError($"构建失败: {config.ExcelFilePath} - {result.ErrorMessage}");
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 从目录批量构建
        /// </summary>
        /// <param name="excelDirectory">Excel文件目录</param>
        /// <param name="baseConfig">基础配置</param>
        /// <returns>构建结果列表</returns>
        public static List<BuildResult> BuildFromDirectory(string excelDirectory, BuildConfig baseConfig)
        {
            var results = new List<BuildResult>();
            
            if (!Directory.Exists(excelDirectory))
            {
                Debug.LogError($"Excel目录不存在: {excelDirectory}");
                return results;
            }
            
            var excelFiles = Directory.GetFiles(excelDirectory, "*.xlsx", SearchOption.AllDirectories);
            
            foreach (var excelFile in excelFiles)
            {
                // 跳过临时文件
                if (Path.GetFileName(excelFile).StartsWith("~$"))
                    continue;
                    
                var config = new BuildConfig
                {
                    ExcelFilePath = excelFile,
                    SheetName = baseConfig.SheetName,
                    NamespaceName = baseConfig.NamespaceName,
                    CodeOutputPath = baseConfig.CodeOutputPath,
                    DataOutputPath = baseConfig.DataOutputPath,
                    GenerateCode = baseConfig.GenerateCode,
                    GenerateData = baseConfig.GenerateData,
                    OverwriteExisting = baseConfig.OverwriteExisting
                };
                
                var result = BuildDataTable(config);
                results.Add(result);
            }
            
            return results;
        }
        
        /// <summary>
        /// 验证构建配置
        /// </summary>
        private static bool ValidateConfig(BuildConfig config, out string errorMessage)
        {
            errorMessage = null;
            
            if (config == null)
            {
                errorMessage = "构建配置不能为空";
                return false;
            }
            
            if (string.IsNullOrEmpty(config.ExcelFilePath))
            {
                errorMessage = "Excel文件路径不能为空";
                return false;
            }
            
            if (!File.Exists(config.ExcelFilePath))
            {
                errorMessage = $"Excel文件不存在: {config.ExcelFilePath}";
                return false;
            }
            
            if (config.GenerateCode && string.IsNullOrEmpty(config.CodeOutputPath))
            {
                errorMessage = "代码输出路径不能为空";
                return false;
            }
            
            if (config.GenerateData && string.IsNullOrEmpty(config.DataOutputPath))
            {
                errorMessage = "数据输出路径不能为空";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 生成枚举定义文件
        /// </summary>
        private static List<string> GenerateEnumFiles(ExcelTableInfo tableInfo, BuildConfig config)
        {
            var generatedFiles = new List<string>();
            
            try
            {
                // 提取枚举信息
                var enumInfos = EnumCodeGenerator.ExtractEnumInfo(tableInfo);
                
                if (enumInfos.Count == 0)
                {
                    return generatedFiles;
                }
                
                var absoluteEnumPath = Path.GetFullPath(config.EnumOutputPath);
                
                // 确保输出目录存在
                if (!Directory.Exists(absoluteEnumPath))
                {
                    Directory.CreateDirectory(absoluteEnumPath);
                }
                
                // 生成每个枚举文件
                foreach (var kvp in enumInfos)
                {
                    var enumInfo = kvp.Value;
                    
                    // 跳过没有值的枚举
                    if (enumInfo.Values.Count == 0)
                    {
                        Debug.LogWarning($"枚举 {enumInfo.Name} 没有找到任何值，跳过生成");
                        continue;
                    }
                    
                    var fileName = $"{enumInfo.Name}.cs";
                    var filePath = Path.Combine(absoluteEnumPath, fileName);
                    
                    // 检查文件是否已存在
                    if (File.Exists(filePath) && !config.OverwriteExisting)
                    {
                        Debug.LogWarning($"枚举文件已存在且不允许覆盖: {filePath}");
                        continue;
                    }
                    
                    var code = EnumCodeGenerator.GenerateEnumCode(enumInfo, config.NamespaceName);
                    File.WriteAllText(filePath, code, System.Text.Encoding.UTF8);
                    generatedFiles.Add(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成枚举文件时发生错误: {ex.Message}");
                throw;
            }
            
            return generatedFiles;
        }
        
        /// <summary>
        /// 生成代码文件
        /// </summary>
        private static string GenerateCodeFile(ExcelTableInfo tableInfo, BuildConfig config)
        {
            var absoluteCodePath = Path.GetFullPath(config.CodeOutputPath);
            var className = $"{tableInfo.TableName}DataRow";
            var filePath = Path.Combine(absoluteCodePath, $"{className}.cs");
            
            // 检查文件是否已存在
            if (File.Exists(filePath) && !config.OverwriteExisting)
            {
                throw new InvalidOperationException($"代码文件已存在且不允许覆盖: {filePath}");
            }
            
            DataRowCodeGenerator.GenerateDataRowClass(tableInfo, config.NamespaceName, absoluteCodePath);
            return filePath;
        }
        
        /// <summary>
        /// 生成数据文件
        /// </summary>
        private static string GenerateDataFile(ExcelTableInfo tableInfo, BuildConfig config)
        {
            var absoluteDataPath = Path.GetFullPath(config.DataOutputPath);
            var fileName = $"{tableInfo.TableName}.bytes";
            var filePath = Path.Combine(absoluteDataPath, fileName);
            
            // 检查文件是否已存在
            if (File.Exists(filePath) && !config.OverwriteExisting)
            {
                throw new InvalidOperationException($"数据文件已存在且不允许覆盖: {filePath}");
            }
            
            BinaryDataSerializer.SerializeToBinary(tableInfo, absoluteDataPath);
            return filePath;
        }
        
        /// <summary>
        /// 获取构建统计信息
        /// </summary>
        /// <param name="results">构建结果列表</param>
        /// <returns>统计信息</returns>
        public static string GetBuildStatistics(List<BuildResult> results)
        {
            var totalCount = results.Count;
            var successCount = 0;
            var failureCount = 0;
            var totalFiles = 0;
            
            foreach (var result in results)
            {
                if (result.Success)
                {
                    successCount++;
                    totalFiles += result.GeneratedFiles.Count;
                }
                else
                {
                    failureCount++;
                }
            }
            
            return $"构建统计: 总数 {totalCount}, 成功 {successCount}, 失败 {failureCount}, 生成文件 {totalFiles} 个";
        }
    }
}
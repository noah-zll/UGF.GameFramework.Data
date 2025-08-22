using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 类型定义构建配置
    /// </summary>
    public class TypeDefinitionBuildConfig
    {
        /// <summary>
        /// 类型定义文件路径
        /// </summary>
        public string TypeDefinitionFilePath { get; set; }
        
        /// <summary>
        /// 代码输出目录
        /// </summary>
        public string CodeOutputDirectory { get; set; }
        
        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace { get; set; }
        
        /// <summary>
        /// 是否生成枚举
        /// </summary>
        public bool GenerateEnums { get; set; } = true;
        
        /// <summary>
        /// 是否生成类
        /// </summary>
        public bool GenerateClasses { get; set; } = true;
        
        /// <summary>
        /// 是否生成结构体
        /// </summary>
        public bool GenerateStructs { get; set; } = true;
        
        /// <summary>
        /// 是否覆盖现有文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;
    }
    
    /// <summary>
    /// 类型定义构建结果
    /// </summary>
    public class TypeDefinitionBuildResult
    {
        /// <summary>
        /// 构建是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 生成的枚举数量
        /// </summary>
        public int GeneratedEnumsCount { get; set; }
        
        /// <summary>
        /// 生成的类数量
        /// </summary>
        public int GeneratedClassesCount { get; set; }
        
        /// <summary>
        /// 生成的结构体数量
        /// </summary>
        public int GeneratedStructsCount { get; set; }
        
        /// <summary>
        /// 生成的文件列表
        /// </summary>
        public List<string> GeneratedFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// 构建耗时（毫秒）
        /// </summary>
        public long BuildTimeMs { get; set; }
    }
    
    /// <summary>
    /// 类型定义构建器
    /// </summary>
    public static class TypeDefinitionBuilder
    {
        /// <summary>
        /// 构建类型定义
        /// </summary>
        /// <param name="config">构建配置</param>
        /// <returns>构建结果</returns>
        public static TypeDefinitionBuildResult BuildTypeDefinitions(TypeDefinitionBuildConfig config)
        {
            var result = new TypeDefinitionBuildResult();
            var startTime = DateTime.Now;
            
            try
            {
                // 验证配置
                if (!ValidateConfig(config, out string validationError))
                {
                    result.ErrorMessage = validationError;
                    return result;
                }
                
                // 解析类型定义文件
                var parseResult = TypeDefinitionParser.ParseTypeDefinitionFile(config.TypeDefinitionFilePath, config.Namespace);
                if (!parseResult.Success)
                {
                    result.ErrorMessage = parseResult.ErrorMessage;
                    return result;
                }
                
                // 确保输出目录存在
                Directory.CreateDirectory(config.CodeOutputDirectory);
                
                // 生成枚举
                if (config.GenerateEnums && parseResult.Enums.Count > 0)
                {
                    result.GeneratedEnumsCount = GenerateEnums(parseResult.Enums, config, result);
                }
                
                // 生成类
                if (config.GenerateClasses && parseResult.Classes.Count > 0)
                {
                    result.GeneratedClassesCount = GenerateClasses(parseResult.Classes, config, result);
                }
                
                // 生成结构体
                if (config.GenerateStructs && parseResult.Structs.Count > 0)
                {
                    result.GeneratedStructsCount = GenerateStructs(parseResult.Structs, config, result);
                }
                
                result.Success = true;
                
                var totalGenerated = result.GeneratedEnumsCount + result.GeneratedClassesCount + result.GeneratedStructsCount;
                Debug.Log($"类型定义构建完成，共生成 {totalGenerated} 个文件（枚举: {result.GeneratedEnumsCount}, 类: {result.GeneratedClassesCount}, 结构体: {result.GeneratedStructsCount}）");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"构建类型定义时发生错误: {ex.Message}";
                Debug.LogError(result.ErrorMessage);
            }
            finally
            {
                result.BuildTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
            }
            
            return result;
        }
        
        /// <summary>
        /// 验证构建配置
        /// </summary>
        private static bool ValidateConfig(TypeDefinitionBuildConfig config, out string errorMessage)
        {
            errorMessage = null;
            
            if (config == null)
            {
                errorMessage = "构建配置不能为空";
                return false;
            }
            
            if (string.IsNullOrEmpty(config.TypeDefinitionFilePath))
            {
                errorMessage = "类型定义文件路径不能为空";
                return false;
            }
            
            if (!File.Exists(config.TypeDefinitionFilePath))
            {
                errorMessage = $"类型定义文件不存在: {config.TypeDefinitionFilePath}";
                return false;
            }
            
            if (string.IsNullOrEmpty(config.CodeOutputDirectory))
            {
                errorMessage = "代码输出目录不能为空";
                return false;
            }
            
            if (!config.GenerateEnums && !config.GenerateClasses && !config.GenerateStructs)
            {
                errorMessage = "至少需要选择生成一种类型（枚举、类或结构体）";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 生成枚举
        /// </summary>
        private static int GenerateEnums(List<EnumDefinitionInfo> enums, TypeDefinitionBuildConfig config, TypeDefinitionBuildResult result)
        {
            int successCount = 0;
            
            foreach (var enumDef in enums)
            {
                try
                {
                    var fileName = $"{enumDef.Name}.cs";
                    var filePath = Path.Combine(config.CodeOutputDirectory, fileName);
                    
                    // 检查是否覆盖现有文件
                    if (!config.OverwriteExisting && File.Exists(filePath))
                    {
                        Debug.LogWarning($"枚举文件已存在，跳过生成: {filePath}");
                        continue;
                    }
                    
                    if (EnumCodeGenerator.GenerateEnumFileFromDefinition(enumDef, config.CodeOutputDirectory, config.Namespace))
                    {
                        successCount++;
                        result.GeneratedFiles.Add(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"生成枚举 {enumDef.Name} 时发生错误: {ex.Message}");
                }
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 生成类
        /// </summary>
        private static int GenerateClasses(List<ClassDefinitionInfo> classes, TypeDefinitionBuildConfig config, TypeDefinitionBuildResult result)
        {
            int successCount = 0;
            
            foreach (var classDef in classes)
            {
                try
                {
                    var fileName = $"{classDef.Name}.cs";
                    var filePath = Path.Combine(config.CodeOutputDirectory, fileName);
                    
                    // 检查是否覆盖现有文件
                    if (!config.OverwriteExisting && File.Exists(filePath))
                    {
                        Debug.LogWarning($"类文件已存在，跳过生成: {filePath}");
                        continue;
                    }
                    
                    if (ClassCodeGenerator.GenerateClassFile(classDef, config.CodeOutputDirectory, config.Namespace))
                    {
                        successCount++;
                        result.GeneratedFiles.Add(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"生成类 {classDef.Name} 时发生错误: {ex.Message}");
                }
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 生成结构体
        /// </summary>
        private static int GenerateStructs(List<StructDefinitionInfo> structs, TypeDefinitionBuildConfig config, TypeDefinitionBuildResult result)
        {
            int successCount = 0;
            
            foreach (var structDef in structs)
            {
                try
                {
                    var fileName = $"{structDef.Name}.cs";
                    var filePath = Path.Combine(config.CodeOutputDirectory, fileName);
                    
                    // 检查是否覆盖现有文件
                    if (!config.OverwriteExisting && File.Exists(filePath))
                    {
                        Debug.LogWarning($"结构体文件已存在，跳过生成: {filePath}");
                        continue;
                    }
                    
                    if (StructCodeGenerator.GenerateStructFile(structDef, config.CodeOutputDirectory, config.Namespace))
                    {
                        successCount++;
                        result.GeneratedFiles.Add(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"生成结构体 {structDef.Name} 时发生错误: {ex.Message}");
                }
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 批量构建类型定义
        /// </summary>
        /// <param name="configs">构建配置列表</param>
        /// <returns>构建结果列表</returns>
        public static List<TypeDefinitionBuildResult> BuildTypeDefinitions(List<TypeDefinitionBuildConfig> configs)
        {
            var results = new List<TypeDefinitionBuildResult>();
            
            foreach (var config in configs)
            {
                var result = BuildTypeDefinitions(config);
                results.Add(result);
                
                if (!result.Success)
                {
                    Debug.LogError($"构建类型定义失败: {result.ErrorMessage}");
                }
            }
            
            return results;
        }
    }
}
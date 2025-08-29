using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 常量代码生成器
    /// </summary>
    public static class ConstantCodeGenerator
    {
        /// <summary>
        /// 生成常量类代码
        /// </summary>
        /// <param name="constantDefinition">常量定义信息</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成的代码</returns>
        public static string GenerateConstantCode(ConstantDefinitionInfo constantDefinition, string namespaceName = null)
        {
            var sb = new StringBuilder();
            
            // 文件头注释
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine("// 此文件由工具自动生成，请勿手动修改。");
            sb.AppendLine("// 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine();
            
            // using语句
            sb.AppendLine("using System;");
            sb.AppendLine();
            
            // 命名空间开始
            var ns = namespaceName ?? constantDefinition.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }
            
            var indent = string.IsNullOrEmpty(ns) ? "" : "    ";
            
            // 类注释
            sb.AppendLine($"{indent}/// <summary>");
            if (!string.IsNullOrEmpty(constantDefinition.Description))
            {
                sb.AppendLine($"{indent}/// {constantDefinition.Description}");
            }
            else
            {
                sb.AppendLine($"{indent}/// {constantDefinition.Name}常量定义");
            }
            sb.AppendLine($"{indent}/// </summary>");
            
            // 类定义
            sb.AppendLine($"{indent}public static class {constantDefinition.Name}");
            sb.AppendLine($"{indent}{{");
            
            // 按分类分组常量
            var groupedConstants = new Dictionary<string, List<ConstantInfo>>();
            foreach (var constant in constantDefinition.Constants)
            {
                var category = string.IsNullOrEmpty(constant.Category) ? "Default" : constant.Category;
                if (!groupedConstants.ContainsKey(category))
                {
                    groupedConstants[category] = new List<ConstantInfo>();
                }
                groupedConstants[category].Add(constant);
            }
            
            bool isFirstCategory = true;
            foreach (var kvp in groupedConstants)
            {
                var category = kvp.Key;
                var constants = kvp.Value;
                
                // 添加分类注释（如果不是默认分类）
                if (!isFirstCategory)
                {
                    sb.AppendLine();
                }
                
                if (category != "Default")
                {
                    sb.AppendLine($"{indent}    #region {category}");
                    sb.AppendLine();
                }
                
                // 生成常量
                for (int i = 0; i < constants.Count; i++)
                {
                    var constant = constants[i];
                    
                    // 常量注释
                    if (!string.IsNullOrEmpty(constant.Description))
                    {
                        sb.AppendLine($"{indent}    /// <summary>");
                        sb.AppendLine($"{indent}    /// {constant.Description}");
                        sb.AppendLine($"{indent}    /// </summary>");
                    }
                    
                    // 常量定义
                    var constantValue = FormatConstantValue(constant.Type, constant.Value);
                    sb.AppendLine($"{indent}    public const {constant.Type} {constant.Name} = {constantValue};");
                    
                    if (i < constants.Count - 1)
                    {
                        sb.AppendLine();
                    }
                }
                
                if (category != "Default")
                {
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    #endregion");
                }
                
                isFirstCategory = false;
            }
            
            sb.AppendLine($"{indent}}}");
            
            // 命名空间结束
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化常量值
        /// </summary>
        /// <param name="type">常量类型</param>
        /// <param name="value">常量值</param>
        /// <returns>格式化后的值</returns>
        private static string FormatConstantValue(string type, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetDefaultValue(type);
            }
            
            switch (type.ToLower())
            {
                case "string":
                    return $"\"{ value.Replace("\"", "\\\"") }\"";
                case "char":
                    return $"'{value}'";
                case "float":
                    if (!value.EndsWith("f") && !value.EndsWith("F"))
                    {
                        return value + "f";
                    }
                    return value;
                case "double":
                    if (!value.EndsWith("d") && !value.EndsWith("D"))
                    {
                        return value + "d";
                    }
                    return value;
                case "decimal":
                    if (!value.EndsWith("m") && !value.EndsWith("M"))
                    {
                        return value + "m";
                    }
                    return value;
                case "long":
                    if (!value.EndsWith("L"))
                    {
                        return value + "L";
                    }
                    return value;
                case "uint":
                    if (!value.EndsWith("u") && !value.EndsWith("U"))
                    {
                        return value + "u";
                    }
                    return value;
                case "ulong":
                    if (!value.EndsWith("UL") && !value.EndsWith("ul"))
                    {
                        return value + "UL";
                    }
                    return value;
                case "bool":
                    return value.ToLower() == "true" || value == "1" ? "true" : "false";
                default:
                    return value;
            }
        }
        
        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>默认值</returns>
        private static string GetDefaultValue(string type)
        {
            switch (type.ToLower())
            {
                case "string":
                    return "\"\"";
                case "char":
                    return "'\\0'";
                case "int":
                case "short":
                case "byte":
                case "sbyte":
                    return "0";
                case "uint":
                case "ushort":
                    return "0u";
                case "long":
                    return "0L";
                case "ulong":
                    return "0UL";
                case "float":
                    return "0.0f";
                case "double":
                    return "0.0d";
                case "decimal":
                    return "0.0m";
                case "bool":
                    return "false";
                default:
                    return "default";
            }
        }
        
        /// <summary>
        /// 生成常量类文件
        /// </summary>
        /// <param name="constantDefinition">常量定义信息</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成是否成功</returns>
        public static bool GenerateConstantFile(ConstantDefinitionInfo constantDefinition, string outputDirectory, string namespaceName = null)
        {
            try
            {
                var code = GenerateConstantCode(constantDefinition, namespaceName);
                var fileName = $"{constantDefinition.Name}.cs";
                var filePath = Path.Combine(outputDirectory, fileName);
                
                // 确保输出目录存在
                Directory.CreateDirectory(outputDirectory);
                
                File.WriteAllText(filePath, code, Encoding.UTF8);
                Debug.Log($"成功生成常量文件: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成常量文件失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 批量生成常量类文件
        /// </summary>
        /// <param name="constantDefinitions">常量定义信息列表</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成成功的数量</returns>
        public static int GenerateConstantFiles(List<ConstantDefinitionInfo> constantDefinitions, string outputDirectory, string namespaceName = null)
        {
            int successCount = 0;
            
            foreach (var constantDefinition in constantDefinitions)
            {
                if (GenerateConstantFile(constantDefinition, outputDirectory, namespaceName))
                {
                    successCount++;
                }
            }
            
            Debug.Log($"批量生成常量文件完成，成功: {successCount}/{constantDefinitions.Count}");
            return successCount;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 枚举代码生成器
    /// </summary>
    public static class EnumCodeGenerator
    {
        /// <summary>
        /// 枚举信息
        /// </summary>
        public class EnumInfo
        {
            public string Name { get; set; }
            public List<string> Values { get; set; } = new List<string>();
            public Dictionary<string, int> ValueIndexMap { get; set; } = new Dictionary<string, int>();
        }
        
        /// <summary>
        /// 从Excel表格信息中提取枚举信息
        /// </summary>
        /// <param name="tableInfo">表格信息</param>
        /// <returns>枚举信息字典</returns>
        public static Dictionary<string, EnumInfo> ExtractEnumInfo(ExcelTableInfo tableInfo)
        {
            var enumInfos = new Dictionary<string, EnumInfo>();
            
            // 遍历所有字段，找出枚举类型
            foreach (var field in tableInfo.Fields)
            {
                if (SupportedDataTypes.IsEnumType(field.Type))
                {
                    var enumTypeName = SupportedDataTypes.GetEnumTypeName(field.Type);
                    if (string.IsNullOrEmpty(enumTypeName))
                        continue;
                        
                    if (!enumInfos.ContainsKey(enumTypeName))
                    {
                        enumInfos[enumTypeName] = new EnumInfo { Name = enumTypeName };
                    }
                    
                    var enumInfo = enumInfos[enumTypeName];
                    
                    // 收集该字段的所有枚举值
                    foreach (var row in tableInfo.Rows)
                    {
                        if (row.ContainsKey(field.Name))
                        {
                            var value = row[field.Name]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(value))
                            {
                                // 如果是数字，跳过（数字值不需要生成枚举定义）
                                if (int.TryParse(value, out _))
                                    continue;
                                    
                                if (!enumInfo.Values.Contains(value))
                                {
                                    enumInfo.Values.Add(value);
                                    enumInfo.ValueIndexMap[value] = enumInfo.Values.Count - 1;
                                }
                            }
                        }
                    }
                }
            }
            
            return enumInfos;
        }
        
        /// <summary>
        /// 生成枚举定义代码
        /// </summary>
        /// <param name="enumInfo">枚举信息</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成的代码</returns>
        public static string GenerateEnumCode(EnumInfo enumInfo, string namespaceName = null)
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
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
            
            // 枚举注释
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {enumInfo.Name}枚举");
            sb.AppendLine($"    /// </summary>");
            
            // 枚举定义
            sb.AppendLine($"    public enum {enumInfo.Name}");
            sb.AppendLine("    {");
            
            // 枚举值
            for (int i = 0; i < enumInfo.Values.Count; i++)
            {
                var value = enumInfo.Values[i];
                var comment = $"/// <summary>{value}</summary>";
                
                sb.AppendLine($"        {comment}");
                sb.AppendLine($"        {value} = {i}{(i < enumInfo.Values.Count - 1 ? "," : "")}");
                
                if (i < enumInfo.Values.Count - 1)
                {
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("    }");
            
            // 命名空间结束
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 生成所有枚举定义文件
        /// </summary>
        /// <param name="enumInfos">枚举信息字典</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        public static void GenerateEnumFiles(Dictionary<string, EnumInfo> enumInfos, string outputDirectory, string namespaceName = null)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            
            foreach (var kvp in enumInfos)
            {
                var enumInfo = kvp.Value;
                
                // 跳过没有值的枚举
                if (enumInfo.Values.Count == 0)
                {
                    Debug.LogWarning($"枚举 {enumInfo.Name} 没有找到任何值，跳过生成");
                    continue;
                }
                
                var code = GenerateEnumCode(enumInfo, namespaceName);
                var fileName = $"{enumInfo.Name}.cs";
                var filePath = Path.Combine(outputDirectory, fileName);
                
                File.WriteAllText(filePath, code, Encoding.UTF8);
                Debug.Log($"已生成枚举文件: {filePath}");
            }
        }
        
        /// <summary>
        /// 从类型定义信息生成枚举代码
        /// </summary>
        /// <param name="enumDefinition">枚举定义信息</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成的代码</returns>
        public static string GenerateEnumCodeFromDefinition(EnumDefinitionInfo enumDefinition, string namespaceName = null)
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
            var ns = namespaceName ?? enumDefinition.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }
            
            var indent = string.IsNullOrEmpty(ns) ? "" : "    ";
            
            // 枚举注释
            sb.AppendLine($"{indent}/// <summary>");
            if (!string.IsNullOrEmpty(enumDefinition.Description))
            {
                sb.AppendLine($"{indent}/// {enumDefinition.Description}");
            }
            else
            {
                sb.AppendLine($"{indent}/// {enumDefinition.Name}枚举");
            }
            sb.AppendLine($"{indent}/// </summary>");
            
            // 枚举定义
            sb.AppendLine($"{indent}public enum {enumDefinition.Name}");
            sb.AppendLine($"{indent}{{");
            
            // 枚举值
            for (int i = 0; i < enumDefinition.Values.Count; i++)
            {
                var enumValue = enumDefinition.Values[i];
                
                // 枚举值注释
                if (!string.IsNullOrEmpty(enumValue.Description))
                {
                    sb.AppendLine($"{indent}    /// <summary>");
                    sb.AppendLine($"{indent}    /// {enumValue.Description}");
                    sb.AppendLine($"{indent}    /// </summary>");
                }
                
                // 枚举值定义
                var valueDeclaration = $"{indent}    {enumValue.Name}";
                if (enumValue.Value.HasValue)
                {
                    valueDeclaration += $" = {enumValue.Value.Value}";
                }
                
                if (i < enumDefinition.Values.Count - 1)
                {
                    valueDeclaration += ",";
                }
                
                sb.AppendLine(valueDeclaration);
                
                if (i < enumDefinition.Values.Count - 1)
                {
                    sb.AppendLine();
                }
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
        /// 生成枚举定义文件（从类型定义）
        /// </summary>
        /// <param name="enumDefinition">枚举定义信息</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成是否成功</returns>
        public static bool GenerateEnumFileFromDefinition(EnumDefinitionInfo enumDefinition, string outputDirectory, string namespaceName = null)
        {
            try
            {
                var code = GenerateEnumCodeFromDefinition(enumDefinition, namespaceName);
                var fileName = $"{enumDefinition.Name}.cs";
                var filePath = Path.Combine(outputDirectory, fileName);
                
                // 确保输出目录存在
                Directory.CreateDirectory(outputDirectory);
                
                File.WriteAllText(filePath, code, Encoding.UTF8);
                Debug.Log($"成功生成枚举文件: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成枚举文件失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 批量生成枚举定义文件（从类型定义）
        /// </summary>
        /// <param name="enumDefinitions">枚举定义信息列表</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成成功的数量</returns>
        public static int GenerateEnumFilesFromDefinitions(List<EnumDefinitionInfo> enumDefinitions, string outputDirectory, string namespaceName = null)
        {
            int successCount = 0;
            
            foreach (var enumDefinition in enumDefinitions)
            {
                if (GenerateEnumFileFromDefinition(enumDefinition, outputDirectory, namespaceName))
                {
                    successCount++;
                }
            }
            
            Debug.Log($"批量生成枚举文件完成，成功: {successCount}/{enumDefinitions.Count}");
            return successCount;
        }
        
        /// <summary>
        /// 从类型定义表生成枚举定义
        /// </summary>
        /// <param name="typeDefinitionFilePath">类型定义表文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成成功的数量</returns>
        public static int GenerateEnumsFromTypeDefinition(string typeDefinitionFilePath, string outputDirectory, string namespaceName = null)
        {
            try
            {
                var parseResult = TypeDefinitionParser.ParseTypeDefinitionFile(typeDefinitionFilePath, namespaceName);
                
                if (!parseResult.Success)
                {
                    Debug.LogError($"解析类型定义文件失败: {parseResult.ErrorMessage}");
                    return 0;
                }
                
                if (parseResult.Enums.Count == 0)
                {
                    Debug.LogWarning($"在类型定义文件 {typeDefinitionFilePath} 中没有找到任何枚举定义");
                    return 0;
                }
                
                var successCount = GenerateEnumFilesFromDefinitions(parseResult.Enums, outputDirectory, namespaceName);
                Debug.Log($"从类型定义文件 {typeDefinitionFilePath} 成功生成了 {successCount} 个枚举定义");
                return successCount;
            }
            catch (Exception ex)
            {
                Debug.LogError($"从类型定义文件生成枚举定义时发生错误: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 从Excel文件生成枚举定义
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <param name="sheetName">工作表名称</param>
        public static void GenerateEnumsFromExcel(string excelFilePath, string outputDirectory, string namespaceName = null, string sheetName = null)
        {
            try
            {
                var tableInfo = ExcelParser.ParseExcel(excelFilePath, sheetName);
                var enumInfos = ExtractEnumInfo(tableInfo);
                
                if (enumInfos.Count == 0)
                {
                    Debug.LogWarning($"在Excel文件 {excelFilePath} 中没有找到任何枚举类型字段");
                    return;
                }
                
                GenerateEnumFiles(enumInfos, outputDirectory, namespaceName);
                Debug.Log($"从Excel文件 {excelFilePath} 成功生成了 {enumInfos.Count} 个枚举定义");
            }
            catch (Exception ex)
            {
                Debug.LogError($"从Excel文件生成枚举定义时发生错误: {ex.Message}");
                throw;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 结构体代码生成器
    /// </summary>
    public static class StructCodeGenerator
    {
        /// <summary>
        /// 生成结构体代码文件
        /// </summary>
        /// <param name="structInfo">结构体定义信息</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成是否成功</returns>
        public static bool GenerateStructFile(StructDefinitionInfo structInfo, string outputDirectory, string namespaceName = null)
        {
            try
            {
                var code = GenerateStructCode(structInfo, namespaceName ?? structInfo.Namespace);
                var fileName = $"{structInfo.Name}.cs";
                var filePath = Path.Combine(outputDirectory, fileName);
                
                // 确保输出目录存在
                Directory.CreateDirectory(outputDirectory);
                
                File.WriteAllText(filePath, code, Encoding.UTF8);
                Debug.Log($"成功生成结构体文件: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成结构体文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量生成结构体代码文件
        /// </summary>
        /// <param name="structInfos">结构体定义信息列表</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成成功的数量</returns>
        public static int GenerateStructFiles(List<StructDefinitionInfo> structInfos, string outputDirectory, string namespaceName = null)
        {
            int successCount = 0;
            
            foreach (var structInfo in structInfos)
            {
                if (GenerateStructFile(structInfo, outputDirectory, namespaceName))
                {
                    successCount++;
                }
            }
            
            Debug.Log($"批量生成结构体文件完成，成功: {successCount}/{structInfos.Count}");
            return successCount;
        }

        /// <summary>
        /// 生成结构体代码
        /// </summary>
        /// <param name="structInfo">结构体定义信息</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成的代码</returns>
        public static string GenerateStructCode(StructDefinitionInfo structInfo, string namespaceName = null)
        {
            var sb = new StringBuilder();
            var usings = GetRequiredUsings(structInfo);
            
            // 添加using语句
            foreach (var usingStatement in usings.OrderBy(u => u))
            {
                sb.AppendLine($"using {usingStatement};");
            }
            
            if (usings.Any())
            {
                sb.AppendLine();
            }
            
            // 开始命名空间
            var ns = namespaceName ?? structInfo.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }
            
            // 生成结构体定义
            GenerateStructDefinition(sb, structInfo, string.IsNullOrEmpty(ns) ? 0 : 1);
            
            // 结束命名空间
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 生成结构体定义
        /// </summary>
        private static void GenerateStructDefinition(StringBuilder sb, StructDefinitionInfo structInfo, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            // 生成结构体注释
            if (!string.IsNullOrEmpty(structInfo.Description))
            {
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// {structInfo.Description}");
                sb.AppendLine($"{indent}/// </summary>");
            }
            
            // 生成结构体声明
            sb.AppendLine($"{indent}public struct {structInfo.Name}");
            sb.AppendLine($"{indent}{{");
            
            // 生成字段
            GenerateFields(sb, structInfo.Fields, indentLevel + 1);
            
            // 生成构造函数
            GenerateConstructor(sb, structInfo, indentLevel + 1);
            
            // 生成ToString方法
            GenerateToStringMethod(sb, structInfo, indentLevel + 1);
            
            // 生成Equals和GetHashCode方法
            GenerateEqualsAndHashCode(sb, structInfo, indentLevel + 1);
            
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 生成字段
        /// </summary>
        private static void GenerateFields(StringBuilder sb, List<FieldInfo> fields, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                
                // 添加空行分隔（除了第一个字段）
                if (i > 0)
                {
                    sb.AppendLine();
                }
                
                // 生成字段注释
                if (!string.IsNullOrEmpty(field.Description))
                {
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {field.Description}");
                    sb.AppendLine($"{indent}/// </summary>");
                }
                
                // 生成字段声明
                var fieldType = field.IsArray ? $"{field.Type}[]" : field.Type;
                var fieldDeclaration = $"{indent}public {fieldType} {field.Name}";
                
                // 添加默认值（如果有）
                if (!string.IsNullOrEmpty(field.DefaultValue))
                {
                    fieldDeclaration += $" = {field.DefaultValue}";
                }
                
                fieldDeclaration += ";";
                sb.AppendLine(fieldDeclaration);
            }
        }

        /// <summary>
        /// 生成构造函数
        /// </summary>
        private static void GenerateConstructor(StringBuilder sb, StructDefinitionInfo structInfo, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 构造函数");
            sb.AppendLine($"{indent}/// </summary>");
            
            // 生成参数列表
            var parameters = new List<string>();
            foreach (var field in structInfo.Fields)
            {
                var fieldType = field.IsArray ? $"{field.Type}[]" : field.Type;
                parameters.Add($"{fieldType} {ToCamelCase(field.Name)}");
            }
            
            sb.AppendLine($"{indent}public {structInfo.Name}({string.Join(", ", parameters)})");
            sb.AppendLine($"{indent}{{");
            
            // 生成字段赋值
            foreach (var field in structInfo.Fields)
            {
                sb.AppendLine($"{indent}    this.{field.Name} = {ToCamelCase(field.Name)};");
            }
            
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 生成ToString方法
        /// </summary>
        private static void GenerateToStringMethod(StringBuilder sb, StructDefinitionInfo structInfo, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 返回结构体的字符串表示");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public override string ToString()");
            sb.AppendLine($"{indent}{{");
            
            // 生成字符串格式
            var fieldFormats = new List<string>();
            foreach (var field in structInfo.Fields)
            {
                if (field.IsArray)
                {
                    fieldFormats.Add($"{field.Name}: [{{string.Join(\", \", {field.Name} ?? new {field.Type}[0])}}]");
                }
                else
                {
                    fieldFormats.Add($"{field.Name}: {{{field.Name}}}");
                }
            }
            
            var formatString = $"{structInfo.Name} {{ {string.Join(", ", fieldFormats)} }}";
            sb.AppendLine($"{indent}    return $\"{formatString}\";");
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 生成Equals和GetHashCode方法
        /// </summary>
        private static void GenerateEqualsAndHashCode(StringBuilder sb, StructDefinitionInfo structInfo, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            // Equals方法
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 比较两个结构体是否相等");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public override bool Equals(object obj)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if (obj is {structInfo.Name} other)");
            sb.AppendLine($"{indent}    {{");
            
            // 生成字段比较
            var comparisons = new List<string>();
            foreach (var field in structInfo.Fields)
            {
                if (field.IsArray)
                {
                    comparisons.Add($"({field.Name}?.SequenceEqual(other.{field.Name}) ?? other.{field.Name} == null)");
                }
                else
                {
                    comparisons.Add($"{field.Name}.Equals(other.{field.Name})");
                }
            }
            
            if (comparisons.Any())
            {
                sb.AppendLine($"{indent}        return {string.Join(" && ", comparisons)};");
            }
            else
            {
                sb.AppendLine($"{indent}        return true;");
            }
            
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    return false;");
            sb.AppendLine($"{indent}}}");
            
            // GetHashCode方法
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 获取哈希码");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public override int GetHashCode()");
            sb.AppendLine($"{indent}{{");
            
            if (structInfo.Fields.Any())
            {
                sb.AppendLine($"{indent}    return HashCode.Combine(");
                var hashFields = new List<string>();
                foreach (var field in structInfo.Fields.Take(8)) // HashCode.Combine最多支持8个参数
                {
                    if (field.IsArray)
                    {
                        hashFields.Add($"        {field.Name}?.GetHashCode() ?? 0");
                    }
                    else
                    {
                        hashFields.Add($"        {field.Name}");
                    }
                }
                sb.AppendLine(string.Join(",\n", hashFields));
                sb.AppendLine($"{indent}    );");
            }
            else
            {
                sb.AppendLine($"{indent}    return 0;");
            }
            
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 获取需要的using语句
        /// </summary>
        private static HashSet<string> GetRequiredUsings(StructDefinitionInfo structInfo)
        {
            var usings = new HashSet<string>
            {
                "System"
            };
            
            // 检查是否需要Linq（用于数组比较）
            if (structInfo.Fields.Any(f => f.IsArray))
            {
                usings.Add("System.Linq");
            }
            
            // 检查字段类型是否需要特殊的using
            foreach (var field in structInfo.Fields)
            {
                AddUsingsForType(usings, field.Type);
            }
            
            return usings;
        }

        /// <summary>
        /// 为特定类型添加using语句
        /// </summary>
        private static void AddUsingsForType(HashSet<string> usings, string typeName)
        {
            switch (typeName.ToLower())
            {
                case "datetime":
                    // DateTime在System中，已经包含
                    break;
                case "list":
                case "dictionary":
                case "hashset":
                    usings.Add("System.Collections.Generic");
                    break;
                case "ienumerable":
                case "icollection":
                case "ilist":
                    usings.Add("System.Collections.Generic");
                    break;
                default:
                    // 对于自定义类型，不需要额外的using
                    break;
            }
        }

        /// <summary>
        /// 转换为驼峰命名
        /// </summary>
        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }
    }
}
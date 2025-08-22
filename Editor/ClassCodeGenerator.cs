using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 类代码生成器
    /// </summary>
    public static class ClassCodeGenerator
    {
        /// <summary>
        /// 生成类代码文件
        /// </summary>
        /// <param name="classInfo">类定义信息</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成是否成功</returns>
        public static bool GenerateClassFile(ClassDefinitionInfo classInfo, string outputDirectory, string namespaceName = null)
        {
            try
            {
                var code = GenerateClassCode(classInfo, namespaceName ?? classInfo.Namespace);
                var fileName = $"{classInfo.Name}.cs";
                var filePath = Path.Combine(outputDirectory, fileName);
                
                // 确保输出目录存在
                Directory.CreateDirectory(outputDirectory);
                
                File.WriteAllText(filePath, code, Encoding.UTF8);
                Debug.Log($"成功生成类文件: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成类文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量生成类代码文件
        /// </summary>
        /// <param name="classInfos">类定义信息列表</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成成功的数量</returns>
        public static int GenerateClassFiles(List<ClassDefinitionInfo> classInfos, string outputDirectory, string namespaceName = null)
        {
            int successCount = 0;
            
            foreach (var classInfo in classInfos)
            {
                if (GenerateClassFile(classInfo, outputDirectory, namespaceName))
                {
                    successCount++;
                }
            }
            
            Debug.Log($"批量生成类文件完成，成功: {successCount}/{classInfos.Count}");
            return successCount;
        }

        /// <summary>
        /// 生成类代码
        /// </summary>
        /// <param name="classInfo">类定义信息</param>
        /// <param name="namespaceName">命名空间</param>
        /// <returns>生成的代码</returns>
        public static string GenerateClassCode(ClassDefinitionInfo classInfo, string namespaceName = null)
        {
            var sb = new StringBuilder();
            var usings = GetRequiredUsings(classInfo);
            
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
            var ns = namespaceName ?? classInfo.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }
            
            // 生成类定义
            GenerateClassDefinition(sb, classInfo, string.IsNullOrEmpty(ns) ? 0 : 1);
            
            // 结束命名空间
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 生成类定义
        /// </summary>
        private static void GenerateClassDefinition(StringBuilder sb, ClassDefinitionInfo classInfo, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            // 生成类注释
            if (!string.IsNullOrEmpty(classInfo.Description))
            {
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// {classInfo.Description}");
                sb.AppendLine($"{indent}/// </summary>");
            }
            
            // 生成类声明
            var classDeclaration = $"{indent}public class {classInfo.Name}";
            
            // 添加继承
            var inheritance = new List<string>();
            if (!string.IsNullOrEmpty(classInfo.BaseClass))
            {
                inheritance.Add(classInfo.BaseClass);
            }
            inheritance.AddRange(classInfo.Interfaces);
            
            if (inheritance.Any())
            {
                classDeclaration += $" : {string.Join(", ", inheritance)}";
            }
            
            sb.AppendLine(classDeclaration);
            sb.AppendLine($"{indent}{{");
            
            // 生成属性
            GenerateProperties(sb, classInfo.Properties, indentLevel + 1);
            
            // 生成构造函数（如果有默认值）
            GenerateConstructor(sb, classInfo, indentLevel + 1);
            
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 生成属性
        /// </summary>
        private static void GenerateProperties(StringBuilder sb, List<PropertyInfo> properties, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                
                // 添加空行分隔（除了第一个属性）
                if (i > 0)
                {
                    sb.AppendLine();
                }
                
                // 生成属性注释
                if (!string.IsNullOrEmpty(property.Description))
                {
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {property.Description}");
                    sb.AppendLine($"{indent}/// </summary>");
                }
                
                // 生成特性
                if (!string.IsNullOrEmpty(property.Attributes))
                {
                    sb.AppendLine($"{indent}{property.Attributes}");
                }
                
                // 生成属性声明
                var propertyType = property.IsArray ? $"{property.Type}[]" : property.Type;
                var propertyDeclaration = $"{indent}public {propertyType} {property.Name} {{ get; set; }}";
                
                // 添加默认值
                if (!string.IsNullOrEmpty(property.DefaultValue))
                {
                    propertyDeclaration += $" = {property.DefaultValue}";
                }
                
                propertyDeclaration += ";";
                sb.AppendLine(propertyDeclaration);
            }
        }

        /// <summary>
        /// 生成构造函数
        /// </summary>
        private static void GenerateConstructor(StringBuilder sb, ClassDefinitionInfo classInfo, int indentLevel)
        {
            var propertiesWithDefaults = classInfo.Properties.Where(p => !string.IsNullOrEmpty(p.DefaultValue)).ToList();
            
            // 如果没有默认值，不生成构造函数
            if (!propertiesWithDefaults.Any())
                return;
            
            var indent = new string(' ', indentLevel * 4);
            
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 构造函数");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public {classInfo.Name}()");
            sb.AppendLine($"{indent}{{");
            
            foreach (var property in propertiesWithDefaults)
            {
                sb.AppendLine($"{indent}    {property.Name} = {property.DefaultValue};");
            }
            
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 获取需要的using语句
        /// </summary>
        private static HashSet<string> GetRequiredUsings(ClassDefinitionInfo classInfo)
        {
            var usings = new HashSet<string>
            {
                "System"
            };
            
            // 检查属性类型是否需要特殊的using
            foreach (var property in classInfo.Properties)
            {
                AddUsingsForType(usings, property.Type);
                
                // 检查特性
                if (!string.IsNullOrEmpty(property.Attributes))
                {
                    if (property.Attributes.Contains("[Key]") || property.Attributes.Contains("[Required]"))
                    {
                        usings.Add("System.ComponentModel.DataAnnotations");
                    }
                }
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
    }
}
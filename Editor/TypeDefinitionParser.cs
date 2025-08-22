using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEngine;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 类型定义信息
    /// </summary>
    public class TypeDefinitionInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Namespace { get; set; }
    }

    /// <summary>
    /// 枚举定义信息
    /// </summary>
    public class EnumDefinitionInfo : TypeDefinitionInfo
    {
        public List<EnumValueInfo> Values { get; set; } = new List<EnumValueInfo>();
    }

    /// <summary>
    /// 枚举值信息
    /// </summary>
    public class EnumValueInfo
    {
        public string Name { get; set; }
        public int? Value { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
    }

    /// <summary>
    /// 类定义信息
    /// </summary>
    public class ClassDefinitionInfo : TypeDefinitionInfo
    {
        public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>();
        public string BaseClass { get; set; }
        public List<string> Interfaces { get; set; } = new List<string>();
    }

    /// <summary>
    /// 属性信息
    /// </summary>
    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsArray { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public string Attributes { get; set; }
    }

    /// <summary>
    /// 结构体定义信息
    /// </summary>
    public class StructDefinitionInfo : TypeDefinitionInfo
    {
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
    }

    /// <summary>
    /// 字段信息
    /// </summary>
    public class FieldInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsArray { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// 类型定义解析结果
    /// </summary>
    public class TypeDefinitionParseResult
    {
        public List<EnumDefinitionInfo> Enums { get; set; } = new List<EnumDefinitionInfo>();
        public List<ClassDefinitionInfo> Classes { get; set; } = new List<ClassDefinitionInfo>();
        public List<StructDefinitionInfo> Structs { get; set; } = new List<StructDefinitionInfo>();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 类型定义表解析器
    /// </summary>
    public static class TypeDefinitionParser
    {
        /// <summary>
        /// 解析类型定义Excel文件
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="defaultNamespace">默认命名空间</param>
        /// <returns>解析结果</returns>
        public static TypeDefinitionParseResult ParseTypeDefinitionFile(string filePath, string defaultNamespace = "")
        {
            var result = new TypeDefinitionParseResult();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = $"文件不存在: {filePath}";
                    return result;
                }

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // 解析枚举定义
                    ParseEnumDefinitions(package, result, defaultNamespace);
                    
                    // 解析类定义
                    ParseClassDefinitions(package, result, defaultNamespace);
                    
                    // 解析结构体定义
                    ParseStructDefinitions(package, result, defaultNamespace);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"解析类型定义文件时发生错误: {ex.Message}";
                Debug.LogError(result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// 解析枚举定义
        /// </summary>
        private static void ParseEnumDefinitions(ExcelPackage package, TypeDefinitionParseResult result, string defaultNamespace)
        {
            var worksheet = FindWorksheet(package, "Enums", "EnumDefinitions");
            if (worksheet == null) return;

            var enumDict = new Dictionary<string, EnumDefinitionInfo>();
            
            // 查找表头
            var headers = FindHeaders(worksheet, new[] { "TypeName", "ValueName", "Value", "Description", "Category" });
            if (headers["TypeName"] == -1 || headers["ValueName"] == -1)
            {
                Debug.LogWarning("枚举定义表缺少必要的列: TypeName, ValueName");
                return;
            }

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            for (int row = 4; row <= rowCount; row++) // 从第4行开始，跳过表头和类型说明行
            {
                var typeName = GetCellValue(worksheet, row, headers["TypeName"]);
                var valueName = GetCellValue(worksheet, row, headers["ValueName"]);
                
                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(valueName))
                    continue;

                // 获取或创建枚举定义
                if (!enumDict.ContainsKey(typeName))
                {
                    enumDict[typeName] = new EnumDefinitionInfo
                    {
                        Name = typeName,
                        Namespace = defaultNamespace
                    };
                }

                var enumDef = enumDict[typeName];
                var enumValue = new EnumValueInfo
                {
                    Name = valueName,
                    Description = GetCellValue(worksheet, row, headers["Description"]),
                    Category = GetCellValue(worksheet, row, headers["Category"])
                };

                // 解析枚举值
                var valueStr = GetCellValue(worksheet, row, headers["Value"]);
                if (!string.IsNullOrEmpty(valueStr) && int.TryParse(valueStr, out int value))
                {
                    enumValue.Value = value;
                }

                enumDef.Values.Add(enumValue);
            }

            result.Enums.AddRange(enumDict.Values);
        }

        /// <summary>
        /// 解析类定义
        /// </summary>
        private static void ParseClassDefinitions(ExcelPackage package, TypeDefinitionParseResult result, string defaultNamespace)
        {
            var worksheet = FindWorksheet(package, "Classes", "ClassDefinitions");
            if (worksheet == null) return;

            var classDict = new Dictionary<string, ClassDefinitionInfo>();
            
            // 查找表头
            var headers = FindHeaders(worksheet, new[] { "ClassName", "PropertyName", "PropertyType", "IsArray", "Description", "DefaultValue", "Attributes" });
            if (headers["ClassName"] == -1 || headers["PropertyName"] == -1 || headers["PropertyType"] == -1)
            {
                Debug.LogWarning("类定义表缺少必要的列: ClassName, PropertyName, PropertyType");
                return;
            }

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            for (int row = 4; row <= rowCount; row++) // 从第4行开始，跳过表头和类型说明行
            {
                var className = GetCellValue(worksheet, row, headers["ClassName"]);
                var propertyName = GetCellValue(worksheet, row, headers["PropertyName"]);
                var propertyType = GetCellValue(worksheet, row, headers["PropertyType"]);
                
                if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyType))
                    continue;

                // 获取或创建类定义
                if (!classDict.ContainsKey(className))
                {
                    classDict[className] = new ClassDefinitionInfo
                    {
                        Name = className,
                        Namespace = defaultNamespace
                    };
                }

                var classDef = classDict[className];
                var property = new PropertyInfo
                {
                    Name = propertyName,
                    Type = propertyType,
                    Description = GetCellValue(worksheet, row, headers["Description"]),
                    DefaultValue = GetCellValue(worksheet, row, headers["DefaultValue"]),
                    Attributes = GetCellValue(worksheet, row, headers["Attributes"])
                };

                // 解析是否为数组
                var isArrayStr = GetCellValue(worksheet, row, headers["IsArray"]);
                property.IsArray = !string.IsNullOrEmpty(isArrayStr) && 
                                 (isArrayStr.ToLower() == "true" || isArrayStr == "1" || isArrayStr.ToLower() == "yes");

                classDef.Properties.Add(property);
            }

            result.Classes.AddRange(classDict.Values);
        }

        /// <summary>
        /// 解析结构体定义
        /// </summary>
        private static void ParseStructDefinitions(ExcelPackage package, TypeDefinitionParseResult result, string defaultNamespace)
        {
            var worksheet = FindWorksheet(package, "Structs", "StructDefinitions");
            if (worksheet == null) return;

            var structDict = new Dictionary<string, StructDefinitionInfo>();
            
            // 查找表头
            var headers = FindHeaders(worksheet, new[] { "StructName", "FieldName", "FieldType", "IsArray", "Description", "DefaultValue" });
            if (headers["StructName"] == -1 || headers["FieldName"] == -1 || headers["FieldType"] == -1)
            {
                Debug.LogWarning("结构体定义表缺少必要的列: StructName, FieldName, FieldType");
                return;
            }

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            for (int row = 4; row <= rowCount; row++) // 从第4行开始，跳过表头和类型说明行
            {
                var structName = GetCellValue(worksheet, row, headers["StructName"]);
                var fieldName = GetCellValue(worksheet, row, headers["FieldName"]);
                var fieldType = GetCellValue(worksheet, row, headers["FieldType"]);
                
                if (string.IsNullOrEmpty(structName) || string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(fieldType))
                    continue;

                // 获取或创建结构体定义
                if (!structDict.ContainsKey(structName))
                {
                    structDict[structName] = new StructDefinitionInfo
                    {
                        Name = structName,
                        Namespace = defaultNamespace
                    };
                }

                var structDef = structDict[structName];
                var field = new FieldInfo
                {
                    Name = fieldName,
                    Type = fieldType,
                    Description = GetCellValue(worksheet, row, headers["Description"]),
                    DefaultValue = GetCellValue(worksheet, row, headers["DefaultValue"])
                };

                // 解析是否为数组
                var isArrayStr = GetCellValue(worksheet, row, headers["IsArray"]);
                field.IsArray = !string.IsNullOrEmpty(isArrayStr) && 
                              (isArrayStr.ToLower() == "true" || isArrayStr == "1" || isArrayStr.ToLower() == "yes");

                structDef.Fields.Add(field);
            }

            result.Structs.AddRange(structDict.Values);
        }

        /// <summary>
        /// 查找工作表
        /// </summary>
        private static ExcelWorksheet FindWorksheet(ExcelPackage package, params string[] names)
        {
            foreach (var name in names)
            {
                var worksheet = package.Workbook.Worksheets[name];
                if (worksheet != null)
                    return worksheet;
            }
            return null;
        }

        /// <summary>
        /// 查找表头位置
        /// </summary>
        private static Dictionary<string, int> FindHeaders(ExcelWorksheet worksheet, string[] headerNames)
        {
            var headers = new Dictionary<string, int>();
            foreach (var name in headerNames)
            {
                headers[name] = -1;
            }

            if (worksheet.Dimension == null)
                return headers;

            int colCount = worksheet.Dimension.Columns;
            for (int col = 1; col <= colCount; col++)
            {
                var cellValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(cellValue) && headers.ContainsKey(cellValue))
                {
                    headers[cellValue] = col;
                }
            }

            return headers;
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        private static string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            if (col <= 0) return string.Empty;
            return worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? string.Empty;
        }
    }
}
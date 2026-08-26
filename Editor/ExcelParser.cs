using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// Excel解析器
    /// </summary>
    public static class ExcelParser
    {
        private static readonly Dictionary<string, Type> s_EnumTypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_EnumTypeAmbiguous = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 解析Excel文件
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="sheetName">工作表名称，为空则使用第一个工作表</param>
        /// <returns>解析后的表格信息</returns>
        public static ExcelTableInfo ParseExcel(string filePath, string sheetName = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel文件不存在: {filePath}");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet;

                if (string.IsNullOrEmpty(sheetName))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        throw new InvalidOperationException("Excel文件中没有工作表");
                    }
                    worksheet = package.Workbook.Worksheets[0];
                }
                else
                {
                    worksheet = package.Workbook.Worksheets[sheetName];
                    if (worksheet == null)
                    {
                        throw new InvalidOperationException($"找不到工作表: {sheetName}");
                    }
                }

                return ParseWorksheet(worksheet, Path.GetFileNameWithoutExtension(filePath));
            }
        }

        /// <summary>
        /// 解析工作表
        /// </summary>
        private static ExcelTableInfo ParseWorksheet(ExcelWorksheet worksheet, string tableName)
        {
            var tableInfo = new ExcelTableInfo
            {
                TableName = tableName,
                ClassName = tableName
            };

            if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
            {
                throw new InvalidOperationException("工作表数据不足，至少需要2行（字段名和字段类型）");
            }

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // 解析字段信息
            ParseFields(worksheet, tableInfo, colCount);

            // 解析数据行（从第4行开始，前3行是字段名、类型、描述）
            int dataStartRow = 4;
            if (rowCount >= dataStartRow)
            {
                ParseDataRows(worksheet, tableInfo, dataStartRow, rowCount, colCount);
            }

            return tableInfo;
        }

        /// <summary>
        /// 解析字段信息
        /// </summary>
        private static void ParseFields(ExcelWorksheet worksheet, ExcelTableInfo tableInfo, int colCount)
        {
            for (int col = 1; col <= colCount; col++)
            {
                // 第1行：字段名
                var fieldName = worksheet.Cells[1, col].Text?.Trim();
                if (string.IsNullOrEmpty(fieldName))
                    continue;

                // 第2行：字段类型
                var fieldType = worksheet.Cells[2, col].Text?.Trim();
                if (string.IsNullOrEmpty(fieldType))
                {
                    Debug.LogWarning($"字段 {fieldName} 没有指定类型，默认使用string类型");
                    fieldType = "string";
                }

                // 验证类型是否支持
                if (!SupportedDataTypes.IsSupported(fieldType) &&
                    !SupportedDataTypes.IsEnumType(fieldType) &&
                    !SupportedDataTypes.IsCollectionType(fieldType) &&
                    !SupportedDataTypes.IsCustomType(fieldType) &&
                    !SupportedDataTypes.IsReferenceType(fieldType))
                {
                    Debug.LogWarning($"字段 {fieldName} 的类型 {fieldType} 不支持，默认使用string类型");
                    fieldType = "string";
                }

                // 第3行：字段描述（可选）
                var fieldDescription = worksheet.Cells[3, col].Text?.Trim();

                var fieldInfo = new ExcelFieldInfo
                {
                    Name = fieldName,
                    Type = fieldType,
                    Description = fieldDescription,
                    ColumnIndex = col,
                    IsPrimaryKey = col == 1 // 默认第一列为主键
                };

                tableInfo.Fields.Add(fieldInfo);

                // 设置主键字段
                if (fieldInfo.IsPrimaryKey)
                {
                    tableInfo.PrimaryKeyField = fieldName;
                }
            }
        }

        /// <summary>
        /// 解析数据行
        /// </summary>
        private static void ParseDataRows(ExcelWorksheet worksheet, ExcelTableInfo tableInfo,
            int startRow, int endRow, int colCount)
        {
            for (int row = startRow; row <= endRow; row++)
            {
                var rowData = new Dictionary<string, object>();
                bool hasData = false;

                for (int col = 1; col <= colCount && col <= tableInfo.Fields.Count; col++)
                {
                    var field = tableInfo.Fields[col - 1];
                    var cellValue = worksheet.Cells[row, col].Text?.Trim();

                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        hasData = true;
                    }

                    // 转换数据类型
                    var convertedValue = ConvertValue(cellValue, field.Type, field.Name, row);
                    rowData[field.Name] = convertedValue;
                }

                // 只添加有数据的行
                if (hasData)
                {
                    tableInfo.Rows.Add(rowData);
                }
            }
        }

        /// <summary>
        /// 快速预览Excel表头字段信息（只读前3行，不解析数据）
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>字段列表</returns>
        public static List<ExcelFieldInfo> PreviewFields(string filePath)
        {
            var fields = new List<ExcelFieldInfo>();

            if (!File.Exists(filePath))
                return fields;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = null;
                if (package.Workbook.Worksheets.Count > 0)
                {
                    worksheet = package.Workbook.Worksheets[0];
                }

                if (worksheet == null || worksheet.Dimension == null)
                    return fields;

                int colCount = worksheet.Dimension.Columns;
                for (int col = 1; col <= colCount; col++)
                {
                    var fieldName = worksheet.Cells[1, col].Text?.Trim();
                    if (string.IsNullOrEmpty(fieldName))
                        continue;

                    var fieldType = worksheet.Cells[2, col].Text?.Trim();
                    if (string.IsNullOrEmpty(fieldType))
                        fieldType = "string";

                    var fieldDescription = worksheet.Cells[3, col].Text?.Trim();

                    fields.Add(new ExcelFieldInfo
                    {
                        Name = fieldName,
                        Type = fieldType,
                        Description = fieldDescription,
                        ColumnIndex = col,
                        IsPrimaryKey = col == 1
                    });
                }
            }

            return fields;
        }

        /// <summary>
        /// 转换数据类型（集合/自定义类型走 JSON 字面量解析，标量保持原样）
        /// </summary>
        private static object ConvertValue(string value, string type, string fieldName, int row)
        {
            if (string.IsNullOrEmpty(value))
            {
                return JsonValueParser.GetDefaultValue(type);
            }

            try
            {
                return JsonValueParser.Parse(value, type);
            }
            catch (Exception ex)
            {
                Debug.LogError($"第{row}行字段{fieldName}的值'{value}'无法转换为{type}类型: {ex.Message}");
                return JsonValueParser.GetDefaultValue(type);
            }
        }

        internal static Type ResolveEnumType(string enumTypeName)
        {
            if (string.IsNullOrWhiteSpace(enumTypeName)) return null;
            if (s_EnumTypeAmbiguous.Contains(enumTypeName)) return null;

            if (s_EnumTypeCache.TryGetValue(enumTypeName, out var cached))
            {
                return cached;
            }

            var direct = Type.GetType(enumTypeName, false);
            if (direct != null && direct.IsEnum)
            {
                s_EnumTypeCache[enumTypeName] = direct;
                return direct;
            }

            bool isFullName = enumTypeName.Contains(".") || enumTypeName.Contains("+") || enumTypeName.Contains(",");
            string nestedVariant = null;
            if (isFullName && enumTypeName.Contains(".") && !enumTypeName.Contains("+"))
            {
                int lastDot = enumTypeName.LastIndexOf('.');
                if (lastDot > 0 && lastDot < enumTypeName.Length - 1)
                {
                    nestedVariant = enumTypeName.Substring(0, lastDot) + "+" + enumTypeName.Substring(lastDot + 1);
                }
            }

            var matches = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null || assembly.IsDynamic) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null) continue;

                foreach (var type in types)
                {
                    if (type == null) continue;
                    if (!type.IsEnum) continue;

                    if (isFullName)
                    {
                        if (string.Equals(type.FullName, enumTypeName, StringComparison.Ordinal) ||
                            (!string.IsNullOrEmpty(nestedVariant) && string.Equals(type.FullName, nestedVariant, StringComparison.Ordinal)))
                        {
                            matches.Add(type);
                        }
                    }
                    else
                    {
                        if (string.Equals(type.Name, enumTypeName, StringComparison.Ordinal))
                        {
                            matches.Add(type);
                        }
                    }
                }
            }

            if (matches.Count == 1)
            {
                s_EnumTypeCache[enumTypeName] = matches[0];
                return matches[0];
            }

            if (matches.Count > 1)
            {
                if (!isFullName)
                {
                    var filtered = matches
                        .Where(t => t != null && (string.IsNullOrEmpty(t.Namespace) || !t.Namespace.StartsWith("System", StringComparison.Ordinal)))
                        .ToList();

                    if (filtered.Count == 1)
                    {
                        s_EnumTypeCache[enumTypeName] = filtered[0];
                        return filtered[0];
                    }

                    if (filtered.Count > 1)
                    {
                        matches = filtered;
                    }
                }

                s_EnumTypeAmbiguous.Add(enumTypeName);
                Debug.LogWarning($"发现多个同名枚举 {enumTypeName}，无法自动解析，请在Excel类型中填写全名（含命名空间）。候选: {string.Join(", ", matches.Select(t => t.FullName))}");
                s_EnumTypeCache[enumTypeName] = null;
                return null;
            }

            s_EnumTypeCache[enumTypeName] = null;
            return null;
        }
    }
}

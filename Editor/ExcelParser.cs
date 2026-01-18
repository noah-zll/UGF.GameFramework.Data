using System;
using System.Collections.Generic;
using System.IO;
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
                if (!SupportedDataTypes.IsSupported(fieldType) && !SupportedDataTypes.IsEnumType(fieldType))
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
        /// 转换数据类型
        /// </summary>
        private static object ConvertValue(string value, string type, string fieldName, int row)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetDefaultValue(type);
            }
            
            try
            {
                // 检查是否为枚举类型
                if (SupportedDataTypes.IsEnumType(type))
                {
                    return ConvertEnumValue(value, type, fieldName, row);
                }
                
                switch (type.ToLower())
                {
                    case SupportedDataTypes.Int:
                        return int.Parse(value);
                    case SupportedDataTypes.Float:
                        return float.Parse(value);
                    case SupportedDataTypes.String:
                        return value;
                    case SupportedDataTypes.Bool:
                        return bool.Parse(value);
                    case SupportedDataTypes.Long:
                        return long.Parse(value);
                    case SupportedDataTypes.Double:
                        return double.Parse(value);
                    case SupportedDataTypes.Byte:
                        return byte.Parse(value);
                    case SupportedDataTypes.Short:
                        return short.Parse(value);
                    default:
                        return value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"第{row}行字段{fieldName}的值'{value}'无法转换为{type}类型: {ex.Message}");
                return GetDefaultValue(type);
            }
        }
        
        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        private static object GetDefaultValue(string type)
        {
            // 检查是否为枚举类型
            if (SupportedDataTypes.IsEnumType(type))
            {
                return 0; // 枚举默认值为0
            }
            
            switch (type.ToLower())
            {
                case SupportedDataTypes.Int:
                    return 0;
                case SupportedDataTypes.Float:
                    return 0f;
                case SupportedDataTypes.String:
                    return string.Empty;
                case SupportedDataTypes.Bool:
                    return false;
                case SupportedDataTypes.Long:
                    return 0L;
                case SupportedDataTypes.Double:
                    return 0.0;
                case SupportedDataTypes.Byte:
                    return (byte)0;
                case SupportedDataTypes.Short:
                    return (short)0;
                default:
                    return string.Empty;
            }
        }
        
        /// <summary>
        /// 转换枚举值
        /// </summary>
        private static object ConvertEnumValue(string value, string type, string fieldName, int row)
        {
            try
            {
                var enumTypeName = SupportedDataTypes.GetEnumTypeName(type);
                
                // 尝试解析为整数
                if (int.TryParse(value, out var intValue))
                {
                    return intValue;
                }
                
                // 如果有枚举类型名，尝试通过反射解析
                if (!string.IsNullOrEmpty(enumTypeName))
                {
                    var enumType = Type.GetType(enumTypeName);
                    if (enumType != null && enumType.IsEnum)
                    {
                        if (Enum.TryParse(enumType, value, true, out var enumValue))
                        {
                            return Convert.ToInt32(enumValue);
                        }
                    }
                }
                
                // 如果无法解析，返回0
                Debug.LogWarning($"第{row}行字段{fieldName}的枚举值'{value}'无法解析，使用默认值0");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"第{row}行字段{fieldName}的枚举值'{value}'转换失败: {ex.Message}");
                return 0;
            }
        }
    }
}
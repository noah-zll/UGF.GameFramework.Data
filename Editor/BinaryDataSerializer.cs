using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 二进制数据序列化器
    /// </summary>
    public static class BinaryDataSerializer
    {
        /// <summary>
        /// 将Excel数据序列化为二进制格式
        /// </summary>
        /// <param name="tableInfo">表格信息</param>
        /// <param name="outputPath">输出路径</param>
        public static void SerializeToBinary(ExcelTableInfo tableInfo, string outputPath)
        {
            if (tableInfo == null)
            {
                throw new ArgumentNullException(nameof(tableInfo));
            }
            
            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("输出路径不能为空", nameof(outputPath));
            }
            
            // 确保输出目录存在
            Directory.CreateDirectory(outputPath);
            
            var fileName = string.IsNullOrEmpty(tableInfo.ClassName) ? $"{tableInfo.TableName}.bytes" : $"{tableInfo.ClassName}.bytes";
            var filePath = Path.Combine(outputPath, fileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (var binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8))
                {
                    SerializeTableData(binaryWriter, tableInfo);
                }
            }
            
            Debug.Log($"二进制数据表已生成: {filePath}");
        }
        
        /// <summary>
        /// 序列化表格数据
        /// </summary>
        private static void SerializeTableData(BinaryWriter writer, ExcelTableInfo tableInfo)
        {
            // 写入文件头信息
            WriteHeader(writer, tableInfo);
            
            // 写入数据行
            WriteDataRows(writer, tableInfo);
        }
        
        /// <summary>
        /// 写入文件头信息
        /// </summary>
        private static void WriteHeader(BinaryWriter writer, ExcelTableInfo tableInfo)
        {
            // 写入魔数（用于验证文件格式）
            writer.Write(0x44544247); // "GBTD" (GameFramework Binary Table Data)
            
            // 写入版本号
            writer.Write((byte)1);
            
            // 写入表名
            writer.Write(tableInfo.TableName ?? string.Empty);
            
            // 写入字段数量
            writer.Write(tableInfo.Fields.Count);
            
            // 写入字段信息
            foreach (var field in tableInfo.Fields)
            {
                writer.Write(field.Name ?? string.Empty);
                writer.Write(field.Type ?? string.Empty);
                writer.Write(field.Description ?? string.Empty);
                writer.Write(field.IsPrimaryKey);
            }
            
            // 写入数据行数量
            writer.Write(tableInfo.Rows.Count);
        }
        
        /// <summary>
        /// 写入数据行
        /// </summary>
        private static void WriteDataRows(BinaryWriter writer, ExcelTableInfo tableInfo)
        {
            foreach (var row in tableInfo.Rows)
            {
                // 为每一行写入数据
                foreach (var field in tableInfo.Fields)
                {
                    if (row.TryGetValue(field.Name, out var value))
                    {
                        WriteFieldValue(writer, value, field.Type);
                    }
                    else
                    {
                        // 写入默认值
                        WriteDefaultValue(writer, field.Type);
                    }
                }
            }
        }
        
        /// <summary>
        /// 写入字段值
        /// </summary>
        private static void WriteFieldValue(BinaryWriter writer, object value, string type)
        {
            if (value == null)
            {
                WriteDefaultValue(writer, type);
                return;
            }
            
            try
            {
                // 检查是否为枚举类型
                if (SupportedDataTypes.IsEnumType(type))
                {
                    WriteEnumValue(writer, value, type);
                    return;
                }
                
                switch (type.ToLower())
                {
                    case SupportedDataTypes.Int:
                        writer.Write(Convert.ToInt32(value));
                        break;
                    case SupportedDataTypes.Float:
                        writer.Write(Convert.ToSingle(value));
                        break;
                    case SupportedDataTypes.String:
                        writer.Write(value.ToString() ?? string.Empty);
                        break;
                    case SupportedDataTypes.Bool:
                        writer.Write(Convert.ToBoolean(value));
                        break;
                    case SupportedDataTypes.Long:
                        writer.Write(Convert.ToInt64(value));
                        break;
                    case SupportedDataTypes.Double:
                        writer.Write(Convert.ToDouble(value));
                        break;
                    case SupportedDataTypes.Byte:
                        writer.Write(Convert.ToByte(value));
                        break;
                    case SupportedDataTypes.Short:
                        writer.Write(Convert.ToInt16(value));
                        break;
                    default:
                        writer.Write(value.ToString() ?? string.Empty);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入字段值时发生错误，类型: {type}, 值: {value}, 错误: {ex.Message}");
                WriteDefaultValue(writer, type);
            }
        }
        
        /// <summary>
        /// 写入默认值
        /// </summary>
        private static void WriteDefaultValue(BinaryWriter writer, string type)
        {
            // 检查是否为枚举类型
            if (SupportedDataTypes.IsEnumType(type))
            {
                writer.Write(0); // 枚举默认值为0
                return;
            }
            
            switch (type.ToLower())
            {
                case SupportedDataTypes.Int:
                    writer.Write(0);
                    break;
                case SupportedDataTypes.Float:
                    writer.Write(0f);
                    break;
                case SupportedDataTypes.String:
                    writer.Write(string.Empty);
                    break;
                case SupportedDataTypes.Bool:
                    writer.Write(false);
                    break;
                case SupportedDataTypes.Long:
                    writer.Write(0L);
                    break;
                case SupportedDataTypes.Double:
                    writer.Write(0.0);
                    break;
                case SupportedDataTypes.Byte:
                    writer.Write((byte)0);
                    break;
                case SupportedDataTypes.Short:
                    writer.Write((short)0);
                    break;
                default:
                    writer.Write(string.Empty);
                    break;
            }
        }
        
        /// <summary>
        /// 写入枚举值
        /// </summary>
        private static void WriteEnumValue(BinaryWriter writer, object value, string type)
        {
            try
            {
                var enumTypeName = SupportedDataTypes.GetEnumTypeName(type);
                
                // 如果值是字符串，尝试解析为枚举值
                if (value is string stringValue)
                {
                    // 尝试解析为整数
                    if (int.TryParse(stringValue, out var intValue))
                    {
                        writer.Write(intValue);
                        return;
                    }
                    
                    // 如果有枚举类型名，尝试通过反射解析
                    if (!string.IsNullOrEmpty(enumTypeName))
                    {
                        var enumType = Type.GetType(enumTypeName);
                        if (enumType != null && enumType.IsEnum)
                        {
                            if (Enum.TryParse(enumType, stringValue, true, out var enumValue))
                            {
                                writer.Write(Convert.ToInt32(enumValue));
                                return;
                            }
                        }
                    }
                    
                    // 如果无法解析，写入0
                    writer.Write(0);
                }
                else
                {
                    // 直接转换为整数
                    writer.Write(Convert.ToInt32(value));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入枚举值时发生错误，类型: {type}, 值: {value}, 错误: {ex.Message}");
                writer.Write(0);
            }
        }
        
        /// <summary>
        /// 验证二进制文件格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否为有效的二进制数据表文件</returns>
        public static bool ValidateBinaryFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;
                
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var binaryReader = new BinaryReader(fileStream, Encoding.UTF8))
                    {
                        // 检查魔数
                        var magic = binaryReader.ReadInt32();
                        if (magic != 0x44544247) // "GBTD"
                            return false;
                            
                        // 检查版本号
                        var version = binaryReader.ReadByte();
                        if (version != 1)
                            return false;
                            
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 读取二进制文件信息（用于调试）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件信息</returns>
        public static string ReadBinaryFileInfo(string filePath)
        {
            if (!ValidateBinaryFile(filePath))
                return "无效的二进制数据表文件";
                
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var binaryReader = new BinaryReader(fileStream, Encoding.UTF8))
                    {
                        var sb = new StringBuilder();
                        
                        // 跳过魔数和版本号
                        binaryReader.ReadInt32();
                        binaryReader.ReadByte();
                        
                        // 读取表名
                        var tableName = binaryReader.ReadString();
                        sb.AppendLine($"表名: {tableName}");
                        
                        // 读取字段数量
                        var fieldCount = binaryReader.ReadInt32();
                        sb.AppendLine($"字段数量: {fieldCount}");
                        
                        // 读取字段信息
                        sb.AppendLine("字段信息:");
                        for (int i = 0; i < fieldCount; i++)
                        {
                            var fieldName = binaryReader.ReadString();
                            var fieldType = binaryReader.ReadString();
                            var fieldDesc = binaryReader.ReadString();
                            var isPrimaryKey = binaryReader.ReadBoolean();
                            
                            sb.AppendLine($"  {fieldName} ({fieldType}) - {fieldDesc} {(isPrimaryKey ? "[主键]" : "")}");
                        }
                        
                        // 读取数据行数量
                        var rowCount = binaryReader.ReadInt32();
                        sb.AppendLine($"数据行数量: {rowCount}");
                        
                        return sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                return $"读取文件信息时发生错误: {ex.Message}";
            }
        }
    }
}
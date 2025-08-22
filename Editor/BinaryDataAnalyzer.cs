using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 二进制数据分析器
    /// </summary>
    public static class BinaryDataAnalyzer
    {
        /// <summary>
        /// 数据表信息
        /// </summary>
        public class DataTableInfo
        {
            public string TableName { get; set; }
            public int RecordCount { get; set; }
            public int HeaderSize { get; set; }
            public List<RecordInfo> Records { get; set; } = new List<RecordInfo>();
            public bool IsValid { get; set; }
        }
        
        /// <summary>
        /// 记录信息
        /// </summary>
        public class RecordInfo
        {
            public int Index { get; set; }
            public int Offset { get; set; }
            public int Size { get; set; }
            public byte[] Data { get; set; }
            public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// 分析二进制数据表
        /// </summary>
        public static DataTableInfo AnalyzeDataTable(byte[] data)
        {
            var info = new DataTableInfo();
            
            if (data == null || data.Length < 12)
            {
                return info;
            }
            
            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    // 检查魔数
                    var magic = reader.ReadInt32();
                    if (magic != 0x44544247) // "GBTD"
                    {
                        Debug.LogWarning($"无效的魔数: 0x{magic:X8}, 期望: 0x44544247");
                        return info;
                    }
                    
                    // 检查版本号
                    var version = reader.ReadByte();
                    if (version != 1)
                    {
                        Debug.LogWarning($"不支持的版本号: {version}, 期望: 1");
                        return info;
                    }
                    
                    // 读取表名
                    info.TableName = reader.ReadString();
                    
                    // 读取字段数量
                    var fieldCount = reader.ReadInt32();
                    if (fieldCount < 0 || fieldCount > 100)
                    {
                        Debug.LogWarning($"无效的字段数量: {fieldCount}");
                        return info;
                    }
                    
                    // 跳过字段信息（暂时不解析）
                    for (int i = 0; i < fieldCount; i++)
                    {
                        reader.ReadString(); // 字段名
                        reader.ReadString(); // 字段类型
                        reader.ReadString(); // 字段描述
                        reader.ReadBoolean(); // 是否主键
                    }
                    
                    // 读取记录数量
                    info.RecordCount = reader.ReadInt32();
                    info.HeaderSize = (int)stream.Position;
                    
                    // 分析记录数据
                    AnalyzeRecords(reader, info, data);
                    
                    info.IsValid = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"分析数据表失败: {ex.Message}");
            }
            
            return info;
        }
        
        /// <summary>
        /// 分析记录数据
        /// </summary>
        private static void AnalyzeRecords(BinaryReader reader, DataTableInfo info, byte[] data)
        {
            var remainingData = data.Length - info.HeaderSize;
            if (remainingData <= 0 || info.RecordCount <= 0)
            {
                return;
            }
            
            // 估算每条记录的平均大小
            var estimatedRecordSize = remainingData / info.RecordCount;
            
            try
            {
                for (int i = 0; i < info.RecordCount && reader.BaseStream.Position < reader.BaseStream.Length; i++)
                {
                    var record = new RecordInfo
                    {
                        Index = i,
                        Offset = (int)reader.BaseStream.Position
                    };
                    
                    var startPosition = reader.BaseStream.Position;
                    
                    // 尝试解析记录数据
                    ParseRecord(reader, record, estimatedRecordSize);
                    
                    record.Size = (int)(reader.BaseStream.Position - startPosition);
                    
                    // 读取原始数据
                    var currentPos = reader.BaseStream.Position;
                    reader.BaseStream.Position = startPosition;
                    record.Data = reader.ReadBytes(record.Size);
                    reader.BaseStream.Position = currentPos;
                    
                    info.Records.Add(record);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"解析记录数据失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解析单条记录
        /// </summary>
        private static void ParseRecord(BinaryReader reader, RecordInfo record, int estimatedSize)
        {
            try
            {
                var startPosition = reader.BaseStream.Position;
                var maxPosition = Math.Min(startPosition + estimatedSize * 2, reader.BaseStream.Length);
                
                // 尝试解析常见的数据类型
                int fieldIndex = 0;
                
                while (reader.BaseStream.Position < maxPosition && fieldIndex < 20) // 限制字段数量
                {
                    var fieldName = $"Field_{fieldIndex}";
                    
                    try
                    {
                        // 尝试不同的数据类型
                        if (TryParseInt32(reader, out var intValue))
                        {
                            record.Fields[fieldName] = intValue;
                        }
                        else if (TryParseString(reader, out var stringValue))
                        {
                            record.Fields[fieldName] = stringValue;
                        }
                        else if (TryParseFloat(reader, out var floatValue))
                        {
                            record.Fields[fieldName] = floatValue;
                        }
                        else if (TryParseBool(reader, out var boolValue))
                        {
                            record.Fields[fieldName] = boolValue;
                        }
                        else
                        {
                            // 如果无法解析，跳过一个字节
                            if (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                var byteValue = reader.ReadByte();
                                record.Fields[fieldName] = $"0x{byteValue:X2}";
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        fieldIndex++;
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"解析记录失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 尝试解析Int32
        /// </summary>
        private static bool TryParseInt32(BinaryReader reader, out int value)
        {
            value = 0;
            if (reader.BaseStream.Position + 4 > reader.BaseStream.Length)
            {
                return false;
            }
            
            try
            {
                value = reader.ReadInt32();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 尝试解析字符串
        /// </summary>
        private static bool TryParseString(BinaryReader reader, out string value)
        {
            value = string.Empty;
            
            if (reader.BaseStream.Position + 4 > reader.BaseStream.Length)
            {
                return false;
            }
            
            try
            {
                var length = reader.ReadInt32();
                if (length < 0 || length > 1024 || reader.BaseStream.Position + length > reader.BaseStream.Length)
                {
                    // 回退位置
                    reader.BaseStream.Position -= 4;
                    return false;
                }
                
                var bytes = reader.ReadBytes(length);
                value = Encoding.UTF8.GetString(bytes);
                
                // 验证字符串是否有效
                if (ContainsValidText(value))
                {
                    return true;
                }
                
                // 如果字符串无效，回退位置
                reader.BaseStream.Position -= length + 4;
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 尝试解析Float
        /// </summary>
        private static bool TryParseFloat(BinaryReader reader, out float value)
        {
            value = 0f;
            if (reader.BaseStream.Position + 4 > reader.BaseStream.Length)
            {
                return false;
            }
            
            try
            {
                value = reader.ReadSingle();
                return !float.IsNaN(value) && !float.IsInfinity(value);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 尝试解析Bool
        /// </summary>
        private static bool TryParseBool(BinaryReader reader, out bool value)
        {
            value = false;
            if (reader.BaseStream.Position + 1 > reader.BaseStream.Length)
            {
                return false;
            }
            
            try
            {
                var byteValue = reader.ReadByte();
                if (byteValue == 0 || byteValue == 1)
                {
                    value = byteValue == 1;
                    return true;
                }
                
                // 回退位置
                reader.BaseStream.Position -= 1;
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 检查字符串是否包含有效文本
        /// </summary>
        private static bool ContainsValidText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 256)
            {
                return false;
            }
            
            // 检查是否包含可打印字符
            int printableCount = 0;
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || c == ' ')
                {
                    printableCount++;
                }
            }
            
            // 至少50%的字符是可打印的
            return printableCount >= text.Length * 0.5;
        }
        
        /// <summary>
        /// 生成数据表摘要
        /// </summary>
        public static string GenerateDataTableSummary(DataTableInfo info)
        {
            if (!info.IsValid)
            {
                return "无效的数据表格式";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"表名: {info.TableName}");
            sb.AppendLine($"记录数量: {info.RecordCount}");
            sb.AppendLine($"头部大小: {info.HeaderSize} 字节");
            sb.AppendLine($"解析的记录数: {info.Records.Count}");
            
            if (info.Records.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("记录摘要:");
                
                for (int i = 0; i < Math.Min(info.Records.Count, 5); i++)
                {
                    var record = info.Records[i];
                    sb.AppendLine($"  记录 {record.Index}: 偏移量 0x{record.Offset:X8}, 大小 {record.Size} 字节");
                    
                    foreach (var field in record.Fields)
                    {
                        sb.AppendLine($"    {field.Key}: {field.Value}");
                    }
                }
                
                if (info.Records.Count > 5)
                {
                    sb.AppendLine($"  ... 还有 {info.Records.Count - 5} 条记录");
                }
            }
            
            return sb.ToString();
        }
    }
}
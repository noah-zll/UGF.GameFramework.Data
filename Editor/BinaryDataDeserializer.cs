using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 二进制数据反序列化器
    /// </summary>
    public static class BinaryDataDeserializer
    {
        /// <summary>
        /// 数据表信息
        /// </summary>
        public class DataTableInfo
        {
            public string TableName { get; set; } = string.Empty;
            public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
            public List<DataRecord> Records { get; set; } = new List<DataRecord>();
            public bool IsValid { get; set; } = false;
        }

        /// <summary>
        /// 字段信息
        /// </summary>
        public class FieldInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool IsPrimaryKey { get; set; } = false;
        }

        /// <summary>
        /// 数据记录
        /// </summary>
        public class DataRecord
        {
            public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
            public int Index { get; set; }

            /// <summary>
            /// 获取主键值
            /// </summary>
            public object GetPrimaryKey(List<FieldInfo> fields)
            {
                var primaryField = fields.Find(f => f.IsPrimaryKey);
                if (primaryField != null && Values.TryGetValue(primaryField.Name, out var value))
                {
                    return value;
                }
                return null;
            }

            /// <summary>
            /// 根据字段名获取值
            /// </summary>
            public T GetValue<T>(string fieldName, T defaultValue = default(T))
            {
                if (Values.TryGetValue(fieldName, out var value))
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                return defaultValue;
            }
        }

        /// <summary>
        /// 反序列化二进制数据表
        /// </summary>
        public static DataTableInfo DeserializeDataTable(byte[] data)
        {
            var info = new DataTableInfo();

            if (data == null || data.Length < 12)
            {
                return info;
            }

            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
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

                    // 读取字段信息
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var field = new FieldInfo
                        {
                            Name = reader.ReadString(),
                            Type = reader.ReadString(),
                            Description = reader.ReadString(),
                            IsPrimaryKey = reader.ReadBoolean()
                        };
                        info.Fields.Add(field);
                    }

                    // 读取记录数量
                    var recordCount = reader.ReadInt32();
                    if (recordCount < 0 || recordCount > 100000)
                    {
                        Debug.LogWarning($"无效的记录数量: {recordCount}");
                        return info;
                    }

                    // 读取数据记录
                    for (int i = 0; i < recordCount; i++)
                    {
                        var record = new DataRecord { Index = i };

                        foreach (var field in info.Fields)
                        {
                            var value = ReadFieldValue(reader, field.Type);
                            record.Values[field.Name] = value;
                        }

                        info.Records.Add(record);
                    }

                    info.IsValid = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"反序列化二进制数据表时发生错误: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// 读取字段值
        /// </summary>
        private static object ReadFieldValue(BinaryReader reader, string type)
        {
            try
            {
                // 检查是否为集合类型（数组、List、字典）
                if (SupportedDataTypes.IsCollectionType(type))
                {
                    return ReadCollectionValue(reader, type);
                }

                // 检查是否为自定义类/结构体
                if (SupportedDataTypes.IsCustomType(type))
                {
                    return ReadCustomValue(reader, type);
                }

                // 检查是否为枚举类型
                if (SupportedDataTypes.IsEnumType(type))
                {
                    return reader.ReadInt32(); // 枚举值作为int读取
                }

                switch (type.ToLower())
                {
                    case SupportedDataTypes.Int:
                        return reader.ReadInt32();
                    case SupportedDataTypes.Float:
                        return reader.ReadSingle();
                    case SupportedDataTypes.String:
                        return reader.ReadString();
                    case SupportedDataTypes.Bool:
                        return reader.ReadBoolean();
                    case SupportedDataTypes.Long:
                        return reader.ReadInt64();
                    case SupportedDataTypes.Double:
                        return reader.ReadDouble();
                    case SupportedDataTypes.Byte:
                        return reader.ReadByte();
                    case SupportedDataTypes.Short:
                        return reader.ReadInt16();
                    default:
                        return reader.ReadString();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取字段值时发生错误，类型: {type}, 错误: {ex.Message}");
                return GetDefaultValue(type);
            }
        }

        /// <summary>
        /// 读取集合类型值（数组、List、字典）
        /// </summary>
        private static object ReadCollectionValue(BinaryReader reader, string type)
        {
            if (SupportedDataTypes.IsDictionaryType(type))
            {
                SupportedDataTypes.GetDictionaryTypes(type, out var keyType, out var valueType);
                var count = reader.ReadInt32();
                var dict = new Dictionary<object, object>();

                for (int i = 0; i < count; i++)
                {
                    var key = ReadElementValue(reader, keyType);
                    var value = ReadElementValue(reader, valueType);
                    dict[key] = value;
                }
                return dict;
            }

            // 数组或List
            var elementType = SupportedDataTypes.GetElementType(type);
            var elementCount = reader.ReadInt32();
            var list = new List<object>();

            for (int i = 0; i < elementCount; i++)
            {
                list.Add(ReadElementValue(reader, elementType));
            }
            return list;
        }

        /// <summary>
        /// 读取自定义类/结构体实例（按成员定义顺序递归读取，返回成员名→值字典）
        /// </summary>
        private static object ReadCustomValue(BinaryReader reader, string type)
        {
            if (!CustomTypeRegistry.TryGet(type, out var info))
                return null;

            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var member in info.Members)
            {
                var memberType = member.IsArray ? member.Type + "[]" : member.Type;
                values[member.Name] = ReadFieldValue(reader, memberType);
            }
            return values;
        }

        /// <summary>
        /// 读取单个元素值（基础类型、枚举、集合或自定义类型，递归）
        /// </summary>
        private static object ReadElementValue(BinaryReader reader, string type)
        {
            // 集合类型（嵌套集合）：递归读取
            if (SupportedDataTypes.IsCollectionType(type))
            {
                return ReadCollectionValue(reader, type);
            }

            // 自定义类/结构体
            if (SupportedDataTypes.IsCustomType(type))
            {
                return ReadCustomValue(reader, type);
            }

            // 检查是否为枚举类型
            if (SupportedDataTypes.IsEnumType(type))
            {
                return reader.ReadInt32(); // 枚举值作为int读取
            }

            switch (type.ToLower())
            {
                case SupportedDataTypes.Int:
                    return reader.ReadInt32();
                case SupportedDataTypes.Float:
                    return reader.ReadSingle();
                case SupportedDataTypes.String:
                    return reader.ReadString();
                case SupportedDataTypes.Bool:
                    return reader.ReadBoolean();
                case SupportedDataTypes.Long:
                    return reader.ReadInt64();
                case SupportedDataTypes.Double:
                    return reader.ReadDouble();
                case SupportedDataTypes.Byte:
                    return reader.ReadByte();
                case SupportedDataTypes.Short:
                    return reader.ReadInt16();
                default:
                    return reader.ReadString();
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

            // 集合类型默认值为空集合
            if (SupportedDataTypes.IsCollectionType(type))
            {
                return new List<object>();
            }

            // 自定义类型默认实例（各成员默认值）
            if (SupportedDataTypes.IsCustomType(type))
            {
                if (CustomTypeRegistry.TryGet(type, out var info))
                {
                    var values = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (var member in info.Members)
                    {
                        values[member.Name] = GetDefaultValue(member.IsArray ? member.Type + "[]" : member.Type);
                    }
                    return values;
                }
                return null;
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
        /// 根据主键查找记录
        /// </summary>
        public static DataRecord FindRecordByPrimaryKey(DataTableInfo tableInfo, object primaryKey)
        {
            if (tableInfo == null || primaryKey == null)
                return null;

            var primaryField = tableInfo.Fields.Find(f => f.IsPrimaryKey);
            if (primaryField == null)
                return null;

            return tableInfo.Records.Find(r =>
            {
                if (r.Values.TryGetValue(primaryField.Name, out var value))
                {
                    return value.ToString() == primaryKey.ToString();
                }
                return false;
            });
        }

        /// <summary>
        /// 根据字段值搜索记录
        /// </summary>
        public static List<DataRecord> SearchRecords(DataTableInfo tableInfo, string fieldName, object searchValue)
        {
            var results = new List<DataRecord>();

            if (tableInfo == null || string.IsNullOrEmpty(fieldName) || searchValue == null)
                return results;

            var searchStr = searchValue.ToString().ToLower();

            foreach (var record in tableInfo.Records)
            {
                if (record.Values.TryGetValue(fieldName, out var value))
                {
                    if (value.ToString().ToLower().Contains(searchStr))
                    {
                        results.Add(record);
                    }
                }
            }

            return results;
        }
    }
}
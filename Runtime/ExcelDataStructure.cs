using System;
using System.Collections.Generic;

namespace UGF.GameFramework.Data
{
    /// <summary>
    /// Excel字段信息
    /// </summary>
    [Serializable]
    public class ExcelFieldInfo
    {
        /// <summary>
        /// 字段名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 字段类型
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// 字段描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 列索引
        /// </summary>
        public int ColumnIndex { get; set; }
        
        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }
    }
    
    /// <summary>
    /// Excel表格信息
    /// </summary>
    [Serializable]
    public class ExcelTableInfo
    {
        /// <summary>
        /// 表格名称
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// 生成的类名
        /// </summary>
        public string ClassName { get; set; }
        
        /// <summary>
        /// 字段列表
        /// </summary>
        public List<ExcelFieldInfo> Fields { get; set; }
        
        /// <summary>
        /// 数据行列表
        /// </summary>
        public List<Dictionary<string, object>> Rows { get; set; }
        
        /// <summary>
        /// 主键字段名
        /// </summary>
        public string PrimaryKeyField { get; set; }
        
        public ExcelTableInfo()
        {
            Fields = new List<ExcelFieldInfo>();
            Rows = new List<Dictionary<string, object>>();
        }
    }
    
    /// <summary>
    /// 支持的数据类型
    /// </summary>
    public static class SupportedDataTypes
    {
        public const string Int = "int";
        public const string Float = "float";
        public const string String = "string";
        public const string Bool = "bool";
        public const string Long = "long";
        public const string Double = "double";
        public const string Byte = "byte";
        public const string Short = "short";
        public const string Enum = "enum";
        
        /// <summary>
        /// 获取所有支持的数据类型
        /// </summary>
        public static readonly string[] AllTypes = 
        {
            Int, Float, String, Bool, Long, Double, Byte, Short, Enum
        };
        
        /// <summary>
        /// 检查类型是否支持
        /// </summary>
        public static bool IsSupported(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;
                
            return Array.Exists(AllTypes, t => t.Equals(type, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// 获取C#类型名
        /// </summary>
        public static string GetCSharpType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return "string";
                
            switch (type.ToLower())
            {
                case Int: return "int";
                case Float: return "float";
                case String: return "string";
                case Bool: return "bool";
                case Long: return "long";
                case Double: return "double";
                case Byte: return "byte";
                case Short: return "short";
                case Enum: return type; // 枚举类型返回原始类型名
                default: return "string";
            }
        }
        
        /// <summary>
        /// 检查是否为枚举类型
        /// </summary>
        public static bool IsEnumType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;
                
            // 枚举类型格式：enum:EnumTypeName 或直接是枚举类型名
            return type.StartsWith("enum:", StringComparison.OrdinalIgnoreCase) || 
                   (!IsSupported(type) && !type.Contains("[") && !type.Contains("]"));
        }
        
        /// <summary>
        /// 获取枚举类型名
        /// </summary>
        public static string GetEnumTypeName(string type)
        {
            if (string.IsNullOrEmpty(type))
                return string.Empty;
                
            // 如果是 enum:EnumTypeName 格式
            if (type.StartsWith("enum:", StringComparison.OrdinalIgnoreCase))
            {
                return type.Substring(5);
            }
            
            // 如果不是基础类型，则认为是枚举类型
            if (!IsSupported(type) && !type.Contains("[") && !type.Contains("]"))
            {
                return type;
            }
            
            return string.Empty;
        }
    }
}
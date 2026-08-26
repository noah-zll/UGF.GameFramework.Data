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
    /// 自定义类型类别
    /// </summary>
    public enum CustomTypeKind
    {
        /// <summary>枚举</summary>
        Enum,
        /// <summary>类</summary>
        Class,
        /// <summary>结构体</summary>
        Struct
    }

    /// <summary>
    /// 自定义类型成员（类属性 / 结构体字段）
    /// </summary>
    public class CustomTypeMemberInfo
    {
        /// <summary>成员名称</summary>
        public string Name { get; set; }

        /// <summary>成员类型（如 int、ItemType）</summary>
        public string Type { get; set; }

        /// <summary>是否为数组（IsArray=true 时类型写作 Type[]）</summary>
        public bool IsArray { get; set; }

        /// <summary>默认值（字符串形式，可空）</summary>
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// 自定义类型定义（来自类型定义表 Classes/Structs/Enums）
    /// </summary>
    public class CustomTypeInfo
    {
        /// <summary>类型名称</summary>
        public string Name { get; set; }

        /// <summary>类型类别</summary>
        public CustomTypeKind Kind { get; set; }

        /// <summary>类型所在命名空间（生成代码 using 用）</summary>
        public string Namespace { get; set; }

        /// <summary>成员列表（枚举为空）</summary>
        public List<CustomTypeMemberInfo> Members { get; set; } = new List<CustomTypeMemberInfo>();
    }

    /// <summary>
    /// 自定义类型注册表（由类型定义表填充；未配置时为空，类型判定保持宽松枚举规则）
    /// </summary>
    public static class CustomTypeRegistry
    {
        private static readonly Dictionary<string, CustomTypeInfo> Types = new Dictionary<string, CustomTypeInfo>(StringComparer.Ordinal);

        /// <summary>
        /// 清空注册表
        /// </summary>
        public static void Clear()
        {
            Types.Clear();
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public static void Register(CustomTypeInfo info)
        {
            if (info == null || string.IsNullOrEmpty(info.Name))
                return;

            Types[info.Name] = info;
        }

        /// <summary>
        /// 获取类型定义
        /// </summary>
        public static bool TryGet(string name, out CustomTypeInfo info)
        {
            return Types.TryGetValue(name, out info);
        }

        /// <summary>
        /// 是否为注册表中的类或结构体
        /// </summary>
        public static bool IsClassOrStruct(string name)
        {
            return Types.TryGetValue(name, out var info) && info.Kind != CustomTypeKind.Enum;
        }
    }

    /// <summary>
    /// 引用类型注册表（由构建流程填充：表名 → 主键类型）
    /// 用于把 @表名 解析为目标表主键的基础类型
    /// </summary>
    public static class ReferenceTypeRegistry
    {
        private static readonly Dictionary<string, string> PrimaryKeyTypes = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// 清空注册表
        /// </summary>
        public static void Clear()
        {
            PrimaryKeyTypes.Clear();
        }

        /// <summary>
        /// 注册表的主键类型（type 为基础类型写法，如 "int"）
        /// </summary>
        public static void Register(string tableName, string primaryKeyType)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(primaryKeyType))
                return;

            PrimaryKeyTypes[tableName] = primaryKeyType;
        }

        /// <summary>
        /// 获取目标表主键类型
        /// </summary>
        public static bool TryGetPrimaryKeyType(string tableName, out string primaryKeyType)
        {
            return PrimaryKeyTypes.TryGetValue(tableName, out primaryKeyType);
        }

        /// <summary>
        /// 是否已注册该表
        /// </summary>
        public static bool IsRegistered(string tableName)
        {
            return tableName != null && PrimaryKeyTypes.ContainsKey(tableName);
        }
    }

    /// <summary>
    /// 集合类型描述
    /// </summary>
    public sealed class CollectionTypeDescriptor
    {
        /// <summary>
        /// 集合类别
        /// </summary>
        public enum Kind
        {
            /// <summary>数组 T[]</summary>
            Array,
            /// <summary>列表 List&lt;T&gt;</summary>
            List,
            /// <summary>哈希集 HashSet&lt;T&gt;</summary>
            HashSet,
            /// <summary>字典 Dictionary&lt;K,V&gt;</summary>
            Dictionary
        }

        /// <summary>集合类别</summary>
        public Kind CollectionKind;

        /// <summary>元素类型（数组/List/HashSet）</summary>
        public string ElementType;

        /// <summary>键类型（字典）</summary>
        public string KeyType;

        /// <summary>值类型（字典）</summary>
        public string ValueType;
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

            // 数组：元素可能本身是集合或自定义类型
            if (IsArrayType(type))
            {
                return ResolveElementType(GetElementType(type)) + "[]";
            }

            if (IsListType(type))
            {
                return "List<" + ResolveElementType(GetElementType(type)) + ">";
            }

            if (IsHashSetType(type))
            {
                return "HashSet<" + ResolveElementType(GetElementType(type)) + ">";
            }

            if (IsDictionaryType(type))
            {
                GetDictionaryTypes(type, out var keyType, out var valueType);
                return "Dictionary<" + ResolveElementType(keyType) + "," + ResolveElementType(valueType) + ">";
            }

            // 自定义类/结构体：原样返回类型名
            if (IsCustomType(type))
            {
                return type;
            }

            // 引用类型 @表名：返回目标表主键的C#类型
            if (IsReferenceType(type))
            {
                return ResolveElementType(type);
            }

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
        /// 是否为注册表中的类/结构体
        /// </summary>
        public static bool IsCustomType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            return CustomTypeRegistry.TryGet(type, out var info) && info.Kind != CustomTypeKind.Enum;
        }

        /// <summary>
        /// 检查是否为枚举类型
        /// </summary>
        public static bool IsEnumType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            // 枚举类型格式：enum:EnumTypeName
            if (type.StartsWith("enum:", StringComparison.OrdinalIgnoreCase))
                return true;

            // 注册表命中：按注册表的类别判定
            if (CustomTypeRegistry.TryGet(type, out var info))
                return info.Kind == CustomTypeKind.Enum;

            // 宽松规则（未配置注册表或未命中）：非基础、非集合、非引用、不含括号的类型名视为枚举
            return !IsSupported(type) && !IsCollectionType(type) && !IsReferenceType(type) && !type.Contains("[") && !type.Contains("]");
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

            // 注册表命中：仅枚举返回类型名
            if (CustomTypeRegistry.TryGet(type, out var info))
            {
                return info.Kind == CustomTypeKind.Enum ? type : string.Empty;
            }

            // 宽松规则：非基础、非集合、非引用、不含括号的类型名视为枚举
            if (!IsSupported(type) && !IsCollectionType(type) && !IsReferenceType(type) && !type.Contains("[") && !type.Contains("]"))
            {
                return type;
            }

            return string.Empty;
        }

        /// <summary>
        /// 是否为引用类型（@表名，如 @Drop、List&lt;@Drop&gt; 的元素类型）
        /// </summary>
        public static bool IsReferenceType(string type)
        {
            return !string.IsNullOrEmpty(type) && type.StartsWith("@", StringComparison.Ordinal);
        }

        /// <summary>
        /// 获取引用类型的目标表名（@Drop → Drop；非引用类型返回空串）
        /// </summary>
        public static string GetReferenceTargetTable(string type)
        {
            if (!IsReferenceType(type))
                return string.Empty;

            return type.Substring(1).Trim();
        }

        /// <summary>
        /// 将引用类型 @表名 解析为目标表主键的基础类型（未注册时回退 string）
        /// </summary>
        public static string ResolveReferenceType(string type)
        {
            if (!IsReferenceType(type))
                return type;

            var targetTable = GetReferenceTargetTable(type);
            if (ReferenceTypeRegistry.TryGetPrimaryKeyType(targetTable, out var pkType))
                return pkType;

            return String;
        }

        /// <summary>
        /// 是否为数组类型（如 int[]、int[][]、List&lt;int&gt;[]）
        /// </summary>
        public static bool IsArrayType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            return type.EndsWith("[]", StringComparison.Ordinal);
        }

        /// <summary>
        /// 是否为List类型（如 List&lt;int&gt;、List&lt;List&lt;int&gt;&gt;）
        /// </summary>
        public static bool IsListType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            return type.StartsWith("List<", StringComparison.OrdinalIgnoreCase) && type.EndsWith(">");
        }

        /// <summary>
        /// 是否为HashSet类型（如 HashSet&lt;int&gt;）
        /// </summary>
        public static bool IsHashSetType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            return type.StartsWith("HashSet<", StringComparison.OrdinalIgnoreCase) && type.EndsWith(">");
        }

        /// <summary>
        /// 是否为字典类型（如 Dictionary&lt;string,int&gt;、Dictionary&lt;List&lt;int&gt;,string&gt;）
        /// </summary>
        public static bool IsDictionaryType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            return type.StartsWith("Dictionary<", StringComparison.OrdinalIgnoreCase) && type.EndsWith(">");
        }

        /// <summary>
        /// 是否为集合类型（数组、List、HashSet或字典）
        /// </summary>
        public static bool IsCollectionType(string type)
        {
            return TryParseCollection(type, out _);
        }

        /// <summary>
        /// 解析集合类型描述（数组/List/HashSet/字典，支持任意嵌套）
        /// </summary>
        public static bool TryParseCollection(string type, out CollectionTypeDescriptor descriptor)
        {
            descriptor = null;
            if (string.IsNullOrEmpty(type))
                return false;

            descriptor = new CollectionTypeDescriptor();

            if (IsDictionaryType(type))
            {
                descriptor.CollectionKind = CollectionTypeDescriptor.Kind.Dictionary;
                GetDictionaryTypes(type, out descriptor.KeyType, out descriptor.ValueType);
                return true;
            }

            if (IsListType(type))
            {
                descriptor.CollectionKind = CollectionTypeDescriptor.Kind.List;
                descriptor.ElementType = GetElementType(type);
                return true;
            }

            if (IsHashSetType(type))
            {
                descriptor.CollectionKind = CollectionTypeDescriptor.Kind.HashSet;
                descriptor.ElementType = GetElementType(type);
                return true;
            }

            if (IsArrayType(type))
            {
                descriptor.CollectionKind = CollectionTypeDescriptor.Kind.Array;
                descriptor.ElementType = GetElementType(type);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取集合元素类型（数组、List和HashSet使用）
        /// </summary>
        public static string GetElementType(string type)
        {
            if (IsArrayType(type))
            {
                return type.Substring(0, type.Length - 2).Trim();
            }

            if (IsListType(type))
            {
                // 去掉 "List<" 前缀和末尾 ">"，内层类型完整保留（支持嵌套）
                return type.Substring(5, type.Length - 6).Trim();
            }

            if (IsHashSetType(type))
            {
                // 去掉 "HashSet<" 前缀和末尾 ">"
                return type.Substring(8, type.Length - 9).Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取字典的键类型和值类型（按顶层逗号切分，兼容嵌套集合键值）
        /// </summary>
        public static void GetDictionaryTypes(string type, out string keyType, out string valueType)
        {
            keyType = string.Empty;
            valueType = string.Empty;

            if (!IsDictionaryType(type))
                return;

            // 去掉 "Dictionary<" 前缀和 ">" 后缀
            var inner = type.Substring(11, type.Length - 12).Trim();

            // 按括号深度找顶层逗号（深度为0时的逗号），兼容 Dictionary<List<int>,string>
            int depth = 0;
            int commaIndex = -1;
            for (int i = 0; i < inner.Length; i++)
            {
                var c = inner[i];
                if (c == '<')
                    depth++;
                else if (c == '>')
                    depth--;
                else if (c == ',' && depth == 0)
                {
                    commaIndex = i;
                    break;
                }
            }

            if (commaIndex < 0)
                return;

            keyType = inner.Substring(0, commaIndex).Trim();
            valueType = inner.Substring(commaIndex + 1).Trim();
        }

        /// <summary>
        /// 解析元素类型为C#类型名（基础类型原样返回，枚举返回枚举类型名，自定义类型原样返回，集合递归）
        /// </summary>
        public static string ResolveElementType(string elementType)
        {
            if (string.IsNullOrEmpty(elementType))
                return "string";

            // 引用类型 @表名：返回目标表主键的C#类型
            if (IsReferenceType(elementType))
            {
                var targetTable = GetReferenceTargetTable(elementType);
                if (ReferenceTypeRegistry.TryGetPrimaryKeyType(targetTable, out var pkType))
                    return GetCSharpType(pkType);
                return "string"; // 目标表未注册：回退 string（构建期会警告）
            }

            // 枚举类型（enum:EnumName 或 直接类型名）
            if (IsEnumType(elementType))
            {
                var enumTypeName = GetEnumTypeName(elementType);
                return string.IsNullOrEmpty(enumTypeName) ? "int" : enumTypeName;
            }

            // 自定义类/结构体
            if (IsCustomType(elementType))
            {
                return elementType;
            }

            return GetCSharpType(elementType);
        }
    }
}

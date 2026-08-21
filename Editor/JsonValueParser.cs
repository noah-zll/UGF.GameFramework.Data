using System;
using System.Collections.Generic;
using System.Text;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 配表单元格 JSON 字面量解析器。
    /// 集合字段与自定义类型字段的单元格值为 JSON 字面量（[ ... ] / { ... }），标量字段保持原样裸写。
    /// 解析结果统一为 object 树：基础类型原语、枚举 int、集合 List&lt;object&gt;/Dictionary&lt;object,object&gt;、
    /// 自定义类型 Dictionary&lt;string,object&gt;（成员名 → 值，缺省成员补默认值）。
    /// </summary>
    public static class JsonValueParser
    {
        private sealed class Ctx
        {
            public string Text;
            public int Pos;
        }

        /// <summary>
        /// 按目标类型解析单元格值（JSON 字面量或裸标量）
        /// </summary>
        /// <param name="value">单元格文本</param>
        /// <param name="type">字段类型字符串</param>
        /// <returns>解析后的 object 值</returns>
        public static object Parse(string value, string type)
        {
            var ctx = new Ctx { Text = value ?? string.Empty, Pos = 0 };
            return ParseValue(ctx, type);
        }

        /// <summary>
        /// 获取类型默认值（空单元格/解析失败回退用）
        /// </summary>
        public static object GetDefaultValue(string type)
        {
            // 枚举默认值为0
            if (SupportedDataTypes.IsEnumType(type))
                return 0;

            // 集合类型默认值为空集合
            if (SupportedDataTypes.IsCollectionType(type))
                return new List<object>();

            // 自定义类/结构体默认实例（各成员取默认值）
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
                case SupportedDataTypes.Int: return 0;
                case SupportedDataTypes.Float: return 0f;
                case SupportedDataTypes.String: return string.Empty;
                case SupportedDataTypes.Bool: return false;
                case SupportedDataTypes.Long: return 0L;
                case SupportedDataTypes.Double: return 0.0;
                case SupportedDataTypes.Byte: return (byte)0;
                case SupportedDataTypes.Short: return (short)0;
                default: return string.Empty;
            }
        }

        // ==================== 递归解析 ====================

        private static object ParseValue(Ctx ctx, string type)
        {
            SkipWs(ctx);

            // 自定义类/结构体：JSON 对象
            if (SupportedDataTypes.IsCustomType(type))
                return ParseCustomObject(ctx, type);

            // 集合：JSON 数组 / JSON 对象
            if (SupportedDataTypes.IsDictionaryType(type))
                return ParseDictionary(ctx, type);

            if (SupportedDataTypes.IsCollectionType(type))
                return ParseSequence(ctx, type);

            return ParseScalar(ctx, type);
        }

        /// <summary>
        /// 解析数组/List/HashSet（JSON 数组）
        /// </summary>
        private static object ParseSequence(Ctx ctx, string type)
        {
            var elementType = SupportedDataTypes.GetElementType(type);
            var list = new List<object>();

            Expect(ctx, '[');
            while (true)
            {
                SkipWs(ctx);
                if (IsAt(ctx, ']'))
                {
                    ctx.Pos++;
                    break;
                }

                if (list.Count > 0)
                    Expect(ctx, ',');

                SkipWs(ctx);
                list.Add(ParseValue(ctx, elementType));
            }
            return list;
        }

        /// <summary>
        /// 解析字典（JSON 对象）
        /// </summary>
        private static object ParseDictionary(Ctx ctx, string type)
        {
            SupportedDataTypes.GetDictionaryTypes(type, out var keyType, out var valueType);
            var dict = new Dictionary<object, object>();

            Expect(ctx, '{');
            while (true)
            {
                SkipWs(ctx);
                if (IsAt(ctx, '}'))
                {
                    ctx.Pos++;
                    break;
                }

                if (dict.Count > 0)
                    Expect(ctx, ',');

                SkipWs(ctx);
                var key = ParseValue(ctx, keyType);
                SkipWs(ctx);
                Expect(ctx, ':');
                SkipWs(ctx);
                var value = ParseValue(ctx, valueType);
                dict[key] = value;
            }
            return dict;
        }

        /// <summary>
        /// 解析自定义类/结构体（JSON 对象 → 成员名→值字典）
        /// </summary>
        private static object ParseCustomObject(Ctx ctx, string type)
        {
            if (!CustomTypeRegistry.TryGet(type, out var info))
                throw new InvalidOperationException($"未注册的自定义类型: {type}");

            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Expect(ctx, '{');
            while (true)
            {
                SkipWs(ctx);
                if (IsAt(ctx, '}'))
                {
                    ctx.Pos++;
                    break;
                }

                if (seen.Count > 0)
                    Expect(ctx, ',');

                SkipWs(ctx);
                var key = ReadMemberName(ctx);
                SkipWs(ctx);
                Expect(ctx, ':');
                SkipWs(ctx);

                var member = FindMember(info, key);
                if (member == null)
                    throw new InvalidOperationException($"类型 {type} 没有成员: {key}");

                var memberType = member.IsArray ? member.Type + "[]" : member.Type;
                values[member.Name] = ParseValue(ctx, memberType);
                seen.Add(key);
            }

            // 缺省成员补默认值（按定义顺序）
            foreach (var member in info.Members)
            {
                if (!values.ContainsKey(member.Name))
                {
                    values[member.Name] = GetDefaultValue(member.IsArray ? member.Type + "[]" : member.Type);
                }
            }

            return values;
        }

        /// <summary>
        /// 解析标量（基础类型或枚举），支持 JSON 字符串（带引号）与裸 token
        /// </summary>
        private static object ParseScalar(Ctx ctx, string type)
        {
            SkipWs(ctx);

            // 枚举：数字或名字（带引号或裸 token）
            if (SupportedDataTypes.IsEnumType(type))
                return ParseEnum(ctx, type);

            if (IsAt(ctx, '"'))
            {
                var str = ParseJsonString(ctx);
                return ConvertStringToValue(str, type);
            }

            var token = ReadToken(ctx);
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("缺少值");

            return ConvertTokenToValue(token, type);
        }

        private static object ParseEnum(Ctx ctx, string type)
        {
            SkipWs(ctx);
            var enumTypeName = SupportedDataTypes.GetEnumTypeName(type);

            string token;
            if (IsAt(ctx, '"'))
            {
                token = ParseJsonString(ctx);
            }
            else
            {
                token = ReadToken(ctx);
                if (string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("缺少枚举值");
            }

            if (int.TryParse(token, out var intValue))
                return intValue;

            if (!string.IsNullOrEmpty(enumTypeName))
            {
                var enumType = ExcelParser.ResolveEnumType(enumTypeName);
                if (enumType != null && enumType.IsEnum && Enum.TryParse(enumType, token, true, out var enumValue))
                {
                    return Convert.ToInt32(enumValue);
                }
            }

            // 无法解析时返回0
            return 0;
        }

        // ==================== 基础类型转换 ====================

        private static object ConvertStringToValue(string str, string type)
        {
            switch (type.ToLower())
            {
                case SupportedDataTypes.Int: return int.Parse(str);
                case SupportedDataTypes.Float: return float.Parse(str);
                case SupportedDataTypes.String: return str;
                case SupportedDataTypes.Bool: return ParseBool(str);
                case SupportedDataTypes.Long: return long.Parse(str);
                case SupportedDataTypes.Double: return double.Parse(str);
                case SupportedDataTypes.Byte: return byte.Parse(str);
                case SupportedDataTypes.Short: return short.Parse(str);
                default: return str;
            }
        }

        private static object ConvertTokenToValue(string token, string type)
        {
            switch (type.ToLower())
            {
                case SupportedDataTypes.Int: return int.Parse(token);
                case SupportedDataTypes.Float: return float.Parse(token);
                case SupportedDataTypes.String: return token;
                case SupportedDataTypes.Bool: return ParseBool(token);
                case SupportedDataTypes.Long: return long.Parse(token);
                case SupportedDataTypes.Double: return double.Parse(token);
                case SupportedDataTypes.Byte: return byte.Parse(token);
                case SupportedDataTypes.Short: return short.Parse(token);
                default: return token;
            }
        }

        private static bool ParseBool(string value)
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
                return true;
            if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0")
                return false;
            return bool.Parse(value);
        }

        // ==================== 底层字符工具 ====================

        private static void SkipWs(Ctx ctx)
        {
            while (ctx.Pos < ctx.Text.Length)
            {
                var c = ctx.Text[ctx.Pos];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    ctx.Pos++;
                else
                    break;
            }
        }

        private static bool IsAt(Ctx ctx, char c)
        {
            return ctx.Pos < ctx.Text.Length && ctx.Text[ctx.Pos] == c;
        }

        private static void Expect(Ctx ctx, char c)
        {
            SkipWs(ctx);
            if (!IsAt(ctx, c))
                throw new InvalidOperationException($"期望字符 '{c}'，实际位于 {ctx.Pos}: {SafeSnippet(ctx)}");
            ctx.Pos++;
        }

        /// <summary>
        /// 解析 JSON 字符串（双引号包裹，支持 \" 和 \\ 转义）
        /// </summary>
        private static string ParseJsonString(Ctx ctx)
        {
            Expect(ctx, '"');
            var sb = new StringBuilder();

            while (ctx.Pos < ctx.Text.Length)
            {
                var c = ctx.Text[ctx.Pos++];
                if (c == '"')
                    return sb.ToString();

                if (c == '\\')
                {
                    if (ctx.Pos >= ctx.Text.Length)
                        break;
                    var esc = ctx.Text[ctx.Pos++];
                    sb.Append(esc == 'n' ? '\n' : esc == 't' ? '\t' : esc);
                }
                else
                {
                    sb.Append(c);
                }
            }

            throw new InvalidOperationException("字符串引号未闭合");
        }

        /// <summary>
        /// 读取裸 token（到结构字符或空白为止）
        /// </summary>
        private static string ReadToken(Ctx ctx)
        {
            var sb = new StringBuilder();
            while (ctx.Pos < ctx.Text.Length)
            {
                var c = ctx.Text[ctx.Pos];
                if (c == '[' || c == ']' || c == '{' || c == '}' || c == ',' || c == ':' ||
                    c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    break;
                sb.Append(c);
                ctx.Pos++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// 读取自定义类型成员名（支持带引号 JSON 字符串或裸 token）
        /// </summary>
        private static string ReadMemberName(Ctx ctx)
        {
            if (IsAt(ctx, '"'))
                return ParseJsonString(ctx);

            var token = ReadToken(ctx);
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("缺少成员名");
            return token;
        }

        private static CustomTypeMemberInfo FindMember(CustomTypeInfo info, string name)
        {
            foreach (var member in info.Members)
            {
                if (member.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return member;
            }
            return null;
        }

        private static string SafeSnippet(Ctx ctx)
        {
            var start = Math.Max(0, ctx.Pos - 10);
            var len = Math.Min(ctx.Text.Length - start, 20);
            return ctx.Text.Substring(start, len);
        }
    }
}

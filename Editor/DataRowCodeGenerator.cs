using System;
using System.IO;
using System.Text;
using UnityEngine;
using UGF.GameFramework.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// DataRow代码生成器
    /// </summary>
    public static class DataRowCodeGenerator
    {
        private static readonly Dictionary<string, string> s_EnumNamespaceCache = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// 生成读取代码时使用的局部变量唯一ID（每个类生成前重置）
        /// </summary>
        private static int s_uid;

        /// <summary>
        /// 生成DataRow类代码
        /// </summary>
        /// <param name="tableInfo">表格信息</param>
        /// <param name="namespaceName">命名空间</param>
        /// <param name="outputPath">输出路径</param>
        public static void GenerateDataRowClass(ExcelTableInfo tableInfo, string namespaceName, string outputPath)
        {
            if (tableInfo == null)
            {
                throw new ArgumentNullException(nameof(tableInfo));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("输出路径不能为空", nameof(outputPath));
            }

            var className = string.IsNullOrEmpty(tableInfo.ClassName) ? $"DR{tableInfo.TableName}" : $"DR{tableInfo.ClassName}";
            var code = GenerateCode(tableInfo, className, namespaceName);

            // 确保输出目录存在
            Directory.CreateDirectory(outputPath);

            var filePath = Path.Combine(outputPath, $"{className}.cs");
            File.WriteAllText(filePath, code, Encoding.UTF8);

            Debug.Log($"DataRow类已生成: {filePath}");
        }

        /// <summary>
        /// 生成代码内容
        /// </summary>
        private static string GenerateCode(ExcelTableInfo tableInfo, string className, string namespaceName)
        {
            var sb = new StringBuilder();

            // 重置局部变量唯一ID（每次生成一个类）
            s_uid = 0;

            // 文件头注释
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine("// 此文件由工具自动生成，请勿手动修改。");
            sb.AppendLine("// 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine();

            // using语句
            sb.AppendLine("using GameFramework;");
            sb.AppendLine("using GameFramework.DataTable;");
            sb.AppendLine("using UnityGameFramework.Runtime;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Collections.Generic;");
            if (HasReferenceField(tableInfo))
            {
                sb.AppendLine("using UGF.GameFramework.Data;");
            }
            sb.AppendLine();

            var extraUsings = CollectExtraUsings(tableInfo, namespaceName);
            if (extraUsings.Count > 0)
            {
                foreach (var ns in extraUsings)
                {
                    sb.AppendLine($"using {ns};");
                }
                sb.AppendLine();
            }

            // 命名空间开始
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            // 类注释
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {tableInfo.TableName}数据表行类");
            sb.AppendLine($"    /// </summary>");

            // 类定义
            sb.AppendLine($"    public class {className} : IDataRow");
            sb.AppendLine("    {");

            // 生成属性
            GenerateProperties(sb, tableInfo);

            // 生成关系字段强类型查询方法
            GenerateRelationMethods(sb, tableInfo);

            // 生成ParseDataRow方法
            GenerateParseDataRowMethod(sb, tableInfo);

            // 类结束
            sb.AppendLine("    }");

            // 命名空间结束
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 生成属性
        /// </summary>
        private static void GenerateProperties(StringBuilder sb, ExcelTableInfo tableInfo)
        {
            foreach (var field in tableInfo.Fields)
            {
                var csharpType = GetPropertyType(field.Type);

                sb.AppendLine();

                // 属性注释
                if (!string.IsNullOrEmpty(field.Description))
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {field.Description}");
                    sb.AppendLine($"        /// </summary>");
                }
                else
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {field.Name}");
                    sb.AppendLine($"        /// </summary>");
                }

                // 属性定义
                sb.AppendLine($"        public {csharpType} {field.Name} {{ get; private set; }}");
            }
        }

        /// <summary>
        /// 表中是否存在引用类型字段（@表名，含集合元素/字典键值）
        /// </summary>
        private static bool HasReferenceField(ExcelTableInfo tableInfo)
        {
            if (tableInfo?.Fields == null)
                return false;

            foreach (var field in tableInfo.Fields)
            {
                if (ContainsReferenceType(field.Type))
                    return true;
            }
            return false;
        }

        private static bool ContainsReferenceType(string type)
        {
            if (SupportedDataTypes.IsReferenceType(type))
                return true;

            if (SupportedDataTypes.IsDictionaryType(type))
            {
                SupportedDataTypes.GetDictionaryTypes(type, out var keyType, out var valueType);
                return SupportedDataTypes.IsReferenceType(keyType) || SupportedDataTypes.IsReferenceType(valueType);
            }

            if (SupportedDataTypes.IsCollectionType(type))
            {
                return SupportedDataTypes.IsReferenceType(SupportedDataTypes.GetElementType(type));
            }

            return false;
        }

        /// <summary>
        /// 生成关系字段强类型查询方法
        /// 支持：@Drop（1:1）、List&lt;@Drop&gt;/@Drop[]/HashSet&lt;@Drop&gt;（1:N）、字典键/值为引用（按key查询）
        /// </summary>
        private static void GenerateRelationMethods(StringBuilder sb, ExcelTableInfo tableInfo)
        {
            var usedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var field in tableInfo.Fields)
            {
                if (SupportedDataTypes.IsReferenceType(field.Type))
                {
                    var target = SupportedDataTypes.GetReferenceTargetTable(field.Type);
                    EmitRelationMethod(sb, tableInfo, field, target, false, ref usedNames);
                }
                else if (SupportedDataTypes.IsCollectionType(field.Type) && !SupportedDataTypes.IsDictionaryType(field.Type))
                {
                    var elementType = SupportedDataTypes.GetElementType(field.Type);
                    if (SupportedDataTypes.IsReferenceType(elementType))
                    {
                        var target = SupportedDataTypes.GetReferenceTargetTable(elementType);
                        EmitRelationMethod(sb, tableInfo, field, target, true, ref usedNames);
                    }
                }
                else if (SupportedDataTypes.IsDictionaryType(field.Type))
                {
                    SupportedDataTypes.GetDictionaryTypes(field.Type, out var keyType, out var valueType);
                    if (SupportedDataTypes.IsReferenceType(keyType))
                    {
                        var target = SupportedDataTypes.GetReferenceTargetTable(keyType);
                        EmitRelationMethodByKey(sb, tableInfo, field, target, ref usedNames);
                    }
                    else if (SupportedDataTypes.IsReferenceType(valueType))
                    {
                        var target = SupportedDataTypes.GetReferenceTargetTable(valueType);
                        EmitRelationMethodByKey(sb, tableInfo, field, target, ref usedNames);
                    }
                }
            }
        }

        private static string GetTargetClassName(string targetTable)
        {
            return "DR" + targetTable;
        }

        private static string GetReferencePkCSharpType(string targetTable)
        {
            if (ReferenceTypeRegistry.TryGetPrimaryKeyType(targetTable, out var pkType))
            {
                var csharp = SupportedDataTypes.GetCSharpType(pkType);
                return string.IsNullOrEmpty(csharp) ? "int" : csharp;
            }
            return "int";
        }

        /// <summary>
        /// 生成 1:1 / 1:N 强类型查询方法
        /// </summary>
        private static void EmitRelationMethod(StringBuilder sb, ExcelTableInfo tableInfo, ExcelFieldInfo field,
            string targetTable, bool isCollection, ref HashSet<string> usedNames)
        {
            var targetClass = GetTargetClassName(targetTable);
            var baseName = isCollection ? $"Get{targetTable}s" : $"Get{targetTable}";
            var methodName = usedNames.Contains(baseName) ? $"Get{targetTable}_{field.Name}" : baseName;
            usedNames.Add(methodName);

            var relationName = $"{tableInfo.TableName}_{targetTable}_{field.Name}";
            var indent = "        ";

            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 关系字段：{targetTable}（{(isCollection ? "一对多" : "一对一")}，目标表主键）");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}/// <returns>{(isCollection ? "目标表行数组" : "目标表行")}</returns>");
            sb.AppendLine($"{indent}public {targetClass}{(isCollection ? "[]" : "")} {methodName}()");
            sb.AppendLine($"{indent}{{");

            if (isCollection)
            {
                var countExpr = SupportedDataTypes.IsArrayType(field.Type) ? "list.Length" : "list.Count";
                sb.AppendLine($"{indent}    var list = {field.Name};");
                sb.AppendLine($"{indent}    if (list == null) return new {targetClass}[0];");
                sb.AppendLine($"{indent}    var result = new {targetClass}[{countExpr}];");
                sb.AppendLine($"{indent}    for (var i = 0; i < {countExpr}; i++)");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        result[i] = GameEntry.GetComponent<DataTableComponent>().GetRelatedOne(\"{relationName}\", list[i]) as {targetClass};");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    return result;");
            }
            else
            {
                sb.AppendLine($"{indent}    var row = GameEntry.GetComponent<DataTableComponent>().GetRelatedOne(\"{relationName}\", {field.Name});");
                sb.AppendLine($"{indent}    return row as {targetClass};");
            }

            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 生成字典键/值为引用的强类型查询方法（按 key 查询）
        /// </summary>
        private static void EmitRelationMethodByKey(StringBuilder sb, ExcelTableInfo tableInfo, ExcelFieldInfo field,
            string targetTable, ref HashSet<string> usedNames)
        {
            var targetClass = GetTargetClassName(targetTable);
            var baseName = $"Get{targetTable}";
            var methodName = usedNames.Contains(baseName) ? $"Get{targetTable}_{field.Name}" : baseName;
            usedNames.Add(methodName);

            var relationName = $"{tableInfo.TableName}_{targetTable}_{field.Name}";
            var pkType = GetReferencePkCSharpType(targetTable);
            var indent = "        ";

            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 关系字段：{targetTable}（字典引用，按 {pkType} 键查询目标表）");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}/// <param name=\"key\">目标表主键</param>");
            sb.AppendLine($"{indent}/// <returns>目标表行</returns>");
            sb.AppendLine($"{indent}public {targetClass} {methodName}({pkType} key)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    var row = GameEntry.GetComponent<DataTableComponent>().GetRelatedOne(\"{relationName}\", key);");
            sb.AppendLine($"{indent}    return row as {targetClass};");
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 获取属性类型
        /// </summary>
        private static string GetPropertyType(string type)
        {
            // 检查是否为集合类型（数组、List、字典）
            if (SupportedDataTypes.IsCollectionType(type))
            {
                return SupportedDataTypes.GetCSharpType(type);
            }

            // 检查是否为枚举类型
            if (SupportedDataTypes.IsEnumType(type))
            {
                var enumTypeName = SupportedDataTypes.GetEnumTypeName(type);
                return string.IsNullOrEmpty(enumTypeName) ? "int" : enumTypeName;
            }

            return SupportedDataTypes.GetCSharpType(type);
        }

        private static List<string> CollectExtraUsings(ExcelTableInfo tableInfo, string currentNamespace)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (tableInfo?.Fields == null) return result.ToList();

            foreach (var field in tableInfo.Fields)
            {
                // 枚举命名空间
                if (SupportedDataTypes.IsEnumType(field.Type))
                {
                    var enumTypeName = SupportedDataTypes.GetEnumTypeName(field.Type);

                    if (string.IsNullOrWhiteSpace(enumTypeName)) continue;

                    if (enumTypeName.Contains(".") || enumTypeName.Contains("+")) continue;

                    var enumNamespace = TryResolveEnumNamespace(enumTypeName);
                    if (string.IsNullOrEmpty(enumNamespace)) continue;
                    if (!string.IsNullOrEmpty(currentNamespace) && string.Equals(enumNamespace, currentNamespace, StringComparison.Ordinal)) continue;

                    result.Add(enumNamespace);
                }

                // 自定义类/结构体命名空间（递归收集字段类型中引用的所有自定义类型）
                var customTypes = new HashSet<string>(StringComparer.Ordinal);
                CollectCustomTypes(field.Type, customTypes);
                foreach (var customName in customTypes)
                {
                    if (!CustomTypeRegistry.TryGet(customName, out var info)) continue;

                    var ns = info.Namespace;
                    if (string.IsNullOrEmpty(ns)) continue;
                    if (!string.IsNullOrEmpty(currentNamespace) && string.Equals(ns, currentNamespace, StringComparison.Ordinal)) continue;

                    result.Add(ns);
                }
            }

            return result.OrderBy(n => n, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// 递归收集类型字符串中引用的所有自定义类/结构体类型名（含集合元素、字典键值、自定义类型成员）
        /// </summary>
        private static void CollectCustomTypes(string type, HashSet<string> customTypes)
        {
            if (string.IsNullOrEmpty(type))
                return;

            if (SupportedDataTypes.IsCustomType(type))
            {
                customTypes.Add(type);

                // 递归收集成员内引用的自定义类型
                if (CustomTypeRegistry.TryGet(type, out var info))
                {
                    foreach (var member in info.Members)
                    {
                        CollectCustomTypes(member.IsArray ? member.Type + "[]" : member.Type, customTypes);
                    }
                }
                return;
            }

            if (SupportedDataTypes.IsCollectionType(type))
            {
                if (SupportedDataTypes.IsDictionaryType(type))
                {
                    SupportedDataTypes.GetDictionaryTypes(type, out var keyType, out var valueType);
                    CollectCustomTypes(keyType, customTypes);
                    CollectCustomTypes(valueType, customTypes);
                }
                else
                {
                    CollectCustomTypes(SupportedDataTypes.GetElementType(type), customTypes);
                }
            }
        }

        private static string TryResolveEnumNamespace(string enumShortName)
        {
            if (string.IsNullOrWhiteSpace(enumShortName)) return null;
            if (s_EnumNamespaceCache.TryGetValue(enumShortName, out var cached)) return cached;

            var enumType = ExcelParser.ResolveEnumType(enumShortName);
            if (enumType == null || !enumType.IsEnum)
            {
                s_EnumNamespaceCache[enumShortName] = null;
                return null;
            }

            var ns = enumType.Namespace;
            s_EnumNamespaceCache[enumShortName] = string.IsNullOrEmpty(ns) ? null : ns;
            return s_EnumNamespaceCache[enumShortName];
        }

        /// <summary>
        /// 生成ParseDataRow方法
        /// </summary>
        private static void GenerateParseDataRowMethod(StringBuilder sb, ExcelTableInfo tableInfo)
        {
            // ParseDataRow方法 - 字符串重载
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 解析数据表行");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"dataRowString\">要解析的数据表行字符串</param>");
            sb.AppendLine("        /// <param name=\"userData\">用户自定义数据</param>");
            sb.AppendLine("        /// <returns>是否解析数据表行成功</returns>");
            sb.AppendLine("        public bool ParseDataRow(string dataRowString, object userData)");
            sb.AppendLine("        {");
            sb.AppendLine("            // 字符串解析暂不实现");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");

            // ParseDataRow方法 - 二进制重载
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 解析数据表行");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"dataRowBytes\">要解析的数据表行二进制流</param>");
            sb.AppendLine("        /// <param name=\"startIndex\">数据表行二进制流的起始位置</param>");
            sb.AppendLine("        /// <param name=\"length\">数据表行二进制流的长度</param>");
            sb.AppendLine("        /// <param name=\"userData\">用户自定义数据</param>");
            sb.AppendLine("        /// <returns>是否解析数据表行成功</returns>");
            sb.AppendLine("        public bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)");
            sb.AppendLine("        {");
            sb.AppendLine("            using (var memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))");
            sb.AppendLine("            {");
            sb.AppendLine("                using (var binaryReader = new BinaryReader(memoryStream, System.Text.Encoding.UTF8))");
            sb.AppendLine("                {");
            sb.AppendLine("                    try");
            sb.AppendLine("                    {");

            // 生成字段读取代码
            foreach (var field in tableInfo.Fields)
            {
                // 集合或自定义类型生成专门的读取代码块（递归）
                if (SupportedDataTypes.IsCollectionType(field.Type) || SupportedDataTypes.IsCustomType(field.Type))
                {
                    GenerateCollectionReadCode(sb, field.Name, field.Type);
                }
                else if (SupportedDataTypes.IsEnumType(field.Type))
                {
                    var enumTypeName = SupportedDataTypes.GetEnumTypeName(field.Type);
                    if (!string.IsNullOrEmpty(enumTypeName))
                    {
                        sb.AppendLine($"                        {field.Name} = ({enumTypeName})binaryReader.ReadInt32();");
                    }
                    else
                    {
                        sb.AppendLine($"                        {field.Name} = binaryReader.ReadInt32();");
                    }
                }
                else
                {
                    var readMethod = GetBinaryReadMethod(field.Type);
                    sb.AppendLine($"                        {field.Name} = binaryReader.{readMethod}();");
                }
            }

            sb.AppendLine("                        return true;");
            sb.AppendLine("                    }");
            sb.AppendLine("                    catch (System.Exception ex)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        GameFrameworkLog.Error(\"Parse data row exception: {0}\", ex.ToString());");
            sb.AppendLine("                        return false;");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
        }

        /// <summary>
        /// 获取二进制读取方法名
        /// </summary>
        private static string GetBinaryReadMethod(string type)
        {
            // 检查是否为枚举类型
            if (SupportedDataTypes.IsEnumType(type))
            {
                return "ReadInt32"; // 枚举类型以int形式存储
            }

            // 引用类型 @表名：按目标表主键类型读取
            type = SupportedDataTypes.ResolveReferenceType(type);

            switch (type.ToLower())
            {
                case SupportedDataTypes.Int:
                    return "ReadInt32";
                case SupportedDataTypes.Float:
                    return "ReadSingle";
                case SupportedDataTypes.String:
                    return "ReadString";
                case SupportedDataTypes.Bool:
                    return "ReadBoolean";
                case SupportedDataTypes.Long:
                    return "ReadInt64";
                case SupportedDataTypes.Double:
                    return "ReadDouble";
                case SupportedDataTypes.Byte:
                    return "ReadByte";
                case SupportedDataTypes.Short:
                    return "ReadInt16";
                default:
                    return "ReadString";
            }
        }

        /// <summary>
        /// 生成集合/自定义类型字段的二进制读取代码块（递归）
        /// </summary>
        private static void GenerateCollectionReadCode(StringBuilder sb, string fieldName, string type)
        {
            const string indent = "                        "; // 24空格，与ParseDataRow内层缩进一致
            sb.AppendLine($"{indent}{{");
            var valueVar = EmitReadValue(sb, indent + "    ", type);
            sb.AppendLine($"{indent}    {fieldName} = {valueVar};");
            sb.AppendLine($"{indent}}}");
        }

        private static string NextVar(string prefix)
        {
            return $"{prefix}{s_uid++}";
        }

        /// <summary>
        /// 生成读取一个 type 值并存入新局部变量的代码，返回变量名（递归）
        /// </summary>
        /// <param name="sb">代码缓冲</param>
        /// <param name="indent">缩进</param>
        /// <param name="type">目标类型字符串</param>
        /// <returns>持有读取结果的局部变量名</returns>
        private static string EmitReadValue(StringBuilder sb, string indent, string type)
        {
            var varName = NextVar("__v");

            // 集合类型（嵌套递归）
            if (SupportedDataTypes.IsCollectionType(type))
            {
                if (SupportedDataTypes.IsDictionaryType(type))
                {
                    SupportedDataTypes.GetDictionaryTypes(type, out var keyType, out var valueType);
                    var dictType = SupportedDataTypes.GetCSharpType(type);
                    var countVar = NextVar("__count");
                    var iVar = NextVar("__i");

                    sb.AppendLine($"{indent}var {countVar} = binaryReader.ReadInt32();");
                    sb.AppendLine($"{indent}var {varName} = new {dictType}({countVar});");
                    sb.AppendLine($"{indent}for (var {iVar} = 0; {iVar} < {countVar}; {iVar}++)");
                    sb.AppendLine($"{indent}{{");
                    var keyVar = EmitReadValue(sb, indent + "    ", keyType);
                    var valVar = EmitReadValue(sb, indent + "    ", valueType);
                    sb.AppendLine($"{indent}    {varName}[{keyVar}] = {valVar};");
                    sb.AppendLine($"{indent}}}");
                    return varName;
                }

                var elementType = SupportedDataTypes.GetElementType(type);
                var isListLike = SupportedDataTypes.IsListType(type) || SupportedDataTypes.IsHashSetType(type);
                var collType = SupportedDataTypes.GetCSharpType(type);
                var countVar2 = NextVar("__count");
                var iVar2 = NextVar("__i");

                sb.AppendLine($"{indent}var {countVar2} = binaryReader.ReadInt32();");
                if (isListLike)
                {
                    sb.AppendLine($"{indent}var {varName} = new {collType}({countVar2});");
                }
                else // 数组：元素可能本身是数组（如 int[][] → new int[count][]）
                {
                    sb.AppendLine($"{indent}var {varName} = new {GetArrayInitializer(elementType, countVar2)};");
                }
                sb.AppendLine($"{indent}for (var {iVar2} = 0; {iVar2} < {countVar2}; {iVar2}++)");
                sb.AppendLine($"{indent}{{");
                var itemVar = EmitReadValue(sb, indent + "    ", elementType);
                if (isListLike)
                {
                    sb.AppendLine($"{indent}    {varName}.Add({itemVar});");
                }
                else
                {
                    sb.AppendLine($"{indent}    {varName}[{iVar2}] = {itemVar};");
                }
                sb.AppendLine($"{indent}}}");
                return varName;
            }

            // 自定义类/结构体
            if (SupportedDataTypes.IsCustomType(type))
            {
                sb.AppendLine($"{indent}var {varName} = new {type}();");
                if (CustomTypeRegistry.TryGet(type, out var info))
                {
                    foreach (var member in info.Members)
                    {
                        var memberType = member.IsArray ? member.Type + "[]" : member.Type;
                        if (SupportedDataTypes.IsCollectionType(memberType) || SupportedDataTypes.IsCustomType(memberType))
                        {
                            var memberVar = EmitReadValue(sb, indent, memberType);
                            sb.AppendLine($"{indent}{varName}.{member.Name} = {memberVar};");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}{varName}.{member.Name} = {GetBinaryReadExpr(memberType)};");
                        }
                    }
                }
                return varName;
            }

            // 枚举
            if (SupportedDataTypes.IsEnumType(type))
            {
                sb.AppendLine($"{indent}var {varName} = {GetBinaryReadExpr(type)};");
                return varName;
            }

            // 基础类型
            sb.AppendLine($"{indent}var {varName} = {GetBinaryReadExpr(type)};");
            return varName;
        }

        /// <summary>
        /// 生成数组初始化表达式（支持交错数组，如 int[][] → new int[count][]）
        /// </summary>
        private static string GetArrayInitializer(string elementType, string countVar)
        {
            var elemCSharp = SupportedDataTypes.ResolveElementType(elementType);
            if (elemCSharp.EndsWith("[]", StringComparison.Ordinal))
            {
                // 元素本身是数组：去掉末尾 []，生成 new X[count][]
                return elemCSharp.Substring(0, elemCSharp.Length - 2) + $"[{countVar}][]";
            }
            return $"{elemCSharp}[{countVar}]";
        }

        /// <summary>
        /// 获取元素类型的二进制读取表达式（含枚举转换）
        /// </summary>
        private static string GetBinaryReadExpr(string type)
        {
            // 检查是否为枚举类型
            if (SupportedDataTypes.IsEnumType(type))
            {
                var enumTypeName = SupportedDataTypes.GetEnumTypeName(type);
                if (!string.IsNullOrEmpty(enumTypeName))
                {
                    return $"({enumTypeName})binaryReader.ReadInt32()";
                }
                return "binaryReader.ReadInt32()";
            }

            // 引用类型 @表名：按目标表主键类型读取
            type = SupportedDataTypes.ResolveReferenceType(type);

            switch (type.ToLower())
            {
                case SupportedDataTypes.Int:
                    return "binaryReader.ReadInt32()";
                case SupportedDataTypes.Float:
                    return "binaryReader.ReadSingle()";
                case SupportedDataTypes.String:
                    return "binaryReader.ReadString()";
                case SupportedDataTypes.Bool:
                    return "binaryReader.ReadBoolean()";
                case SupportedDataTypes.Long:
                    return "binaryReader.ReadInt64()";
                case SupportedDataTypes.Double:
                    return "binaryReader.ReadDouble()";
                case SupportedDataTypes.Byte:
                    return "binaryReader.ReadByte()";
                case SupportedDataTypes.Short:
                    return "binaryReader.ReadInt16()";
                default:
                    return "binaryReader.ReadString()";
            }
        }
    }
}

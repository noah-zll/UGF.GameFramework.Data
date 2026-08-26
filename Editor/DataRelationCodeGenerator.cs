using System;
using System.IO;
using System.Text;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 表关系访问扩展代码生成器。
    /// 根据关系集生成静态扩展类，为每个关系生成强类型查询方法：
    /// 一对一 GetXxx 返回单条，一对多/多对多 GetXxx 返回数组。
    /// </summary>
    public static class DataRelationCodeGenerator
    {
        /// <summary>
        /// 生成关系访问扩展代码文件
        /// </summary>
        /// <param name="set">关系集</param>
        /// <param name="dataRowNamespace">DR 类命名空间（如 GameData）</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="className">扩展类名</param>
        public static bool GenerateRelationExtension(TableRelationSet set, string dataRowNamespace, string outputDirectory, string className = "DataRelationExtension")
        {
            if (set == null)
            {
                Debug.LogWarning("关系集为空，无法生成代码");
                return false;
            }

            var code = GenerateCode(set, dataRowNamespace, className);
            Directory.CreateDirectory(outputDirectory);
            var path = Path.Combine(outputDirectory, className + ".g.cs");
            File.WriteAllText(path, code, Encoding.UTF8);
            Debug.Log($"关系访问扩展已生成: {path}");
            return true;
        }

        /// <summary>
        /// 生成代码内容
        /// </summary>
        public static string GenerateCode(TableRelationSet set, string dataRowNamespace, string className)
        {
            var sb = new StringBuilder();

            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine("// 此文件由工具自动生成，请勿手动修改。");
            sb.AppendLine("//------------------------------------------------------------");
            sb.AppendLine();
            sb.AppendLine("using GameFramework.DataTable;");
            sb.AppendLine("using UnityGameFramework.Runtime;");
            sb.AppendLine("using UGF.GameFramework.Data;");
            if (!string.IsNullOrEmpty(dataRowNamespace))
            {
                sb.AppendLine($"using {dataRowNamespace};");
            }
            sb.AppendLine();
            sb.AppendLine("public static class " + className);
            sb.AppendLine("{");

            foreach (var relation in set.Relations)
            {
                GenerateMethod(sb, relation);
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void GenerateMethod(StringBuilder sb, TableRelation relation)
        {
            var methodName = "Get" + ToPascalCase(relation.Name);
            var sourceDr = "DR" + relation.SourceTable;
            var targetDr = "DR" + relation.TargetTable;
            var sourceKey = string.IsNullOrEmpty(relation.SourceField) ? "Id" : relation.SourceField;
            var typeText = relation.RelationType == RelationType.OneToOne ? "一对一" :
                           relation.RelationType == RelationType.OneToMany ? "一对多" : "多对多";

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {typeText}：{relation.SourceTable} -> {relation.TargetTable}");
            if (!string.IsNullOrEmpty(relation.Description))
            {
                sb.AppendLine($"    /// {relation.Description}");
            }
            sb.AppendLine("    /// </summary>");

            if (relation.RelationType == RelationType.OneToOne)
            {
                sb.AppendLine($"    public static {targetDr} {methodName}(this {sourceDr} source)");
                sb.AppendLine("    {");
                sb.AppendLine($"        return GameEntry.GetComponent<DataTableComponent>().GetRelatedOne(\"{relation.Name}\", source.{sourceKey}) as {targetDr};");
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine($"    public static {targetDr}[] {methodName}(this {sourceDr} source)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var rows = GameEntry.GetComponent<DataTableComponent>().GetRelated(\"{relation.Name}\", source.{sourceKey});");
                sb.AppendLine($"        var result = new {targetDr}[rows.Length];");
                sb.AppendLine("        for (int i = 0; i < rows.Length; i++)");
                sb.AppendLine("        {");
                sb.AppendLine($"            result[i] = ({targetDr})rows[i];");
                sb.AppendLine("        }");
                sb.AppendLine("        return result;");
                sb.AppendLine("    }");
            }

            sb.AppendLine();
        }

        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Relation";

            var sb = new StringBuilder(name.Length);
            bool upperNext = true;
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
                    upperNext = false;
                }
                else
                {
                    upperNext = true;
                }
            }

            var result = sb.ToString();
            return result.Length == 0 ? "Relation" : result;
        }
    }
}

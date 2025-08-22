using System;
using System.IO;
using System.Text;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// DataRow代码生成器
    /// </summary>
    public static class DataRowCodeGenerator
    {
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
            
            var className = $"{tableInfo.TableName}DataRow";
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
            sb.AppendLine();
            
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
        /// 获取属性类型
        /// </summary>
        private static string GetPropertyType(string type)
        {
            // 检查是否为枚举类型
            if (SupportedDataTypes.IsEnumType(type))
            {
                var enumTypeName = SupportedDataTypes.GetEnumTypeName(type);
                return string.IsNullOrEmpty(enumTypeName) ? "int" : enumTypeName;
            }
            
            return SupportedDataTypes.GetCSharpType(type);
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
                if (SupportedDataTypes.IsEnumType(field.Type))
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
    }
}
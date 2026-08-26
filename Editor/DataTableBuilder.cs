using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UGF.GameFramework.Data;

namespace UGF.GameFramework.Data.Editor
{
    /// <summary>
    /// 数据表构建器
    /// </summary>
    public static class DataTableBuilder
    {
        /// <summary>
        /// 构建配置
        /// </summary>
        [Serializable]
        public class BuildConfig
        {
            /// <summary>
            /// Excel文件路径
            /// </summary>
            public string ExcelFilePath { get; set; }

            /// <summary>
            /// 工作表名称（为空则使用第一个工作表）
            /// </summary>
            public string SheetName { get; set; }

            /// <summary>
            /// 命名空间
            /// </summary>
            public string NamespaceName { get; set; } = "GameData";

            /// <summary>
            /// 代码输出路径
            /// </summary>
            public string CodeOutputPath { get; set; } = "Assets/Scripts/DataTables";

            /// <summary>
            /// 数据输出路径
            /// </summary>
            public string DataOutputPath { get; set; } = "Assets/StreamingAssets/DataTables";

            /// <summary>
            /// 是否生成代码
            /// </summary>
            public bool GenerateCode { get; set; } = true;

            /// <summary>
            /// 是否生成数据
            /// </summary>
            public bool GenerateData { get; set; } = true;

            /// <summary>
            /// 是否覆盖已存在的文件
            /// </summary>
            public bool OverwriteExisting { get; set; } = true;

            /// <summary>
            /// 是否自动生成枚举定义
            /// </summary>
            public bool GenerateEnums { get; set; } = true;

            /// <summary>
            /// 枚举代码输出路径
            /// </summary>
            public string EnumOutputPath { get; set; } = "Assets/Scripts/Enums";

            /// <summary>
            /// 类型定义文件路径（TypeDefinitions.xlsx，可选）。
            /// 配置后启用自定义类/结构体类型支持，用于解析、序列化与代码生成。
            /// </summary>
            public string TypeDefinitionFilePath { get; set; }

            /// <summary>
            /// 关系配置输出目录（relations.json，可选）。
            /// 配置后自动收集表内引用字段（@表名）生成隐式关系并合并到 relations.json。
            /// </summary>
            public string RelationOutputDirectory { get; set; } = "Assets/StreamingAssets/DataRelations";

            /// <summary>
            /// 是否在构建时校验引用完整性（目标表存在、主键类型匹配、值存在）
            /// </summary>
            public bool ValidateReferences { get; set; } = true;
        }

        /// <summary>
        /// 构建结果
        /// </summary>
        [Serializable]
        public class BuildResult
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// 错误信息
            /// </summary>
            public string ErrorMessage { get; set; }

            /// <summary>
            /// 生成的文件列表
            /// </summary>
            public List<string> GeneratedFiles { get; set; }

            /// <summary>
            /// 表格信息
            /// </summary>
            public ExcelTableInfo TableInfo { get; set; }

            public BuildResult()
            {
                GeneratedFiles = new List<string>();
            }
        }

        /// <summary>
        /// 构建单个数据表
        /// </summary>
        /// <param name="config">构建配置</param>
        /// <returns>构建结果</returns>
        public static BuildResult BuildDataTable(BuildConfig config)
        {
            var result = new BuildResult();

            try
            {
                // 验证配置
                if (!ValidateConfig(config, out string validationError))
                {
                    result.ErrorMessage = validationError;
                    return result;
                }

                // 加载类型定义（自定义类/结构体/枚举注册表）
                ReferenceTypeRegistry.Clear();
                LoadTypeDefinitions(config?.TypeDefinitionFilePath, config?.NamespaceName);

                // 解析Excel文件
                Debug.Log($"开始解析Excel文件: {config.ExcelFilePath}");
                var tableInfo = ExcelParser.ParseExcel(config.ExcelFilePath, config.SheetName);
                result.TableInfo = tableInfo;

                Debug.Log($"Excel解析完成，表名: {tableInfo.TableName}, 字段数: {tableInfo.Fields.Count}, 数据行数: {tableInfo.Rows.Count}");

                // 注册本表主键类型（供其他表引用 @本表）
                RegisterPrimaryKeyTypes(new[] { tableInfo });

                // 生成枚举/代码/数据
                BuildDataTableCore(tableInfo, config, result);

                // 隐式关系收集 + 引用校验 + 写关系配置
                var implicitRelations = new List<TableRelation>();
                CollectImplicitRelations(tableInfo, implicitRelations);
                ValidateReferences(tableInfo, BuildPrimaryKeySets(new[] { tableInfo }), config.ValidateReferences);
                WriteRelationsConfig(implicitRelations, config.RelationOutputDirectory);

                result.Success = true;
                Debug.Log($"数据表构建完成: {tableInfo.TableName}");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                Debug.LogError($"构建数据表失败: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 生成单个表的枚举/代码/数据（已解析的 tableInfo）
        /// </summary>
        private static void BuildDataTableCore(ExcelTableInfo tableInfo, BuildConfig config, BuildResult result)
        {
            // 生成枚举定义
            if (config.GenerateEnums)
            {
                var enumFiles = GenerateEnumFiles(tableInfo, config);
                result.GeneratedFiles.AddRange(enumFiles);
                if (enumFiles.Count > 0)
                {
                    Debug.Log($"已生成 {enumFiles.Count} 个枚举定义文件");
                }
            }

            // 生成代码
            if (config.GenerateCode)
            {
                var codeFilePath = GenerateCodeFile(tableInfo, config);
                result.GeneratedFiles.Add(codeFilePath);
                Debug.Log($"DataRow类已生成: {codeFilePath}");
            }

            // 生成数据
            if (config.GenerateData)
            {
                var dataFilePath = GenerateDataFile(tableInfo, config);
                result.GeneratedFiles.Add(dataFilePath);
                Debug.Log($"二进制数据已生成: {dataFilePath}");
            }
        }

        /// <summary>
        /// 加载类型定义文件到自定义类型注册表（类/结构体/枚举）
        /// </summary>
        public static void LoadTypeDefinitions(string typeDefinitionFilePath, string namespaceName)
        {
            CustomTypeRegistry.Clear();

            if (string.IsNullOrEmpty(typeDefinitionFilePath))
                return;

            if (!File.Exists(typeDefinitionFilePath))
            {
                Debug.LogWarning($"类型定义文件不存在，跳过自定义类型支持: {typeDefinitionFilePath}");
                return;
            }

            var result = TypeDefinitionParser.ParseTypeDefinitionFile(typeDefinitionFilePath, namespaceName);
            if (!result.Success)
            {
                Debug.LogError($"类型定义文件解析失败: {result.ErrorMessage}");
                return;
            }

            // 枚举
            foreach (var enumDef in result.Enums)
            {
                CustomTypeRegistry.Register(new CustomTypeInfo
                {
                    Name = enumDef.Name,
                    Kind = CustomTypeKind.Enum,
                    Namespace = enumDef.Namespace
                });
            }

            // 类
            foreach (var classDef in result.Classes)
            {
                var info = new CustomTypeInfo
                {
                    Name = classDef.Name,
                    Kind = CustomTypeKind.Class,
                    Namespace = classDef.Namespace
                };
                foreach (var property in classDef.Properties)
                {
                    info.Members.Add(new CustomTypeMemberInfo
                    {
                        Name = property.Name,
                        Type = property.Type,
                        IsArray = property.IsArray,
                        DefaultValue = property.DefaultValue
                    });
                }
                CustomTypeRegistry.Register(info);
            }

            // 结构体
            foreach (var structDef in result.Structs)
            {
                var info = new CustomTypeInfo
                {
                    Name = structDef.Name,
                    Kind = CustomTypeKind.Struct,
                    Namespace = structDef.Namespace
                };
                foreach (var field in structDef.Fields)
                {
                    info.Members.Add(new CustomTypeMemberInfo
                    {
                        Name = field.Name,
                        Type = field.Type,
                        IsArray = field.IsArray,
                        DefaultValue = field.DefaultValue
                    });
                }
                CustomTypeRegistry.Register(info);
            }

            Debug.Log($"已加载类型定义: {result.Enums.Count} 枚举, {result.Classes.Count} 类, {result.Structs.Count} 结构体");
        }

        /// <summary>
        /// 批量构建数据表
        /// </summary>
        /// <param name="configs">构建配置列表</param>
        /// <returns>构建结果列表</returns>
        public static List<BuildResult> BuildDataTables(List<BuildConfig> configs)
        {
            var results = new List<BuildResult>();

            foreach (var config in configs)
            {
                var result = BuildDataTable(config);
                results.Add(result);

                if (!result.Success)
                {
                    Debug.LogError($"构建失败: {config.ExcelFilePath} - {result.ErrorMessage}");
                }
            }

            return results;
        }

        /// <summary>
        /// 从目录批量构建
        /// 两阶段：先解析所有表填充引用类型注册表与主键集合，再逐个生成并校验引用
        /// </summary>
        /// <param name="excelDirectory">Excel文件目录</param>
        /// <param name="baseConfig">基础配置</param>
        /// <returns>构建结果列表</returns>
        public static List<BuildResult> BuildFromDirectory(string excelDirectory, BuildConfig baseConfig)
        {
            var results = new List<BuildResult>();

            if (!Directory.Exists(excelDirectory))
            {
                Debug.LogError($"Excel目录不存在: {excelDirectory}");
                return results;
            }

            if (baseConfig == null)
            {
                Debug.LogError("构建配置不能为空");
                return results;
            }

            var excelFiles = Directory.GetFiles(excelDirectory, "*.xlsx", SearchOption.AllDirectories);

            // 加载类型定义 + 清空引用注册表
            ReferenceTypeRegistry.Clear();
            LoadTypeDefinitions(baseConfig.TypeDefinitionFilePath, baseConfig.NamespaceName);

            // Phase 1: 解析所有表，填充引用注册表与主键集合
            var parsedTables = new List<ExcelTableInfo>();
            var primaryKeySets = new Dictionary<string, HashSet<object>>(StringComparer.Ordinal);

            foreach (var excelFile in excelFiles)
            {
                // 跳过临时文件
                if (Path.GetFileName(excelFile).StartsWith("~$"))
                    continue;

                try
                {
                    var tableInfo = ExcelParser.ParseExcel(excelFile, baseConfig.SheetName);
                    parsedTables.Add(tableInfo);
                    RegisterPrimaryKeyTypes(new[] { tableInfo });
                    primaryKeySets[tableInfo.TableName] = ExtractPrimaryKeys(tableInfo);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Excel解析失败，跳过: {excelFile} - {ex.Message}");
                }
            }

            // Phase 2: 逐个构建 + 收集隐式关系 + 校验引用
            var implicitRelations = new List<TableRelation>();

            foreach (var tableInfo in parsedTables)
            {
                var config = new BuildConfig
                {
                    ExcelFilePath = Path.GetDirectoryName(excelFiles.Length > 0 ? excelFiles[0] : excelDirectory),
                    SheetName = baseConfig.SheetName,
                    NamespaceName = baseConfig.NamespaceName,
                    CodeOutputPath = baseConfig.CodeOutputPath,
                    DataOutputPath = baseConfig.DataOutputPath,
                    GenerateCode = baseConfig.GenerateCode,
                    GenerateData = baseConfig.GenerateData,
                    OverwriteExisting = baseConfig.OverwriteExisting,
                    TypeDefinitionFilePath = baseConfig.TypeDefinitionFilePath,
                    RelationOutputDirectory = baseConfig.RelationOutputDirectory,
                    ValidateReferences = baseConfig.ValidateReferences
                };

                var result = new BuildResult { TableInfo = tableInfo };
                BuildDataTableCore(tableInfo, config, result);
                CollectImplicitRelations(tableInfo, implicitRelations);
                ValidateReferences(tableInfo, primaryKeySets, config.ValidateReferences);
                result.Success = true;
                results.Add(result);
            }

            // Phase 3: 写关系配置（合并隐式关系）
            WriteRelationsConfig(implicitRelations, baseConfig.RelationOutputDirectory);

            return results;
        }

        /// <summary>
        /// 注册一批表的主键类型到引用类型注册表
        /// </summary>
        private static void RegisterPrimaryKeyTypes(IEnumerable<ExcelTableInfo> tables)
        {
            if (tables == null)
                return;

            foreach (var tableInfo in tables)
            {
                if (tableInfo?.Fields == null)
                    continue;

                var pk = tableInfo.Fields.Find(f => f.IsPrimaryKey);
                if (pk != null && !string.IsNullOrEmpty(pk.Type))
                {
                    ReferenceTypeRegistry.Register(tableInfo.TableName, pk.Type);
                }
            }
        }

        /// <summary>
        /// 提取表的主键值集合（引用校验用）
        /// </summary>
        private static HashSet<object> ExtractPrimaryKeys(ExcelTableInfo tableInfo)
        {
            var set = new HashSet<object>();
            if (tableInfo == null || string.IsNullOrEmpty(tableInfo.PrimaryKeyField))
                return set;

            foreach (var row in tableInfo.Rows)
            {
                if (row.TryGetValue(tableInfo.PrimaryKeyField, out var value) && value != null)
                {
                    set.Add(value);
                }
            }
            return set;
        }

        private static Dictionary<string, HashSet<object>> BuildPrimaryKeySets(IEnumerable<ExcelTableInfo> tables)
        {
            var result = new Dictionary<string, HashSet<object>>(StringComparer.Ordinal);
            if (tables == null)
                return result;

            foreach (var tableInfo in tables)
            {
                result[tableInfo.TableName] = ExtractPrimaryKeys(tableInfo);
            }
            return result;
        }

        /// <summary>
        /// 收集表内引用字段（@表名）生成的隐式关系
        /// </summary>
        private static void CollectImplicitRelations(ExcelTableInfo tableInfo, List<TableRelation> relations)
        {
            if (tableInfo?.Fields == null || relations == null)
                return;

            foreach (var field in tableInfo.Fields)
            {
                var relation = CreateImplicitRelation(tableInfo, field);
                if (relation != null)
                {
                    relations.Add(relation);
                }
            }
        }

        private static TableRelation CreateImplicitRelation(ExcelTableInfo tableInfo, ExcelFieldInfo field)
        {
            string targetTable = null;
            var relationType = RelationType.OneToOne;

            if (SupportedDataTypes.IsReferenceType(field.Type))
            {
                targetTable = SupportedDataTypes.GetReferenceTargetTable(field.Type);
            }
            else if (SupportedDataTypes.IsCollectionType(field.Type) && !SupportedDataTypes.IsDictionaryType(field.Type))
            {
                var elementType = SupportedDataTypes.GetElementType(field.Type);
                if (SupportedDataTypes.IsReferenceType(elementType))
                {
                    targetTable = SupportedDataTypes.GetReferenceTargetTable(elementType);
                    relationType = RelationType.OneToMany;
                }
            }
            else if (SupportedDataTypes.IsDictionaryType(field.Type))
            {
                SupportedDataTypes.GetDictionaryTypes(field.Type, out var keyType, out var valueType);
                if (SupportedDataTypes.IsReferenceType(keyType) || SupportedDataTypes.IsReferenceType(valueType))
                {
                    targetTable = SupportedDataTypes.IsReferenceType(keyType)
                        ? SupportedDataTypes.GetReferenceTargetTable(keyType)
                        : SupportedDataTypes.GetReferenceTargetTable(valueType);
                }
            }

            if (string.IsNullOrEmpty(targetTable))
                return null;

            return new TableRelation
            {
                Name = $"{tableInfo.TableName}_{targetTable}_{field.Name}",
                RelationType = relationType,
                SourceTable = tableInfo.TableName,
                SourceField = field.Name,
                TargetTable = targetTable,
                TargetField = GetPrimaryKeyFieldName(targetTable),
                Description = $"隐式关系（字段 {field.Name} 引用 {targetTable}）"
            };
        }

        private static string GetPrimaryKeyFieldName(string tableName)
        {
            // 目标表主键字段名：运行时索引按主键建索引，这里标记目标表主键
            return "Id";
        }

        /// <summary>
        /// 构建时引用校验：目标表存在、主键类型匹配、值存在
        /// </summary>
        private static void ValidateReferences(ExcelTableInfo tableInfo,
            Dictionary<string, HashSet<object>> primaryKeySets, bool validate)
        {
            if (tableInfo?.Fields == null)
                return;

            foreach (var field in tableInfo.Fields)
            {
                if (!ContainsReference(field.Type))
                    continue;

                // 目标表存在与主键类型校验
                var targetTable = GetReferencedTarget(field.Type);
                if (string.IsNullOrEmpty(targetTable))
                    continue;

                if (!ReferenceTypeRegistry.IsRegistered(targetTable))
                {
                    Debug.LogError($"引用目标表不存在: {tableInfo.TableName}.{field.Name} 引用 @{targetTable}，目标表未参与构建");
                    continue;
                }

                // 主键类型匹配：引用字段解析后的类型需与目标表主键类型一致
                var resolvedType = GetReferenceResolvedType(field.Type);
                var targetPkType = ReferenceTypeRegistry.TryGetPrimaryKeyType(targetTable, out var pk) ? pk : "string";
                if (resolvedType != null && !string.Equals(resolvedType, targetPkType, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"引用主键类型不匹配: {tableInfo.TableName}.{field.Name}({field.Type} → {resolvedType}) 与 @{targetTable} 主键类型({targetPkType})不一致");
                }

                // 值存在校验
                if (!validate)
                    continue;

                if (!primaryKeySets.TryGetValue(targetTable, out var pkSet) || pkSet.Count == 0)
                    continue;

                foreach (var row in tableInfo.Rows)
                {
                    if (row.TryGetValue(field.Name, out var value) && value != null)
                    {
                        ValidateValuesRecursive(value, field.Type, targetTable, pkSet,
                            $"{tableInfo.TableName}.{field.Name}");
                    }
                }
            }
        }

        private static bool ContainsReference(string type)
        {
            if (SupportedDataTypes.IsReferenceType(type))
                return true;
            if (SupportedDataTypes.IsDictionaryType(type))
            {
                SupportedDataTypes.GetDictionaryTypes(type, out var k, out var v);
                return SupportedDataTypes.IsReferenceType(k) || SupportedDataTypes.IsReferenceType(v);
            }
            if (SupportedDataTypes.IsCollectionType(type))
            {
                return SupportedDataTypes.IsReferenceType(SupportedDataTypes.GetElementType(type));
            }
            return false;
        }

        private static string GetReferencedTarget(string type)
        {
            if (SupportedDataTypes.IsReferenceType(type))
                return SupportedDataTypes.GetReferenceTargetTable(type);
            if (SupportedDataTypes.IsDictionaryType(type))
            {
                SupportedDataTypes.GetDictionaryTypes(type, out var k, out var v);
                return SupportedDataTypes.IsReferenceType(k)
                    ? SupportedDataTypes.GetReferenceTargetTable(k)
                    : SupportedDataTypes.GetReferenceTargetTable(v);
            }
            if (SupportedDataTypes.IsCollectionType(type))
            {
                var elementType = SupportedDataTypes.GetElementType(type);
                return SupportedDataTypes.IsReferenceType(elementType)
                    ? SupportedDataTypes.GetReferenceTargetTable(elementType)
                    : null;
            }
            return null;
        }

        /// <summary>
        /// 提取字段类型中引用部分解析后的主键类型（用于与目标表主键类型比对）
        /// </summary>
        private static string GetReferenceResolvedType(string type)
        {
            if (SupportedDataTypes.IsDictionaryType(type))
            {
                SupportedDataTypes.GetDictionaryTypes(type, out var k, out var v);
                if (SupportedDataTypes.IsReferenceType(k))
                    return SupportedDataTypes.ResolveReferenceType(k);
                if (SupportedDataTypes.IsReferenceType(v))
                    return SupportedDataTypes.ResolveReferenceType(v);
                return null;
            }
            if (SupportedDataTypes.IsReferenceType(type))
                return SupportedDataTypes.ResolveReferenceType(type);
            if (SupportedDataTypes.IsCollectionType(type))
            {
                var elementType = SupportedDataTypes.GetElementType(type);
                return SupportedDataTypes.IsReferenceType(elementType)
                    ? SupportedDataTypes.ResolveReferenceType(elementType)
                    : null;
            }
            return null;
        }

        /// <summary>
        /// 递归校验引用值是否存在（标量、集合元素、字典键/值）
        /// </summary>
        private static void ValidateValuesRecursive(object value, string type, string targetTable,
            HashSet<object> pkSet, string context)
        {
            if (SupportedDataTypes.IsReferenceType(type))
            {
                if (value == null)
                    return;
                if (value is string str && string.IsNullOrEmpty(str))
                    return;
                if (!pkSet.Contains(value))
                {
                    Debug.LogError($"引用值不存在: {context} 值 {value} 不在 @{targetTable} 主键中");
                }
                return;
            }

            if (SupportedDataTypes.IsDictionaryType(type))
            {
                SupportedDataTypes.GetDictionaryTypes(type, out var keyType, out var valueType);
                if (value is System.Collections.IDictionary dict)
                {
                    var refKey = SupportedDataTypes.IsReferenceType(keyType);
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        ValidateValuesRecursive(refKey ? entry.Key : entry.Value,
                            refKey ? keyType : valueType, targetTable, pkSet, context);
                    }
                }
                return;
            }

            if (SupportedDataTypes.IsCollectionType(type))
            {
                var elementType = SupportedDataTypes.GetElementType(type);
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    foreach (var item in enumerable)
                    {
                        ValidateValuesRecursive(item, elementType, targetTable, pkSet, context);
                    }
                }
            }
        }

        /// <summary>
        /// 写出关系配置：合并显式（已有 relations.json）与隐式关系（按 Name 去重，显式优先）
        /// </summary>
        private static void WriteRelationsConfig(List<TableRelation> implicitRelations, string outputDirectory)
        {
            if (implicitRelations == null || implicitRelations.Count == 0)
                return;

            if (string.IsNullOrEmpty(outputDirectory))
                return;

            try
            {
                var absoluteDirectory = Path.GetFullPath(outputDirectory);
                if (!Directory.Exists(absoluteDirectory))
                {
                    Directory.CreateDirectory(absoluteDirectory);
                }

                var filePath = Path.Combine(absoluteDirectory, "relations.json");

                // 已有配置（编辑器导出的显式关系）
                var merged = new Dictionary<string, TableRelation>(StringComparer.Ordinal);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var existing = TableRelationConfig.FromJson(File.ReadAllText(filePath, System.Text.Encoding.UTF8));
                        if (existing?.relations != null)
                        {
                            foreach (var relation in existing.relations)
                            {
                                if (!string.IsNullOrEmpty(relation.Name))
                                {
                                    merged[relation.Name] = relation;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"读取既有关系配置失败，将仅写入隐式关系: {ex.Message}");
                    }
                }

                // 合并隐式关系（Name 冲突时显式优先，已存在的隐式同名跳过）
                foreach (var relation in implicitRelations)
                {
                    if (merged.ContainsKey(relation.Name))
                    {
                        Debug.LogWarning($"隐式关系与既有关系同名，已跳过: {relation.Name}");
                        continue;
                    }
                    merged[relation.Name] = relation;
                }

                var config = new TableRelationConfig();
                config.relations.AddRange(merged.Values);
                File.WriteAllText(filePath, config.ToJson(), System.Text.Encoding.UTF8);
                Debug.Log($"关系配置已生成: {filePath}（{config.relations.Count} 条）");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写关系配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证构建配置
        /// </summary>
        private static bool ValidateConfig(BuildConfig config, out string errorMessage)
        {
            errorMessage = null;

            if (config == null)
            {
                errorMessage = "构建配置不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(config.ExcelFilePath))
            {
                errorMessage = "Excel文件路径不能为空";
                return false;
            }

            if (!File.Exists(config.ExcelFilePath))
            {
                errorMessage = $"Excel文件不存在: {config.ExcelFilePath}";
                return false;
            }

            if (config.GenerateCode && string.IsNullOrEmpty(config.CodeOutputPath))
            {
                errorMessage = "代码输出路径不能为空";
                return false;
            }

            if (config.GenerateData && string.IsNullOrEmpty(config.DataOutputPath))
            {
                errorMessage = "数据输出路径不能为空";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成枚举定义文件
        /// </summary>
        private static List<string> GenerateEnumFiles(ExcelTableInfo tableInfo, BuildConfig config)
        {
            var generatedFiles = new List<string>();

            try
            {
                // 提取枚举信息
                var enumInfos = EnumCodeGenerator.ExtractEnumInfo(tableInfo);

                if (enumInfos.Count == 0)
                {
                    return generatedFiles;
                }

                var absoluteEnumPath = Path.GetFullPath(config.EnumOutputPath);

                // 确保输出目录存在
                if (!Directory.Exists(absoluteEnumPath))
                {
                    Directory.CreateDirectory(absoluteEnumPath);
                }

                // 生成每个枚举文件
                foreach (var kvp in enumInfos)
                {
                    var enumInfo = kvp.Value;

                    // 跳过没有值的枚举
                    if (enumInfo.Values.Count == 0)
                    {
                        Debug.LogWarning($"枚举 {enumInfo.Name} 没有找到任何值，跳过生成");
                        continue;
                    }

                    var fileName = $"{enumInfo.Name}.cs";
                    var filePath = Path.Combine(absoluteEnumPath, fileName);

                    // 检查文件是否已存在
                    if (File.Exists(filePath) && !config.OverwriteExisting)
                    {
                        Debug.LogWarning($"枚举文件已存在且不允许覆盖: {filePath}");
                        continue;
                    }

                    var code = EnumCodeGenerator.GenerateEnumCode(enumInfo, config.NamespaceName);
                    File.WriteAllText(filePath, code, System.Text.Encoding.UTF8);
                    generatedFiles.Add(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成枚举文件时发生错误: {ex.Message}");
                throw;
            }

            return generatedFiles;
        }

        /// <summary>
        /// 生成代码文件
        /// </summary>
        private static string GenerateCodeFile(ExcelTableInfo tableInfo, BuildConfig config)
        {
            var absoluteCodePath = Path.GetFullPath(config.CodeOutputPath);
            var className = string.IsNullOrEmpty(tableInfo.ClassName) ? $"DR{tableInfo.TableName}" : $"DR{tableInfo.ClassName}";
            var filePath = Path.Combine(absoluteCodePath, $"{className}.cs");

            // 检查文件是否已存在
            if (File.Exists(filePath) && !config.OverwriteExisting)
            {
                throw new InvalidOperationException($"代码文件已存在且不允许覆盖: {filePath}");
            }

            DataRowCodeGenerator.GenerateDataRowClass(tableInfo, config.NamespaceName, absoluteCodePath);
            return filePath;
        }

        /// <summary>
        /// 生成数据文件
        /// </summary>
        private static string GenerateDataFile(ExcelTableInfo tableInfo, BuildConfig config)
        {
            var absoluteDataPath = Path.GetFullPath(config.DataOutputPath);
            var fileName = string.IsNullOrEmpty(tableInfo.ClassName) ? $"{tableInfo.TableName}.bytes" : $"{tableInfo.ClassName}.bytes";
            var filePath = Path.Combine(absoluteDataPath, fileName);

            // 检查文件是否已存在
            if (File.Exists(filePath) && !config.OverwriteExisting)
            {
                throw new InvalidOperationException($"数据文件已存在且不允许覆盖: {filePath}");
            }

            try
            {
                BinaryDataSerializer.SerializeToBinary(tableInfo, absoluteDataPath);

                // 验证生成的文件
                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException($"二进制文件生成失败: {filePath}");
                }

                // 验证文件格式
                if (!BinaryDataSerializer.ValidateBinaryFile(filePath))
                {
                    throw new InvalidOperationException($"生成的二进制文件格式无效: {filePath}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"生成二进制数据文件时发生错误: {ex.Message}", ex);
            }

            return filePath;
        }

        /// <summary>
        /// 获取构建统计信息
        /// </summary>
        /// <param name="results">构建结果列表</param>
        /// <returns>统计信息</returns>
        public static string GetBuildStatistics(List<BuildResult> results)
        {
            var totalCount = results.Count;
            var successCount = 0;
            var failureCount = 0;
            var totalFiles = 0;

            foreach (var result in results)
            {
                if (result.Success)
                {
                    successCount++;
                    totalFiles += result.GeneratedFiles.Count;
                }
                else
                {
                    failureCount++;
                }
            }

            return $"构建统计: 总数 {totalCount}, 成功 {successCount}, 失败 {failureCount}, 生成文件 {totalFiles} 个";
        }
    }
}
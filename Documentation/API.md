# API 参考文档

## 核心类库

### ExcelDataStructure

#### ExcelFieldInfo

表示 Excel 表格中的字段信息。

```csharp
public class ExcelFieldInfo
{
    public string Name { get; set; }           // 字段名称
    public string Type { get; set; }           // 字段类型
    public string Description { get; set; }    // 字段描述
    public int ColumnIndex { get; set; }       // 列索引
    public bool IsPrimaryKey { get; set; }     // 是否为主键
}
```

#### ExcelTableInfo

表示完整的 Excel 表格信息。

```csharp
public class ExcelTableInfo
{
    public string TableName { get; set; }                    // 表格名称
    public List<ExcelFieldInfo> Fields { get; set; }         // 字段列表
    public List<Dictionary<string, object>> DataRows { get; set; } // 数据行
    public string PrimaryKeyField { get; set; }              // 主键字段名
}
```

#### SupportedDataTypes

定义支持的数据类型映射，包括对枚举类型的支持。

```csharp
public static class SupportedDataTypes
{
    // 基础数据类型常量
    public const string Int = "int";
    public const string Float = "float";
    public const string String = "string";
    public const string Bool = "bool";
    public const string Long = "long";
    public const string Double = "double";
    public const string Byte = "byte";
    public const string Short = "short";
    public const string Enum = "enum";
    
    // 所有支持的类型数组
    public static readonly string[] AllTypes;
    
    // 基础类型检查和转换
    public static bool IsSupported(string typeName);
    public static string GetCSharpType(string typeName);
    
    // 枚举类型支持 (新增)
    public static bool IsEnumType(string type);
    public static string GetEnumTypeName(string type);
}
```

### ExcelParser

Excel 文件解析器，提供静态方法解析 Excel 文件。

#### 方法

```csharp
public static class ExcelParser
{
    /// <summary>
    /// 解析 Excel 文件
    /// </summary>
    /// <param name="filePath">Excel 文件路径</param>
    /// <param name="worksheetName">工作表名称，null 表示第一个工作表</param>
    /// <returns>解析结果</returns>
    public static ExcelTableInfo ParseExcel(string filePath, string worksheetName = null);
    
    /// <summary>
    /// 验证 Excel 文件格式
    /// </summary>
    /// <param name="filePath">Excel 文件路径</param>
    /// <returns>验证结果</returns>
    public static bool ValidateExcelFormat(string filePath);
    
    /// <summary>
    /// 获取工作表名称列表
    /// </summary>
    /// <param name="filePath">Excel 文件路径</param>
    /// <returns>工作表名称列表</returns>
    public static List<string> GetWorksheetNames(string filePath);
}
```

#### 私有辅助方法

```csharp
private static List<ExcelFieldInfo> ParseFields(ExcelWorksheet worksheet);
private static List<Dictionary<string, object>> ParseDataRows(ExcelWorksheet worksheet, List<ExcelFieldInfo> fields);
private static object ConvertValue(object value, string targetType);
private static object GetDefaultValue(string typeName);
```

### DataRowCodeGenerator

DataRow 类代码生成器。

#### 方法

```csharp
public static class DataRowCodeGenerator
{
    /// <summary>
    /// 生成 DataRow 类代码
    /// </summary>
    /// <param name="tableInfo">表格信息</param>
    /// <param name="namespaceName">命名空间</param>
    /// <returns>生成的代码</returns>
    public static string GenerateDataRowClass(ExcelTableInfo tableInfo, string namespaceName);
    
    /// <summary>
    /// 生成类名
    /// </summary>
    /// <param name="tableName">表格名称</param>
    /// <returns>类名</returns>
    public static string GenerateClassName(string tableName);
    
    /// <summary>
    /// 验证字段名称
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    /// <returns>是否有效</returns>
    public static bool IsValidFieldName(string fieldName);
}
```

#### 私有辅助方法

```csharp
private static string GenerateProperties(List<ExcelFieldInfo> fields);
private static string GenerateParseMethod(List<ExcelFieldInfo> fields, string primaryKeyField);
private static string GetBinaryReadMethod(string typeName);
private static string FormatFieldName(string fieldName);
```

### BinaryDataSerializer

二进制数据序列化器。

#### 方法

```csharp
public static class BinaryDataSerializer
{
    /// <summary>
    /// 序列化表格数据为二进制格式
    /// </summary>
    /// <param name="tableInfo">表格信息</param>
    /// <param name="outputPath">输出文件路径</param>
    public static void SerializeToFile(ExcelTableInfo tableInfo, string outputPath);
    
    /// <summary>
    /// 序列化表格数据为字节数组
    /// </summary>
    /// <param name="tableInfo">表格信息</param>
    /// <returns>字节数组</returns>
    public static byte[] SerializeToBytes(ExcelTableInfo tableInfo);
    
    /// <summary>
    /// 验证二进制文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>验证结果</returns>
    public static bool ValidateBinaryFile(string filePath);
    
    /// <summary>
    /// 读取二进制文件信息
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件信息</returns>
    public static BinaryFileInfo ReadBinaryFileInfo(string filePath);
}
```

#### BinaryFileInfo

```csharp
public class BinaryFileInfo
{
    public string TableName { get; set; }        // 表格名称
    public int FieldCount { get; set; }          // 字段数量
    public int DataRowCount { get; set; }        // 数据行数量
    public long FileSize { get; set; }           // 文件大小
    public DateTime CreateTime { get; set; }     // 创建时间
}
```

#### 私有辅助方法

```csharp
private static void WriteHeader(BinaryWriter writer, ExcelTableInfo tableInfo);
private static void WriteFields(BinaryWriter writer, List<ExcelFieldInfo> fields);
private static void WriteDataRows(BinaryWriter writer, ExcelTableInfo tableInfo);
private static void WriteValue(BinaryWriter writer, object value, string typeName);
```

### DataTableBuilder

配置表构建管道。

#### BuildConfig

构建配置类。

```csharp
public class BuildConfig
{
    public string ExcelPath { get; set; }        // Excel 文件路径
    public string WorksheetName { get; set; }    // 工作表名称
    public string Namespace { get; set; }        // 命名空间
    public string CodeOutputPath { get; set; }   // 代码输出路径
    public string DataOutputPath { get; set; }   // 数据输出路径
    public bool OverwriteExisting { get; set; }  // 是否覆盖现有文件
    public bool GenerateCode { get; set; }       // 是否生成代码
    public bool GenerateData { get; set; }       // 是否生成数据
}
```

#### BuildResult

构建结果类。

```csharp
public class BuildResult
{
    public bool Success { get; set; }                    // 是否成功
    public string ErrorMessage { get; set; }            // 错误信息
    public List<string> GeneratedFiles { get; set; }    // 生成的文件列表
    public TimeSpan BuildTime { get; set; }             // 构建耗时
    public BuildStatistics Statistics { get; set; }     // 构建统计
}
```

#### BuildStatistics

构建统计信息。

```csharp
public class BuildStatistics
{
    public int FieldCount { get; set; }      // 字段数量
    public int DataRowCount { get; set; }    // 数据行数量
    public long CodeFileSize { get; set; }   // 代码文件大小
    public long DataFileSize { get; set; }   // 数据文件大小
}
```

#### 方法

```csharp
public static class DataTableBuilder
{
    /// <summary>
    /// 构建数据表
    /// </summary>
    /// <param name="config">构建配置</param>
    /// <returns>构建结果</returns>
    public static BuildResult BuildDataTable(BuildConfig config);
    
    /// <summary>
    /// 批量构建数据表
    /// </summary>
    /// <param name="configs">构建配置列表</param>
    /// <returns>构建结果列表</returns>
    public static List<BuildResult> BuildDataTables(List<BuildConfig> configs);
    
    /// <summary>
    /// 从目录构建数据表
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="baseConfig">基础配置</param>
    /// <returns>构建结果列表</returns>
    public static List<BuildResult> BuildFromDirectory(string directoryPath, BuildConfig baseConfig);
    
    /// <summary>
    /// 验证构建配置
    /// </summary>
    /// <param name="config">构建配置</param>
    /// <returns>验证结果</returns>
    public static ValidationResult ValidateConfig(BuildConfig config);
}
```

#### ValidationResult

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }                // 是否有效
    public List<string> Errors { get; set; }         // 错误列表
    public List<string> Warnings { get; set; }       // 警告列表
}
```

### DataTableBuilderWindow

Unity 编辑器窗口。

#### 方法

```csharp
public class DataTableBuilderWindow : EditorWindow
{
    /// <summary>
    /// 显示窗口
    /// </summary>
    [MenuItem("Window/UGF/Data Table Builder")]
    public static void ShowWindow();
    
    /// <summary>
    /// 绘制 GUI
    /// </summary>
    private void OnGUI();
    
    /// <summary>
    /// 解析 Excel 文件
    /// </summary>
    private void ParseExcel();
    
    /// <summary>
    /// 生成代码
    /// </summary>
    private void GenerateCode();
    
    /// <summary>
    /// 生成数据
    /// </summary>
    private void GenerateData();
    
    /// <summary>
    /// 一键构建
    /// </summary>
    private void BuildAll();
    
    /// <summary>
    /// 从目录构建
    /// </summary>
    private void BuildFromDirectory();
}
```

## 使用示例

### 基本用法

```csharp
// 解析 Excel 文件
var tableInfo = ExcelParser.ParseExcel("path/to/excel.xlsx");

// 生成代码
string code = DataRowCodeGenerator.GenerateDataRowClass(tableInfo, "Game.DataTables");
File.WriteAllText("ItemConfigDataRow.cs", code);

// 生成二进制数据
BinaryDataSerializer.SerializeToFile(tableInfo, "ItemConfig.bytes");
```

### 使用构建管道

```csharp
var config = new DataTableBuilder.BuildConfig
{
    ExcelPath = "Assets/Configs/ItemConfig.xlsx",
    Namespace = "Game.DataTables",
    CodeOutputPath = "Assets/Scripts/DataTables",
    DataOutputPath = "Assets/GameMain/DataTables",
    GenerateCode = true,
    GenerateData = true
};

var result = DataTableBuilder.BuildDataTable(config);
if (result.Success)
{
    Debug.Log($"构建成功，生成文件: {string.Join(", ", result.GeneratedFiles)}");
}
else
{
    Debug.LogError($"构建失败: {result.ErrorMessage}");
}
```

### 批量构建

```csharp
var baseConfig = new DataTableBuilder.BuildConfig
{
    Namespace = "Game.DataTables",
    CodeOutputPath = "Assets/Scripts/DataTables",
    DataOutputPath = "Assets/GameMain/DataTables"
};

var results = DataTableBuilder.BuildFromDirectory("Assets/Configs", baseConfig);
foreach (var result in results)
{
    if (result.Success)
    {
        Debug.Log($"构建成功: {result.GeneratedFiles.Count} 个文件");
    }
    else
    {
        Debug.LogError($"构建失败: {result.ErrorMessage}");
    }
}
```

## 错误处理

所有公共方法都包含适当的错误处理和异常捕获。建议在使用时检查返回结果：

```csharp
try
{
    var tableInfo = ExcelParser.ParseExcel(excelPath);
    if (tableInfo == null)
    {
        Debug.LogError("Excel 解析失败");
        return;
    }
    
    // 继续处理...
}
catch (Exception ex)
{
    Debug.LogError($"处理过程中发生错误: {ex.Message}");
}
```

## 扩展接口

如果需要扩展功能，可以实现以下接口：

```csharp
// 自定义数据类型转换器
public interface IDataTypeConverter
{
    bool CanConvert(string typeName);
    object Convert(object value, string typeName);
    string GetDefaultValue(string typeName);
}

// 自定义代码生成器
public interface ICodeGenerator
{
    string GenerateCode(ExcelTableInfo tableInfo, string namespaceName);
    string GetFileExtension();
}

// 自定义序列化器
public interface IDataSerializer
{
    void Serialize(ExcelTableInfo tableInfo, string outputPath);
    byte[] SerializeToBytes(ExcelTableInfo tableInfo);
}
```
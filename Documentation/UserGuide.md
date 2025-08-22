# UGF GameFramework Data Tools 使用指南

## 概述

UGF GameFramework Data Tools 是为原生 GameFramework 框架设计的配置表工具链，提供从 Excel 文件到 GameFramework 数据表的完整转换流程。

## 功能特性

### 1. Excel 解析
- 支持 .xlsx 格式的 Excel 文件
- 自动识别字段名称、类型和描述
- 支持多种数据类型：int、long、float、double、bool、string
- 支持主键字段标识

### 2. 代码生成
- 自动生成符合 GameFramework 规范的 DataRow 类
- 支持自定义命名空间
- 生成的代码包含完整的属性和解析方法
- 支持二进制数据读取

### 3. 二进制序列化
- 将 Excel 数据转换为 GameFramework 支持的二进制格式
- 优化的文件大小和加载性能
- 包含数据完整性验证

### 4. 编辑器集成
- Unity 编辑器窗口界面
- 可视化配置和预览
- 一键构建功能
- 批量处理支持

## Excel 格式规范

### 基本格式

```
第1行：字段名称 (Id, Name, Type, ...)
第2行：字段类型 (int, string, float, ...)
第3行：字段描述 (物品ID, 物品名称, 物品类型, ...) [可选]
第4行开始：数据内容
```

### 示例表格

| Id | Name | Type | Quality | Price | Description |
|----|------|------|---------|-------|-------------|
| int | string | int | int | float | string |
| 物品ID | 物品名称 | 物品类型 | 品质等级 | 价格 | 物品描述 |
| 1001 | 铁剑 | 1 | 1 | 100.0 | 一把普通的铁制长剑 |
| 1002 | 钢剑 | 1 | 2 | 250.0 | 锋利的钢制长剑 |

### 支持的数据类型

- **int**: 32位整数
- **long**: 64位整数
- **float**: 单精度浮点数
- **double**: 双精度浮点数
- **bool**: 布尔值 (true/false, 1/0)
- **string**: 字符串

### 主键字段

- 第一个字段默认为主键
- 主键字段必须是唯一的
- 通常使用 int 或 long 类型

## 使用步骤

### 1. 准备 Excel 文件

1. 创建 Excel 文件，按照格式规范填写数据
2. 确保第一行是字段名称，第二行是字段类型
3. 保存为 .xlsx 格式

### 2. 打开工具窗口

在 Unity 编辑器中：
1. 点击菜单 `Window > UGF > Data Table Builder`
2. 工具窗口将会打开

### 3. 配置参数

在工具窗口中配置以下参数：

- **Excel 文件路径**: 选择要转换的 Excel 文件
- **工作表名称**: 指定要解析的工作表（默认为第一个）
- **命名空间**: 生成代码的命名空间（如：Game.DataTables）
- **代码输出路径**: DataRow 类的输出目录
- **数据输出路径**: 二进制数据文件的输出目录

### 4. 预览和构建

1. 点击 "解析 Excel" 预览数据结构
2. 检查字段信息是否正确
3. 点击 "一键构建" 生成代码和数据

### 5. 在项目中使用

生成的文件包括：
- `{TableName}DataRow.cs`: DataRow 类文件
- `{TableName}.bytes`: 二进制数据文件

## 代码使用示例

### 1. 加载数据表

```csharp
using UnityGameFramework.Runtime;
using GameFramework.DataTable;

// 获取数据表组件
DataTableComponent dataTableComponent = GameEntry.GetComponent<DataTableComponent>();

// 加载数据表
dataTableComponent.LoadDataTable<ItemConfigDataRow>("ItemConfig", "DataTables/ItemConfig");
```

### 2. 获取数据

```csharp
// 获取数据表
IDataTable<ItemConfigDataRow> itemTable = dataTableComponent.GetDataTable<ItemConfigDataRow>();

// 根据ID获取单条数据
ItemConfigDataRow item = itemTable.GetDataRow(1001);
if (item != null)
{
    Debug.Log($"物品名称: {item.Name}, 价格: {item.Price}");
}

// 获取所有数据
ItemConfigDataRow[] allItems = itemTable.GetAllDataRows();
foreach (var itemData in allItems)
{
    Debug.Log($"ID: {itemData.Id}, 名称: {itemData.Name}");
}
```

### 3. 条件查询

```csharp
// 查找特定类型的物品
var weapons = itemTable.GetAllDataRows().Where(item => item.Type == 1).ToArray();

// 查找特定品质的物品
var legendaryItems = itemTable.GetAllDataRows().Where(item => item.Quality >= 5).ToArray();
```

## 高级功能

### 二进制数据查看器

工具提供了专门的二进制数据查看器，用于查看和分析生成的二进制数据文件：

#### 打开查看器

有两种方式打开二进制数据查看器：

1. **通过菜单栏**：
   - 选择 `Window > UGF > Binary Data Viewer`

2. **通过数据表构建器**：
   - 在数据表构建器窗口中点击 "查看二进制数据" 按钮

#### 主要功能

- **文件加载**: 支持加载 `.bytes` 格式的二进制数据文件
- **数据结构分析**: 自动解析数据表头部信息和记录结构
- **十六进制视图**: 完整的十六进制数据显示，支持搜索
- **记录详情**: 查看单条记录的详细信息和解析字段
- **文件信息**: 显示文件大小、路径和数据表统计信息

#### 使用场景

- **数据验证**: 检查生成的二进制数据是否正确
- **问题调试**: 分析数据序列化或解析问题
- **格式分析**: 了解 GameFramework 数据表的二进制格式
- **性能分析**: 查看数据文件大小和结构优化空间

详细使用方法请参考 [二进制数据查看器使用指南](BinaryDataViewer.md)。

### 批量构建

工具支持批量处理多个 Excel 文件：

1. 将多个 Excel 文件放在同一目录下
2. 在工具窗口中选择 "从目录构建"
3. 选择包含 Excel 文件的目录
4. 工具将自动处理所有 .xlsx 文件

### 自定义配置

可以通过修改 `DataTableBuilder.BuildConfig` 来自定义构建参数：

```csharp
var config = new DataTableBuilder.BuildConfig
{
    ExcelPath = "path/to/excel.xlsx",
    WorksheetName = "Sheet1",
    Namespace = "Game.DataTables",
    CodeOutputPath = "Assets/Scripts/DataTables",
    DataOutputPath = "Assets/GameMain/DataTables"
};

var result = DataTableBuilder.BuildDataTable(config);
if (result.Success)
{
    Debug.Log("构建成功!");
}
else
{
    Debug.LogError($"构建失败: {result.ErrorMessage}");
}
```

## 常见问题

### Q: Excel 文件解析失败
A: 请检查：
- Excel 文件格式是否为 .xlsx
- 第一行是否为字段名称
- 第二行是否为字段类型
- 字段类型是否为支持的类型

### Q: 生成的代码编译错误
A: 请检查：
- 命名空间是否正确
- 字段名称是否符合 C# 命名规范
- 是否有重复的字段名称

### Q: 数据表加载失败
A: 请检查：
- 二进制文件路径是否正确
- DataRow 类是否正确生成
- 是否正确引用了程序集

### Q: 数据读取异常
A: 请检查：
- Excel 中的数据类型是否与定义一致
- 是否有空值或格式错误的数据
- 主键是否唯一

## 性能优化建议

1. **合理设计表结构**: 避免过多的字段和过大的数据量
2. **使用合适的数据类型**: 根据实际需要选择数据类型
3. **预加载常用表**: 在游戏启动时预加载常用的数据表
4. **按需加载**: 对于大型数据表，考虑按需加载

## 扩展开发

如果需要扩展工具功能，可以：

1. 修改 `ExcelParser` 支持更多数据类型
2. 扩展 `DataRowCodeGenerator` 生成更复杂的代码
3. 优化 `BinaryDataSerializer` 的序列化格式
4. 增强 `DataTableBuilderWindow` 的用户界面

## 技术支持

如果在使用过程中遇到问题，请：

1. 查看 Unity Console 中的错误信息
2. 检查生成的日志文件
3. 参考示例文件和文档
4. 联系技术支持团队
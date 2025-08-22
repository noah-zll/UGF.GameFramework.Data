# 示例文件

本目录包含UGF GameFramework Data Tools的示例文件，帮助您快速了解如何使用配置表工具链。

## 示例文件说明

### Excel示例文件

- `ItemConfig.xlsx` - 物品配置表示例
- `PlayerConfig.xlsx` - 玩家配置表示例
- `SkillConfig.xlsx` - 技能配置表示例

### 生成的代码示例

- `Generated/` - 包含根据Excel文件生成的DataRow类

### 生成的数据示例

- `Data/` - 包含生成的二进制数据表文件

## 快速开始

1. 将示例Excel文件复制到您的项目中
2. 打开 `Window > UGF > Data Table Builder`
3. 选择Excel文件并配置输出路径
4. 点击"一键构建"生成代码和数据
5. 在GameFramework中使用生成的数据表

## Excel格式规范

请参考示例Excel文件了解正确的格式：

- 第1行：字段名称
- 第2行：字段类型
- 第3行：字段描述（可选）
- 第4行开始：数据内容

## 在GameFramework中使用

```csharp
// 获取数据表组件
var dataTableComponent = GameEntry.GetComponent<DataTableComponent>();

// 加载数据表
dataTableComponent.LoadDataTable<ItemConfigDataRow>("ItemConfig", "DataTables/ItemConfig");

// 获取数据
var itemTable = dataTableComponent.GetDataTable<ItemConfigDataRow>();
var item = itemTable.GetDataRow(1001); // 获取ID为1001的物品
```
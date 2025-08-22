# 数据类型定义表格式规范

## 概述

数据类型定义表是一种专门用于定义代码结构（类、枚举等）的Excel表格格式，与传统的数据表分离，专注于类型定义和代码生成。

## 表格结构

### 工作表命名规范

- **枚举定义表**：`Enums` 或 `EnumDefinitions`
- **类定义表**：`Classes` 或 `ClassDefinitions`
- **结构体定义表**：`Structs` 或 `StructDefinitions`

### 枚举定义表格式

#### 表头结构
| TypeName | ValueName | Value | Description | Category |
|----------|-----------|-------|-------------|----------|
| string   | string    | int   | string      | string   |
| 枚举类型名 | 枚举值名称 | 枚举值 | 描述        | 分类     |

#### 示例数据
| TypeName | ValueName | Value | Description | Category |
|----------|-----------|-------|-------------|----------|
| string   | string    | int   | string      | string   |
| 枚举类型名 | 枚举值名称 | 枚举值 | 描述        | 分类     |
| ItemType    | Weapon    | 1     | 武器类型    | Equipment |
| ItemType    | Armor     | 2     | 护甲类型    | Equipment |
| ItemType    | Potion    | 3     | 药水类型    | Consumable |
| ItemQuality | Common    | 0     | 普通品质    | Quality   |
| ItemQuality | Rare      | 1     | 稀有品质    | Quality   |
| ItemQuality | Epic      | 2     | 史诗品质    | Quality   |
| ItemQuality | Legendary | 3     | 传说品质    | Quality   |

### 类定义表格式

#### 表头结构
| ClassName | PropertyName | PropertyType | IsArray | Description | DefaultValue | Attributes |
|-----------|--------------|--------------|---------|-------------|--------------|------------|
| string    | string       | string       | bool    | string      | string       | string     |
| 类名      | 属性名       | 属性类型     | 是否数组 | 描述        | 默认值       | 特性       |

#### 示例数据
| ClassName | PropertyName | PropertyType | IsArray | Description | DefaultValue | Attributes |
|-----------|--------------|--------------|---------|-------------|--------------|------------|
| string    | string       | string       | bool    | string      | string       | string     |
| 类名      | 属性名       | 属性类型     | 是否数组 | 描述        | 默认值       | 特性       |
| ItemData  | Id           | int          | false   | 物品ID      | 0            | [Key]      |
| ItemData  | Name         | string       | false   | 物品名称    | ""           |            |
| ItemData  | Type         | ItemType     | false   | 物品类型    | ItemType.Weapon |         |
| ItemData  | Quality      | ItemQuality  | false   | 物品品质    | ItemQuality.Common |      |
| ItemData  | Price        | float        | false   | 价格        | 0.0f         |            |
| ItemData  | Tags         | string       | true    | 标签列表    |              |            |
| PlayerData| Level        | int          | false   | 等级        | 1            |            |
| PlayerData| Experience   | long         | false   | 经验值      | 0            |            |
| PlayerData| Skills       | int          | true    | 技能ID列表  |              |            |

### 结构体定义表格式

#### 表头结构
| StructName | FieldName | FieldType | IsArray | Description | DefaultValue |
|------------|-----------|-----------|---------|-------------|-------------|
| string     | string    | string    | bool    | string      | string      |
| 结构体名   | 字段名    | 字段类型  | 是否数组 | 描述        | 默认值      |

#### 示例数据
| StructName | FieldName | FieldType | IsArray | Description | DefaultValue |
|------------|-----------|-----------|---------|-------------|-------------|
| string     | string    | string    | bool    | string      | string      |
| 结构体名   | 字段名    | 字段类型  | 是否数组 | 描述        | 默认值      |
| Vector3Int | X         | int       | false   | X坐标       | 0           |
| Vector3Int | Y         | int       | false   | Y坐标       | 0           |
| Vector3Int | Z         | int       | false   | Z坐标       | 0           |
| ItemStack  | ItemId    | int       | false   | 物品ID      | 0           |
| ItemStack  | Count     | int       | false   | 数量        | 1           |

## 字段说明

### 枚举定义表字段

- **TypeName**：枚举类型名称，相同名称的行会被归并为一个枚举
- **ValueName**：枚举值名称，必须是有效的C#标识符
- **Value**：枚举值的数值，可选，如果不指定则自动递增
- **Description**：枚举值的描述，用于生成注释
- **Category**：枚举值的分类，用于组织和管理

### 类定义表字段

- **ClassName**：类名称，相同名称的行会被归并为一个类
- **PropertyName**：属性名称，必须是有效的C#标识符
- **PropertyType**：属性类型，支持基础类型和自定义类型
- **IsArray**：是否为数组类型，true表示该属性是数组
- **Description**：属性描述，用于生成注释
- **DefaultValue**：默认值，用于属性初始化
- **Attributes**：C#特性，如[Key]、[Required]等

### 结构体定义表字段

- **StructName**：结构体名称，相同名称的行会被归并为一个结构体
- **FieldName**：字段名称，必须是有效的C#标识符
- **FieldType**：字段类型，支持基础类型和自定义类型
- **IsArray**：是否为数组类型
- **Description**：字段描述，用于生成注释
- **DefaultValue**：默认值，用于字段初始化

## 支持的数据类型

### 基础类型
- `int`, `long`, `float`, `double`, `bool`, `string`
- `byte`, `short`, `uint`, `ulong`, `ushort`
- `decimal`, `char`, `DateTime`

### 数组类型
- 在IsArray列标记为true，或在类型后添加`[]`
- 例如：`int[]`, `string[]`

### 自定义类型
- 枚举类型：在枚举定义表中定义的类型
- 类类型：在类定义表中定义的类型
- 结构体类型：在结构体定义表中定义的类型

### 可空类型
- 在类型后添加`?`，例如：`int?`, `DateTime?`

## 生成规则

### 枚举生成规则
1. 按TypeName分组生成独立的枚举文件
2. 如果Value列为空，则按出现顺序自动分配数值（从0开始）
3. 生成的枚举包含完整的XML注释
4. 支持指定命名空间

### 类生成规则
1. 按ClassName分组生成独立的类文件
2. 属性按定义顺序排列
3. 自动生成构造函数（如果有默认值）
4. 支持继承和接口实现（通过Attributes字段）
5. 生成的类包含完整的XML注释

### 结构体生成规则
1. 按StructName分组生成独立的结构体文件
2. 字段按定义顺序排列
3. 自动生成构造函数
4. 生成的结构体包含完整的XML注释

## 使用示例

### 创建类型定义表
1. 创建名为`TypeDefinitions.xlsx`的Excel文件
2. 创建`Enums`、`Classes`、`Structs`工作表
3. 按照格式规范填写类型定义
4. 在DataTableBuilder中选择该文件进行代码生成

### 生成的代码示例

```csharp
// ItemType.cs
namespace YourNamespace
{
    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// 武器类型
        /// </summary>
        Weapon = 1,
        
        /// <summary>
        /// 护甲类型
        /// </summary>
        Armor = 2,
        
        /// <summary>
        /// 药水类型
        /// </summary>
        Potion = 3
    }
}

// ItemData.cs
namespace YourNamespace
{
    /// <summary>
    /// 物品数据类
    /// </summary>
    public class ItemData
    {
        /// <summary>
        /// 物品ID
        /// </summary>
        [Key]
        public int Id { get; set; } = 0;
        
        /// <summary>
        /// 物品名称
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// 物品类型
        /// </summary>
        public ItemType Type { get; set; } = ItemType.Weapon;
        
        /// <summary>
        /// 物品品质
        /// </summary>
        public ItemQuality Quality { get; set; } = ItemQuality.Common;
        
        /// <summary>
        /// 价格
        /// </summary>
        public float Price { get; set; } = 0.0f;
        
        /// <summary>
        /// 标签列表
        /// </summary>
        public string[] Tags { get; set; }
    }
}
```

## 优势

1. **类型安全**：预定义的类型确保数据一致性
2. **性能优化**：不需要遍历大量数据来推断类型
3. **维护性好**：类型定义集中管理，易于维护
4. **版本控制友好**：类型变更可以明确追踪
5. **团队协作**：类型定义可以作为接口规范
6. **代码复用**：生成的类型可以在多个数据表中使用
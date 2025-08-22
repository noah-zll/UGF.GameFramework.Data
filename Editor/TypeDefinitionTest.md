# 类型定义表功能测试

## 测试目标
验证从类型定义表生成代码（枚举、类、结构体）的功能是否正常工作。

## 测试环境
- Unity Editor
- UGF.GameFramework.Data 数据表构建器
- Excel 文件支持

## 测试用例

### 1. 枚举定义测试

#### 测试数据（Enum工作表）
```
| Name        | Type | Description      | Value | Comment           |
|-------------|------|------------------|-------|-------------------|
| PlayerState | enum | 玩家状态枚举     |       |                   |
| Idle        |      | 空闲状态         | 0     | 玩家处于空闲状态  |
| Moving      |      | 移动状态         | 1     | 玩家正在移动      |
| Fighting    |      | 战斗状态         | 2     | 玩家正在战斗      |
| Dead        |      | 死亡状态         | 3     | 玩家已死亡        |
```

#### 预期生成代码
```csharp
namespace GameData
{
    /// <summary>
    /// 玩家状态枚举
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// 空闲状态 - 玩家处于空闲状态
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 移动状态 - 玩家正在移动
        /// </summary>
        Moving = 1,
        
        /// <summary>
        /// 战斗状态 - 玩家正在战斗
        /// </summary>
        Fighting = 2,
        
        /// <summary>
        /// 死亡状态 - 玩家已死亡
        /// </summary>
        Dead = 3
    }
}
```

### 2. 类定义测试

#### 测试数据（Class工作表）
```
| Name       | Type   | Description    | DefaultValue | Comment        |
|------------|--------|----------------|--------------|----------------|
| PlayerInfo | class  | 玩家信息类     |              |                |
| Id         | int    | 玩家ID         | 0            | 唯一标识符     |
| Name       | string | 玩家名称       | ""           | 玩家显示名称   |
| Level      | int    | 玩家等级       | 1            | 当前等级       |
| Experience | float  | 经验值         | 0.0f         | 当前经验值     |
```

#### 预期生成代码
```csharp
namespace GameData
{
    /// <summary>
    /// 玩家信息类
    /// </summary>
    public class PlayerInfo
    {
        /// <summary>
        /// 玩家ID - 唯一标识符
        /// </summary>
        public int Id { get; set; } = 0;
        
        /// <summary>
        /// 玩家名称 - 玩家显示名称
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// 玩家等级 - 当前等级
        /// </summary>
        public int Level { get; set; } = 1;
        
        /// <summary>
        /// 经验值 - 当前经验值
        /// </summary>
        public float Experience { get; set; } = 0.0f;
    }
}
```

### 3. 结构体定义测试

#### 测试数据（Struct工作表）
```
| Name     | Type   | Description | DefaultValue | Comment      |
|----------|--------|-------------|--------------|-------------|
| Vector2D | struct | 二维向量    |              |              |
| X        | float  | X坐标       | 0.0f         | 横坐标       |
| Y        | float  | Y坐标       | 0.0f         | 纵坐标       |
```

#### 预期生成代码
```csharp
namespace GameData
{
    /// <summary>
    /// 二维向量
    /// </summary>
    public struct Vector2D
    {
        /// <summary>
        /// X坐标 - 横坐标
        /// </summary>
        public float X;
        
        /// <summary>
        /// Y坐标 - 纵坐标
        /// </summary>
        public float Y;
        
        public Vector2D(float x = 0.0f, float y = 0.0f)
        {
            X = x;
            Y = y;
        }
    }
}
```

## 测试步骤

### 步骤1：准备测试数据
1. 创建一个新的Excel文件 `TypeDefinitionTest.xlsx`
2. 创建三个工作表：`Enum`、`Class`、`Struct`
3. 按照上述格式填入测试数据

### 步骤2：使用数据表构建器
1. 打开Unity Editor
2. 选择菜单 `Window > UGF > Data Table Builder`
3. 在类型定义表区域：
   - 启用类型定义表功能
   - 选择测试Excel文件
   - 设置输出路径为 `Assets/Scripts/Generated`
   - 点击"生成类型定义代码"按钮

### 步骤3：验证生成结果
1. 检查输出目录是否生成了对应的C#文件：
   - `PlayerState.cs`
   - `PlayerInfo.cs`
   - `Vector2D.cs`
2. 验证生成的代码是否符合预期格式
3. 检查代码是否能正常编译
4. 验证命名空间、注释、默认值等是否正确

## 预期结果

### 成功标准
- [ ] 所有类型定义文件成功生成
- [ ] 生成的代码格式正确
- [ ] 代码能够正常编译
- [ ] 注释和文档完整
- [ ] 默认值设置正确
- [ ] 命名空间正确

### 错误处理测试
- [ ] 无效Excel文件路径的错误提示
- [ ] 格式错误的Excel数据的错误处理
- [ ] 输出目录权限问题的处理
- [ ] 重复类型名称的冲突处理

## 性能测试

### 大数据量测试
- 测试包含100个枚举值的枚举定义
- 测试包含50个属性的类定义
- 测试同时生成多种类型的混合场景

### 预期性能指标
- 生成时间应在5秒内完成
- 内存使用应保持在合理范围
- 不应出现内存泄漏

## 集成测试

### 与现有功能的兼容性
- 验证类型定义功能不影响原有的数据表生成功能
- 验证生成的枚举可以在数据表中正常使用
- 验证生成的类可以作为数据表的字段类型

## 测试报告模板

```
测试日期：____年__月__日
测试人员：__________
测试环境：Unity ______版本

测试结果：
□ 通过  □ 失败

详细结果：
1. 枚举生成测试：□ 通过  □ 失败
2. 类生成测试：  □ 通过  □ 失败
3. 结构体生成测试：□ 通过  □ 失败
4. 错误处理测试：□ 通过  □ 失败
5. 性能测试：    □ 通过  □ 失败
6. 集成测试：    □ 通过  □ 失败

问题记录：
_________________________________
_________________________________
_________________________________

建议改进：
_________________________________
_________________________________
_________________________________
```

## 注意事项

1. **Excel格式要求**：严格按照定义的格式创建测试数据
2. **命名规范**：确保类型名称符合C#命名规范
3. **类型支持**：验证所有支持的数据类型都能正确处理
4. **编码问题**：注意中文注释的编码处理
5. **路径问题**：确保输出路径的正确性和权限

通过以上测试用例，可以全面验证类型定义表功能的正确性和稳定性。
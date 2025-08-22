# UGF GameFramework Data Tools

为原生GameFramework提供完整的配置表工具链，支持从Excel文件生成二进制数据表。

## 功能特性

- **Excel解析**: 支持读取Excel文件(.xlsx)并解析为数据结构
- **代码生成**: 根据Excel结构自动生成DataRow类
- **二进制序列化**: 将Excel数据转换为GameFramework支持的二进制格式
- **编辑器集成**: 提供Unity编辑器工具窗口，可视化配置表构建
- **构建管道**: 完整的Excel到二进制数据表转换流程

## 系统要求

- Unity 2021.3 或更高版本
- GameFramework 框架
- EPPlus Excel处理库

## 使用方法

1. 在Unity编辑器中打开 `Window > UGF > Data Table Builder`
2. 选择Excel文件和输出目录
3. 配置数据表设置
4. 点击构建按钮生成数据表

## Excel格式规范

- 第一行：字段名称
- 第二行：字段类型 (int, float, string, bool等)
- 第三行：字段描述 (可选)
- 第四行开始：数据内容

## 输出文件

- `{TableName}DataRow.cs`: 数据行类文件
- `{TableName}.bytes`: 二进制数据文件
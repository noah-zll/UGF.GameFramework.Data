using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.GameFramework.Data
{
    /// <summary>
    /// 表关系类型
    /// </summary>
    public enum RelationType
    {
        /// <summary>一对一：源表一条记录对应目标表至多一条</summary>
        OneToOne = 0,
        /// <summary>一对多：源表一条记录对应目标表多条</summary>
        OneToMany = 1,
        /// <summary>多对多：通过中间表（JoinTable）双向关联</summary>
        ManyToMany = 2
    }

    /// <summary>
    /// 关系节点（数据表，编辑器画布布局信息）
    /// </summary>
    [Serializable]
    public class RelationNode
    {
        /// <summary>表名（不含 DR 前缀）</summary>
        public string TableName = string.Empty;

        /// <summary>节点在画布上的位置</summary>
        public Vector2 Position;

        /// <summary>是否展开显示全部字段</summary>
        public bool Expanded = true;
    }

    /// <summary>
    /// 表关系定义（源表 → 目标表）
    /// </summary>
    [Serializable]
    public class TableRelation
    {
        /// <summary>关系名（图内唯一，运行时查询键）</summary>
        public string Name = string.Empty;

        /// <summary>关系类型</summary>
        public RelationType RelationType = RelationType.OneToMany;

        /// <summary>源表名</summary>
        public string SourceTable = string.Empty;

        /// <summary>源表关联字段（默认主键 Id）</summary>
        public string SourceField = string.Empty;

        /// <summary>目标表名</summary>
        public string TargetTable = string.Empty;

        /// <summary>目标表外键字段</summary>
        public string TargetField = string.Empty;

        /// <summary>仅 N:M：中间表名</summary>
        public string JoinTable = string.Empty;

        /// <summary>仅 N:M：中间表中指向源表的字段</summary>
        public string JoinSourceField = string.Empty;

        /// <summary>仅 N:M：中间表中指向目标表的字段</summary>
        public string JoinTargetField = string.Empty;

        /// <summary>关系说明</summary>
        public string Description = string.Empty;
    }

    /// <summary>
    /// 运行时关系配置（导出 JSON 的包装结构）
    /// </summary>
    [Serializable]
    public class TableRelationConfig
    {
        /// <summary>关系列表</summary>
        public List<TableRelation> relations = new List<TableRelation>();

        /// <summary>
        /// 将关系集转换为运行时配置
        /// </summary>
        public static TableRelationConfig FromRelationSet(TableRelationSet set)
        {
            var config = new TableRelationConfig();
            if (set != null)
            {
                config.relations.AddRange(set.Relations);
            }
            return config;
        }

        /// <summary>
        /// 从 JSON 解析运行时配置
        /// </summary>
        public static TableRelationConfig FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new TableRelationConfig();

            try
            {
                return JsonUtility.FromJson<TableRelationConfig>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析表关系配置失败: {ex.Message}");
                return new TableRelationConfig();
            }
        }

        /// <summary>
        /// 序列化为 JSON
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}

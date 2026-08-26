using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.GameFramework.Data
{
    /// <summary>
    /// 表关系集合（编辑器资产，保存节点布局与关系连线）
    /// </summary>
    [CreateAssetMenu(fileName = "TableRelationSet", menuName = "UGF/GameFramework/Data/表关系集")]
    public class TableRelationSet : ScriptableObject
    {
        /// <summary>表节点列表</summary>
        public List<RelationNode> Nodes = new List<RelationNode>();

        /// <summary>关系连线列表</summary>
        public List<TableRelation> Relations = new List<TableRelation>();

        /// <summary>
        /// 查找表节点
        /// </summary>
        public RelationNode FindNode(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return null;

            return Nodes.Find(n => string.Equals(n.TableName, tableName, StringComparison.Ordinal));
        }

        /// <summary>
        /// 查找关系
        /// </summary>
        public TableRelation FindRelation(string relationName)
        {
            if (string.IsNullOrEmpty(relationName))
                return null;

            return Relations.Find(r => string.Equals(r.Name, relationName, StringComparison.Ordinal));
        }

        /// <summary>
        /// 判断关系名是否已存在（可忽略某个关系，用于编辑时重命名检查）
        /// </summary>
        public bool HasRelationName(string relationName, string ignoreRelationName = null)
        {
            if (string.IsNullOrEmpty(relationName))
                return false;

            foreach (var relation in Relations)
            {
                if (!string.Equals(relation.Name, relationName, StringComparison.Ordinal))
                    continue;
                if (!string.IsNullOrEmpty(ignoreRelationName) &&
                    string.Equals(relation.Name, ignoreRelationName, StringComparison.Ordinal))
                    continue;
                return true;
            }
            return false;
        }
    }
}
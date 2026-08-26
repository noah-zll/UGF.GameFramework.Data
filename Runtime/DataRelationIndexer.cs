using GameFramework.DataTable;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UGF.GameFramework.Data
{
    /// <summary>
    /// 表关系索引器（纯 C#，不依赖 Unity/框架运行时）。
    /// 通过行提供者获取表数据，按关系类型构建索引，支持按源键查询关联记录。
    /// </summary>
    public class DataRelationIndexer
    {
        private static readonly Dictionary<string, PropertyInfo> s_PropertyCache = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

        /// <summary>关系定义</summary>
        public readonly TableRelation Relation;

        private readonly Func<string, IDataRow[]> m_RowProvider;

        private bool m_IndexBuilt;
        private IDataRow[] m_TargetRows;
        private IDataRow[] m_JoinRows;
        private Dictionary<int, IDataRow> m_TargetById;

        // 1:1 目标外键值 → 目标行
        private Dictionary<object, IDataRow> m_OneToOneIndex;
        // 1:N 目标外键值 → 目标行列表
        private Dictionary<object, List<IDataRow>> m_OneToManyIndex;
        // N:M 中间表源外键值 → 中间表行列表
        private Dictionary<object, List<IDataRow>> m_JoinIndex;

        /// <summary>
        /// 构造索引器
        /// </summary>
        /// <param name="relation">关系定义</param>
        /// <param name="rowProvider">表名 → 表全部行（表未加载返回 null）</param>
        public DataRelationIndexer(TableRelation relation, Func<string, IDataRow[]> rowProvider)
        {
            Relation = relation ?? throw new ArgumentNullException(nameof(relation));
            m_RowProvider = rowProvider;
        }

        /// <summary>
        /// 索引是否已构建
        /// </summary>
        public bool IndexBuilt
        {
            get { return m_IndexBuilt; }
        }

        /// <summary>
        /// 失效索引（下次 EnsureIndex 重建）
        /// </summary>
        public void InvalidateIndex()
        {
            m_IndexBuilt = false;
            m_TargetRows = null;
            m_JoinRows = null;
            m_TargetById = null;
            m_OneToOneIndex = null;
            m_OneToManyIndex = null;
            m_JoinIndex = null;
        }

        /// <summary>
        /// 构建索引（如已构建则跳过）
        /// </summary>
        public bool EnsureIndex()
        {
            if (m_IndexBuilt)
                return true;

            if (m_RowProvider == null)
                return false;

            m_TargetRows = m_RowProvider(Relation.TargetTable);
            if (m_TargetRows == null)
                return false;

            switch (Relation.RelationType)
            {
                case RelationType.OneToOne:
                case RelationType.OneToMany:
                    BuildDirectIndex();
                    break;
                case RelationType.ManyToMany:
                    if (!BuildJoinIndex())
                        return false;
                    break;
            }

            m_IndexBuilt = true;
            return true;
        }

        /// <summary>
        /// 按源键查询关联记录
        /// </summary>
        public IDataRow[] Query(object sourceKey)
        {
            switch (Relation.RelationType)
            {
                case RelationType.OneToOne:
                    if (m_OneToOneIndex != null && m_OneToOneIndex.TryGetValue(sourceKey, out var one))
                        return new[] { one };
                    return new IDataRow[0];

                case RelationType.OneToMany:
                    if (m_OneToManyIndex != null && m_OneToManyIndex.TryGetValue(sourceKey, out var many))
                        return many.ToArray();
                    return new IDataRow[0];

                case RelationType.ManyToMany:
                    return QueryManyToMany(sourceKey);
            }

            return new IDataRow[0];
        }

        /// <summary>
        /// 构建 1:1 / 1:N 索引（遍历目标表，按外键字段分组）
        /// </summary>
        private void BuildDirectIndex()
        {
            m_OneToOneIndex = new Dictionary<object, IDataRow>();
            m_OneToManyIndex = new Dictionary<object, List<IDataRow>>();

            foreach (var row in m_TargetRows)
            {
                object key = GetFieldValue(row, Relation.TargetField);
                if (key == null)
                    continue;

                if (Relation.RelationType == RelationType.OneToOne)
                {
                    if (!m_OneToOneIndex.ContainsKey(key))
                    {
                        m_OneToOneIndex[key] = row;
                    }
                }
                else
                {
                    if (!m_OneToManyIndex.TryGetValue(key, out var list))
                    {
                        list = new List<IDataRow>();
                        m_OneToManyIndex[key] = list;
                    }
                    list.Add(row);
                }
            }
        }

        /// <summary>
        /// 构建 N:M 索引：中间表源外键值 → 中间表行；目标表主键 → 目标行
        /// </summary>
        private bool BuildJoinIndex()
        {
            if (string.IsNullOrEmpty(Relation.JoinTable))
                return false;

            m_JoinRows = m_RowProvider(Relation.JoinTable);
            if (m_JoinRows == null)
                return false;

            m_JoinIndex = new Dictionary<object, List<IDataRow>>();
            m_TargetById = new Dictionary<int, IDataRow>();
            foreach (var row in m_TargetRows)
            {
                m_TargetById[row.Id] = row;
            }

            foreach (var row in m_JoinRows)
            {
                object key = GetFieldValue(row, Relation.JoinSourceField);
                if (key == null)
                    continue;

                if (!m_JoinIndex.TryGetValue(key, out var list))
                {
                    list = new List<IDataRow>();
                    m_JoinIndex[key] = list;
                }
                list.Add(row);
            }

            return true;
        }

        private IDataRow[] QueryManyToMany(object sourceKey)
        {
            if (m_JoinIndex == null || m_TargetById == null || string.IsNullOrEmpty(Relation.JoinTargetField))
                return new IDataRow[0];

            if (!m_JoinIndex.TryGetValue(sourceKey, out var joinRows))
                return new IDataRow[0];

            var result = new List<IDataRow>(joinRows.Count);
            foreach (var joinRow in joinRows)
            {
                object targetKey = GetFieldValue(joinRow, Relation.JoinTargetField);
                if (targetKey == null)
                    continue;

                int targetId;
                if (targetKey is int intKey)
                {
                    targetId = intKey;
                }
                else if (!int.TryParse(targetKey.ToString(), out targetId))
                {
                    continue;
                }

                if (m_TargetById.TryGetValue(targetId, out var targetRow))
                {
                    result.Add(targetRow);
                }
            }
            return result.ToArray();
        }

        private static object GetFieldValue(IDataRow row, string fieldName)
        {
            if (row == null || string.IsNullOrEmpty(fieldName))
                return null;

            var type = row.GetType();
            string cacheKey = type.FullName + "." + fieldName;
            if (!s_PropertyCache.TryGetValue(cacheKey, out var property))
            {
                property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                s_PropertyCache[cacheKey] = property;
            }

            if (property == null)
                return null;

            try
            {
                return property.GetValue(row);
            }
            catch
            {
                return null;
            }
        }
    }
}

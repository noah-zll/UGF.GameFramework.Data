using GameFramework;
using GameFramework.DataTable;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UGF.GameFramework.Data
{
    public static class DataTableExtension
    {
        private const string DataRowClassPrefixName = "GameData.DR";

        public static void LoadDataTable<T>(this DataTableComponent dataTableComponent, object userData = null)
        {
            Type dataRowType = typeof(T);
            if (dataRowType == null)
            {
                Log.Warning("Data row type is invalid.");
                return;
            }

            string dataTableName = dataRowType.Name.Substring(2);
            string[] splitedNames = dataTableName.Split('_');
            if (splitedNames.Length > 2)
            {
                Log.Warning("Data table name is invalid.");
                return;
            }

            string dataRowClassName = DataRowClassPrefixName + splitedNames[0];
            string name = splitedNames.Length > 1 ? splitedNames[1] : null;
            DataTableBase dataTable = dataTableComponent.CreateDataTable(dataRowType, name);
            DataTableBuilderSettings settings = DataTableBuilderSettings.Instance;
            if (settings == null)
            {
                Log.Warning("DataTableBuilderSettings not found.");
                return;
            }
            string path = Utility.Path.GetRegularPath(System.IO.Path.Combine(settings.DataOutputDirectory, $"{dataTableName}.bytes"));

            Log.Debug("Load data table '{0}' from path '{1}'.", dataTableName, path);
            dataTable.ReadData(path, 0, userData);
        }

        public static void LoadDataTable(this DataTableComponent dataTableComponent, string dataTableName, object userData = null)
        {
            if (string.IsNullOrEmpty(dataTableName))
            {
                Log.Warning("Data table name is invalid.");
                return;
            }

            string[] splitedNames = dataTableName.Split('_');
            if (splitedNames.Length > 2)
            {
                Log.Warning("Data table name is invalid.");
                return;
            }

            string dataRowClassName = DataRowClassPrefixName + splitedNames[0];
            Type dataRowType = Utility.Assembly.GetType(dataRowClassName);
            if (dataRowType == null)
            {
                Log.Warning("Can not get data row type with class name '{0}'.", dataRowClassName);
                return;
            }

            string name = splitedNames.Length > 1 ? splitedNames[1] : null;
            DataTableBase dataTable = dataTableComponent.CreateDataTable(dataRowType, name);
            DataTableBuilderSettings settings = DataTableBuilderSettings.Instance;
            if (settings == null)
            {
                Log.Warning("DataTableBuilderSettings not found.");
                return;
            }
            string path = Utility.Path.GetRegularPath(System.IO.Path.Combine(settings.DataOutputDirectory, $"{dataTableName}.bytes"));

            Log.Debug("Load data table '{0}' from path '{1}'.", dataTableName, path);
            dataTable.ReadData(path, 0, userData);
        }

        // ==================== 表关系查询扩展 ====================

        private const string RelationConfigPath = "DataRelations/relations.json";

        private static readonly Dictionary<string, DataRelationIndexer> s_RelationIndexers = new Dictionary<string, DataRelationIndexer>(StringComparer.Ordinal);
        private static bool s_RelationConfigLoaded;
        private static bool s_RelationEventSubscribed;

        /// <summary>
        /// 是否存在指定关系
        /// </summary>
        public static bool HasRelation(this DataTableComponent dataTableComponent, string relationName)
        {
            EnsureRelationsLoaded(dataTableComponent);
            return s_RelationIndexers.ContainsKey(relationName);
        }

        /// <summary>
        /// 获取所有关系名
        /// </summary>
        public static string[] GetAllRelationNames(this DataTableComponent dataTableComponent)
        {
            EnsureRelationsLoaded(dataTableComponent);

            var names = new string[s_RelationIndexers.Count];
            s_RelationIndexers.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>
        /// 一对一 / 一对多：获取目标表关联记录（多条；一对一最多一条）
        /// </summary>
        public static IDataRow[] GetRelated(this DataTableComponent dataTableComponent, string relationName, object sourceKey)
        {
            EnsureRelationsLoaded(dataTableComponent);

            if (!s_RelationIndexers.TryGetValue(relationName, out var indexer))
            {
                Log.Warning("Data relation '{0}' not found.", relationName);
                return new IDataRow[0];
            }

            if (!indexer.EnsureIndex())
            {
                Log.Warning("Data relation '{0}' index build failed (table not loaded?).", relationName);
                return new IDataRow[0];
            }

            return indexer.Query(sourceKey);
        }

        /// <summary>
        /// 一对一：获取目标表单条关联记录（无则 null）
        /// </summary>
        public static IDataRow GetRelatedOne(this DataTableComponent dataTableComponent, string relationName, object sourceKey)
        {
            var rows = dataTableComponent.GetRelated(relationName, sourceKey);
            return rows.Length > 0 ? rows[0] : null;
        }

        /// <summary>
        /// 获取目标表关联记录的主键数组
        /// </summary>
        public static int[] GetRelatedIds(this DataTableComponent dataTableComponent, string relationName, object sourceKey)
        {
            var rows = dataTableComponent.GetRelated(relationName, sourceKey);
            var ids = new int[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                ids[i] = rows[i].Id;
            }
            return ids;
        }

        /// <summary>
        /// 重新加载关系配置并清空索引
        /// </summary>
        public static void ReloadRelations(this DataTableComponent dataTableComponent)
        {
            s_RelationConfigLoaded = false;
            s_RelationIndexers.Clear();
            EnsureRelationsLoaded(dataTableComponent);
        }

        /// <summary>
        /// 懒加载关系配置并构建索引器（首次查询时）
        /// </summary>
        private static void EnsureRelationsLoaded(DataTableComponent dataTableComponent)
        {
            if (s_RelationConfigLoaded)
                return;

            s_RelationConfigLoaded = true;
            SubscribeRelationEvents();

            string fullPath = Path.Combine(Application.streamingAssetsPath, RelationConfigPath);
            if (!File.Exists(fullPath))
            {
                Log.Warning("Data relation config not found: '{0}'.", fullPath);
                return;
            }

            string json = File.ReadAllText(fullPath, Encoding.UTF8);
            var config = TableRelationConfig.FromJson(json);
            if (config == null || config.relations.Count == 0)
            {
                Log.Info("Data relation config is empty: '{0}'.", fullPath);
                return;
            }

            foreach (var relation in config.relations)
            {
                if (string.IsNullOrEmpty(relation.Name))
                {
                    Log.Warning("Data relation without name is ignored.");
                    continue;
                }

                if (s_RelationIndexers.ContainsKey(relation.Name))
                {
                    Log.Warning("Duplicate data relation name '{0}' is ignored.", relation.Name);
                    continue;
                }

                s_RelationIndexers[relation.Name] = new DataRelationIndexer(relation, tableName => GetDataTableRows(dataTableComponent, tableName));
            }

            Log.Info("Data relation config loaded: {0} relations from '{1}'.", s_RelationIndexers.Count, fullPath);
        }

        /// <summary>
        /// 订阅数据表加载成功事件：失效所有索引（下次查询懒重建）
        /// </summary>
        private static void SubscribeRelationEvents()
        {
            if (s_RelationEventSubscribed)
                return;

            s_RelationEventSubscribed = true;

            var eventComponent = GameEntry.GetComponent<EventComponent>();
            if (eventComponent == null)
                return;

            eventComponent.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
        }

        private static void OnLoadDataTableSuccess(object sender, GameEventArgs e)
        {
            foreach (var indexer in s_RelationIndexers.Values)
            {
                indexer.InvalidateIndex();
            }
        }

        /// <summary>
        /// 行提供者：表名 → 表全部行（表未加载返回 null）。
        /// 通过反射调用泛型 DataTableComponent.GetDataTable&lt;T&gt;() 与 IDataTable&lt;T&gt;.GetAllDataRows()，
        /// 不依赖 DataTableBase 的具体 API。
        /// </summary>
        private static IDataRow[] GetDataTableRows(DataTableComponent dataTableComponent, string tableName)
        {
            if (dataTableComponent == null || string.IsNullOrEmpty(tableName))
                return null;

            var rowType = Type.GetType(DataRowClassPrefixName + tableName);
            if (rowType == null || !dataTableComponent.HasDataTable(rowType))
                return null;

            try
            {
                // 反射调用 DataTableComponent.GetDataTable<T>()
                var getMethod = typeof(DataTableComponent).GetMethod("GetDataTable", Type.EmptyTypes);
                if (getMethod == null)
                    return null;

                getMethod = getMethod.MakeGenericMethod(rowType);
                var dataTable = getMethod.Invoke(dataTableComponent, null);
                if (dataTable == null)
                    return null;

                // 反射调用 IDataTable<T>.GetAllDataRows()，返回 T[]（T : IDataRow）
                var interfaceType = typeof(IDataTable<>).MakeGenericType(rowType);
                var getAllMethod = interfaceType.GetMethod("GetAllDataRows");
                if (getAllMethod == null)
                    return null;

                var rows = getAllMethod.Invoke(dataTable, null) as System.Collections.IEnumerable;
                if (rows == null)
                    return null;

                var result = new List<IDataRow>();
                foreach (var row in rows)
                {
                    if (row is IDataRow dataRow)
                        result.Add(dataRow);
                }
                return result.ToArray();
            }
            catch (Exception ex)
            {
                Log.Warning("Get table rows '{0}' failed: {1}", tableName, ex.Message);
                return null;
            }
        }
    }
}

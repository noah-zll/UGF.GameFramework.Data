using GameFramework;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.DataTable;

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
    }
}

using System;
using System.IO;
using System.Text;
using GameFramework;
using GameFramework.DataTable;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UGF.GameFramework.Data
{
    public class UGFDataTableHelper : DataTableHelperBase
    {
        private static readonly string BytesAssetExtension = ".bytes";

        private ResourceComponent m_ResourceComponent;

        public override bool ReadData(DataTableBase dataTable, string dataTableAssetName, object dataTableAsset, object userData)
        {
            TextAsset dataTableTextAsset = dataTableAsset as TextAsset;
            if (dataTableTextAsset == null)
            {
                Log.Warning("Data table asset '{0}' is invalid.", dataTableAssetName);
                return false;
            }

            if (dataTableAssetName.EndsWith(BytesAssetExtension, StringComparison.Ordinal))
            {
                return ParseData(dataTable, dataTableTextAsset.bytes, 0, dataTableTextAsset.bytes.Length, userData);
            }

            return dataTable.ParseData(dataTableTextAsset.text, userData);
        }

        public override bool ReadData(DataTableBase dataTable, string dataTableAssetName, byte[] dataTableBytes, int startIndex, int length, object userData)
        {
            if (dataTableAssetName.EndsWith(BytesAssetExtension, StringComparison.Ordinal))
            {
                return ParseData(dataTable, dataTableBytes, startIndex, length, userData);
            }

            return dataTable.ParseData(Utility.Converter.GetString(dataTableBytes, startIndex, length), userData);
        }

        public override bool ParseData(DataTableBase dataTable, string dataTableString, object userData)
        {
            return dataTable.ParseData(dataTableString, userData);
        }

        public override bool ParseData(DataTableBase dataTable, byte[] dataTableBytes, int startIndex, int length, object userData)
        {
            if (dataTableBytes == null || length <= 0)
            {
                return false;
            }

            if (length < 4)
            {
                return dataTable.ParseData(dataTableBytes, startIndex, length, userData);
            }

            int magic = BitConverter.ToInt32(dataTableBytes, startIndex);
            if (magic != 0x44544247)
            {
                return dataTable.ParseData(dataTableBytes, startIndex, length, userData);
            }

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(dataTableBytes, startIndex, length, false))
                {
                    using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                    {
                        binaryReader.ReadInt32();
                        byte version = binaryReader.ReadByte();
                        if (version != 1)
                        {
                            Log.Warning("Unsupported data table version '{0}'.", version);
                            return false;
                        }

                        string tableName = binaryReader.ReadString();
                        int fieldCount = binaryReader.ReadInt32();
                        if (fieldCount <= 0)
                        {
                            Log.Warning("Invalid field count '{0}'.", fieldCount);
                            return false;
                        }

                        string[] fieldTypes = new string[fieldCount];
                        for (int i = 0; i < fieldCount; i++)
                        {
                            string fieldName = binaryReader.ReadString();
                            string fieldType = binaryReader.ReadString();
                            string description = binaryReader.ReadString();
                            bool isPrimaryKey = binaryReader.ReadBoolean();
                            fieldTypes[i] = fieldType;
                        }

                        int recordCount = binaryReader.ReadInt32();
                        if (recordCount < 0)
                        {
                            Log.Warning("Invalid record count '{0}'.", recordCount);
                            return false;
                        }

                        for (int i = 0; i < recordCount; i++)
                        {
                            long rowStartPosition = memoryStream.Position;
                            for (int j = 0; j < fieldTypes.Length; j++)
                            {
                                ReadFieldValue(binaryReader, fieldTypes[j]);
                            }

                            long rowEndPosition = memoryStream.Position;
                            int rowLength = (int)(rowEndPosition - rowStartPosition);
                            int rowOffset = startIndex + (int)rowStartPosition;

                            if (!dataTable.AddDataRow(dataTableBytes, rowOffset, rowLength, userData))
                            {
                                Log.Warning("Can not parse data row bytes.");
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("Can not parse data table bytes with exception '{0}'.", exception);
                return false;
            }
        }

        public override void ReleaseDataAsset(DataTableBase dataTable, object dataTableAsset)
        {
            m_ResourceComponent.UnloadAsset(dataTableAsset);
        }

        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
            if (m_ResourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
            }
        }

        private static void ReadFieldValue(BinaryReader reader, string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                reader.ReadString();
                return;
            }

            if (IsEnumType(type))
            {
                reader.ReadInt32();
                return;
            }

            string lowerType = type.ToLower();
            if (lowerType == "int")
            {
                reader.ReadInt32();
            }
            else if (lowerType == "float")
            {
                reader.ReadSingle();
            }
            else if (lowerType == "string")
            {
                reader.ReadString();
            }
            else if (lowerType == "bool")
            {
                reader.ReadBoolean();
            }
            else if (lowerType == "long")
            {
                reader.ReadInt64();
            }
            else if (lowerType == "double")
            {
                reader.ReadDouble();
            }
            else if (lowerType == "byte")
            {
                reader.ReadByte();
            }
            else if (lowerType == "short")
            {
                reader.ReadInt16();
            }
            else
            {
                reader.ReadString();
            }
        }

        private static bool IsEnumType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return false;
            }

            if (type.StartsWith("enum:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string lowerType = type.ToLower();
            if (lowerType == "int" || lowerType == "float" || lowerType == "string" || lowerType == "bool" || lowerType == "long" || lowerType == "double" || lowerType == "byte" || lowerType == "short")
            {
                return false;
            }

            if (type.IndexOf('[') >= 0 || type.IndexOf(']') >= 0)
            {
                return false;
            }

            return true;
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public static class MetaDataExtensions
    {
        public static string GetCategory(this string str)
        {
           return str.Split('\\').First();
        }

        public static string GetName(this string str)
        {
            return str.Split('\\').Last();
        }

        public static string ToValidDbString(this string str)
        {
            return str.Replace('\\','_');
        }

        public static List<string> GetKeyFieldsName(this DataSourceInfo ds)
        {
            return ds.Fields.Where(w=>w.Value.IsKey).Select(s=>s.Key).ToList();
        }

        public static DataSourceInfo ResolveDataSourceParents(this DataSourceInfo dataSourceInfo, DataSourceInfo[] dataSources)
        {
            if (string.IsNullOrEmpty(dataSourceInfo.ParentProviderString)) return dataSourceInfo;
            var dataSource = (DataSourceInfo)dataSourceInfo.Clone();

            var parentFields = GetParentFields(dataSource,
                dataSources.ToDictionary(ds => ds.DataSourcePath, ds => ds));

            foreach (var parentField in parentFields)
            {
                //parentField.DataSourceProviderString = dataSource.DataSourcePath;  //PARENT FIEDLS MUST HAVE IT`S PARENT DATASOURCE PROVIDER STRING
                parentField.IsParentField = true;
                dataSource.Fields.Add(parentField.Name, parentField);
            }
            return dataSource;
        }

        private static List<FieldInfo> GetParentFields(DataSourceInfo dataSourceInfo, Dictionary<string, DataSourceInfo> dataSources)
        {
            var parentFields = new List<FieldInfo>();
            if (!string.IsNullOrEmpty(dataSourceInfo.ParentProviderString))
            {

                parentFields.AddRange(
                    dataSources[dataSourceInfo.ParentProviderString].Fields.Values.Select(f =>
                    {
                        var childField = (FieldInfo)f.Clone();
                        childField.IsParentField = true;
                        return childField;
                    }));

                parentFields.AddRange(GetParentFields(dataSources[dataSourceInfo.ParentProviderString], dataSources));
            }


            return parentFields;
        }

        public static DataSourceInfo[] GetChildDataSources(this DataSourceInfo dataSourceInfo, DataSourceInfo[] dataSources)
        {
            var childDataSources = new Dictionary<string, DataSourceInfo>();
            ResolveChildDataSources(ref childDataSources, dataSourceInfo, dataSources);
            return childDataSources.Values.ToArray();
        }
        public static DataSourceInfo[] GetChildDataSources(this string dataSourcePath, DataSourceInfo[] dataSources)
        {
            var childDataSources = new Dictionary<string, DataSourceInfo>();
            ResolveChildDataSources(ref childDataSources, dataSources.First(x=>x.DataSourcePath == dataSourcePath), dataSources);
            return childDataSources.Where(x=>x.Key != dataSourcePath).Select(x=>x.Value).ToArray();
        }

        private static void ResolveChildDataSources(ref Dictionary<string, DataSourceInfo> list, HeaderInfo dataSource, DataSourceInfo[] dataSources)
        {
            if (dataSources.All(x => x.ParentProviderString != dataSource.DataSourcePath)) return;
            foreach (var dataSourceInfo in dataSources.Where(x => x.ParentProviderString == dataSource.DataSourcePath))
            {
                if (list.ContainsKey(dataSourceInfo.DataSourcePath)) continue;
                list.Add(dataSourceInfo.DataSourcePath, dataSourceInfo);
                ResolveChildDataSources(ref list, dataSourceInfo, dataSources);
            }
        }

        public static WorksheetInfo MakeWorksheetInfo(this AggregatedWorksheetInfo aggregatedWorksheet)
        {
            return new WorksheetInfo
            {
                Id = aggregatedWorksheet.Id,
                Name = aggregatedWorksheet.Name,
                Category = aggregatedWorksheet.Category,
                Columns = aggregatedWorksheet.GetColumnsFromAgrWs(),
                DataSourceInfo = aggregatedWorksheet.MakeAggregatedDataSourceInfo()
            };
        }

        public static List<ColumnInfo> GetColumnsFromAgrWs(this AggregatedWorksheetInfo aggregatedWorksheet)
        {
            var columns = new List<ColumnInfo>();
            columns.AddRange(aggregatedWorksheet.GroupByColumns);
            columns.ForEach(x=> { x.SortIndex = columns.IndexOf(x); x.SortOrder = ColumnSortOrder.Ascending; } ); // TODO POSSIBLY WE DONT NEED THIS
            columns.AddRange(aggregatedWorksheet.Columns.Select(x => x.Value));
            columns.ForEach(x=>x.ColumnIndex = columns.IndexOf(x));
            return columns;
        }

        public static DataSourceInfo MakeAggregatedDataSourceInfo(this AggregatedWorksheetInfo aggregatedWorksheet)
        {
            return new DataSourceInfo
            {
                Category = aggregatedWorksheet.DataSourceInfo.Category,
                Description = aggregatedWorksheet.DataSourceInfo.Description,
                Id = aggregatedWorksheet.DataSourceInfo.Id,
                Name = aggregatedWorksheet.DataSourceInfo.Name,
                Fields = aggregatedWorksheet.GetColumnsFromAgrWs().ToDictionary(x=>x.Header, x=>new FieldInfo
                {
                    DataSourceProviderString = aggregatedWorksheet.DataSourceInfo.ParentProviderString,
                    IsKey = x.FieldName == x.Header,
                    IsNullable = x.FieldName!=x.Header,
                    DataType = aggregatedWorksheet.DataSourceInfo.Fields[x.FieldName].DataType,
                    Name = x.Header,
                    IsParentField = false,
                    IsReadOnly = true
                })
            };
        }
    }
}

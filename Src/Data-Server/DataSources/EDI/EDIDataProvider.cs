using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Server.EDI.Feeds
{
    public class EDIDataProvider:IDataProvider
    {
        private static string[] exFields = new[] { "paytype", "rdid", "priority", "defaultopt", "outturnsecid", "outturnisin", "ratioold", "rationew", "fractions", "currency", "rate1type", "rate1", "rate2type", "rate2" }; //hardcode
        private readonly DataSourceInfo _dataSourceInfo;
        private readonly string _symbol;
        private readonly string _dataPath;
        private readonly string _filter;


        public EDIDataProvider(DataSourceInfo dataSource,string symbol,string dataPath,string filter)
        {
            _dataSourceInfo = dataSource;
            _symbol = symbol;
            _dataPath = dataPath; 
            _filter = filter;
        }

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            var data = GetFeedData();
            return data;
        }

        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, IEnumerable<string> recordsToDelete, string comment = null)
        {
            return new RevisionInfo(){IsSuccessfull = true};
        }


        private IEnumerable<Dictionary<string, object>> GetFeedData()
        {
            var data = new List<Dictionary<string, object>>();
            if (Directory.Exists(_dataPath))
            {
                string[] files = Directory.GetFiles(_dataPath);
                foreach (string s in files)
                {
                    var fileName = Path.GetFileName(s);
                    if (!fileName.Contains(_filter.Replace("*", ""))) continue;
                    var rows = FileDataParser.ParseTxtToRows("\t", _dataPath + fileName);
                    var keyFieldName = _dataSourceInfo.GetKeyFieldsName().First();
                    var preparedData = PrepareData(rows, keyFieldName);
                    if (!string.IsNullOrEmpty(_dataSourceInfo.ParentDataSourcePath))
                    {
                        var records = ParseInheritData(preparedData, _dataSourceInfo, _symbol);
                        var finalrecords = ParseDataSourceData(_dataSourceInfo, records);
                        data.AddRange(finalrecords);
                        continue;
                    }
                    var finalData = ParseDataSourceData(_dataSourceInfo, preparedData);
                    data.AddRange(finalData);
                }
            }
            return data;
        }

        public  static IEnumerable<Dictionary<string, object>> ParseInheritData(IEnumerable<Dictionary<string, object>> data, DataSourceInfo dsInfo, string eventName)
        {
            foreach (var row in data.Where(w => w["eventcd"].ToString() == eventName))
            {
                var dict = new Dictionary<string, object>();
                try
                {
                    foreach (var keyValuePair in row)
                    {
                        if (exFields.Any(a => a == keyValuePair.Key)) continue; // except fields
                        if (keyValuePair.Key.Contains("type"))
                        {
                            var fieldName = keyValuePair.Key.Replace("type", string.Empty);
                            if (keyValuePair.Value.ToString() == string.Empty) continue;
                            dict.Add(dsInfo.Fields.First(f => String.Equals(f.Key, keyValuePair.Value.ToString(), StringComparison.CurrentCultureIgnoreCase)).Key, dsInfo.Fields.Keys.Contains(keyValuePair.Value.ToString()) ? FileDataParser.TryConvert(row[fieldName].ToString(), dsInfo.Fields[keyValuePair.Value.ToString()].DataType) : string.Empty);
                            continue;
                        }
                        if (keyValuePair.Key.Contains("field") && keyValuePair.Key.Contains("name"))
                        {
                            var fieldName = keyValuePair.Key.Replace("name", string.Empty);
                            if (keyValuePair.Value.ToString() == string.Empty) continue;
                            dict.Add(dsInfo.Fields.First(f => String.Equals(f.Key, keyValuePair.Value.ToString(), StringComparison.CurrentCultureIgnoreCase)).Key, dsInfo.Fields.Keys.Contains(keyValuePair.Value.ToString()) ? FileDataParser.TryConvert(row[fieldName].ToString(), dsInfo.Fields[keyValuePair.Value.ToString()].DataType) : string.Empty);
                            continue;
                        }
                        if (keyValuePair.Key.Contains("field") || keyValuePair.Key.Contains("date")) continue;
                        dict.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                catch (Exception)
                {
                    
                    yield break;
                }
               
                yield return dict;
            }
        }

        private IEnumerable<Dictionary<string, object>> ParseDataSourceData(DataSourceInfo dsInfo,IEnumerable<Dictionary<string, object>> data, string[] fields = null)
        {
            var dsFields = fields ?? dsInfo.Fields.Keys.ToArray();
            foreach (var row in data)
            {
                var dict = new Dictionary<string, object>();
                row.Join(dsFields, j1 => j1.Key.ToLower(), j2 => j2.ToLower(), (j1, j2) =>
                {
                    dict.Add(j2, FileDataParser.TryConvert(row[j2].ToString(), dsInfo.Fields[j2].DataType));
                    return j1;
                }).Count();
                yield return dict;
            }
        }

        public static IEnumerable<Dictionary<string, object>> PrepareData(IEnumerable<Dictionary<string, object>> data, string keyFieldName)
        {
            return data.GroupBy(gr => gr[keyFieldName].ToString()).Select(gr => gr.Last());
        }

        public void SendInheritData(string path)
        {
            if (RecordChangedEvent == null) return;
            var keyFieldName = _dataSourceInfo.GetKeyFieldsName().First();
            var rows = FileDataParser.ParseTxtToRows("\t", path);
            var preparedData = PrepareData(rows, keyFieldName);
            var realData = ParseInheritData(preparedData, _dataSourceInfo, _symbol).Select(s => new ValueChangedEventArgs()
            {
                DataSourceUrn = _dataSourceInfo.DataSourcePath,
                ChangedPropertyNames = _dataSourceInfo.Fields.Keys.ToArray(),
                Value = s
            });
            foreach (var valueChangedEventArgse in realData)
            {
                 RecordChangedEvent(this, valueChangedEventArgse);
            }
        }

        public void SendData(string path)
        {
            if (RecordChangedEvent == null) return;
            var keyFieldName = _dataSourceInfo.GetKeyFieldsName().First();
            var rows = FileDataParser.ParseTxtToRows("\t", path);
            var preparedData = PrepareData(rows, keyFieldName);
            var realData = ParseDataSourceData(_dataSourceInfo, preparedData).Select(s => new ValueChangedEventArgs()
            {
                DataSourceUrn = _dataSourceInfo.DataSourcePath,
                ChangedPropertyNames = _dataSourceInfo.Fields.Keys.ToArray(),
                Value = s
            });
            foreach (var valueChangedEventArgse in realData)
            {
                    RecordChangedEvent(this, valueChangedEventArgse);
            }
        }


    }
}

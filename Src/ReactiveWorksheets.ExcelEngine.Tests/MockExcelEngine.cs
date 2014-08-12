using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.ExcelAddIn.ReactiveExcel;
using ReactiveWorksheets.Server.Tests;

namespace ReactiveWorksheets.ExcelEngine.Tests
{
    class MockExcelEngine : IReactiveExcelEngine
    {
        public readonly ReactiveExcelMessanger ExcelMessanger;

        public MockExcelEngine(IObservable<RecordChangedParam[]> serverObservable)
        {
            ExcelMessanger = new ReactiveExcelMessanger();
            Subcribers = new Dictionary<string, IDisposable>();
            _serverObs = serverObservable;
            DataSourceInfos = new List<DataSourceInfo>();
            LocalDb = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, object>>> LocalDb { get; set; }


        public void SubmitData(string urn, Dictionary<string, object>[] data)
        {
            if (!LocalDb.Any()) return;
            var ds = DataSourceInfos.FirstOrDefault(f=>f.DataSourcePath == urn);
            if (ds == null) return;
         
                var keyFieldName = ds.GetKeyFieldsName().First();
                var resolvedData = new List<Dictionary<string, object>>();
                foreach (var record in data)
                {
                    var keyValue = "|" + record[keyFieldName];
                    var row = new Dictionary<string, Dictionary<string, object>>() { { keyValue, new Dictionary<string, object>(LocalDb[urn][keyValue]) } };
                    LocalDb[urn][keyValue].Join(record, j1 => j1.Key.ToLower(), j2 => j2.Key.ToLower(), (j1, j2) =>
                    {
                        row[keyValue][j1.Key] = j2.Value;
                        return j1;
                    }).Count();
                    resolvedData.Add(row[keyValue]);
                    LocalDb[urn][keyValue] = row[keyValue];
                }
            SubmitedData = resolvedData.ToArray();

        }

        public IObservable<object> RegisterSubject(string dataSourceUrn, string primaryKey, string fieldName)
        {
            return ExcelMessanger.RegisterSubject(dataSourceUrn, "|" + primaryKey, fieldName, OnSubscribed);
        }

        public void RegisterSource(ExcelPoint point)
        {
            if (!LocalDb.ContainsKey(point.DataSourceUrn)) ExcelMessanger.SendData(point, "Invalid DataSourcePath"); 
            var sub = _serverObs.Subscribe(s =>
            {
                UpdateDb(s);
                foreach (var recordChangedParam in s.Where(w => w.ChangedAction == RecordChangedAction.AddedOrUpdated))
                    foreach (var changedPropertyName in recordChangedParam.ChangedPropertyNames)
                        ExcelMessanger.SendData(recordChangedParam.ProviderString, recordChangedParam.RecordKey,
                            changedPropertyName, recordChangedParam.RecordValues[changedPropertyName]);
            });
            Subcribers.Add(point.DataSourceUrn,sub);
        }

        private void UpdateDb(IEnumerable<RecordChangedParam> recordChangedParams)
        {
            foreach (var recordChangedParam in recordChangedParams)
            {
                if (!LocalDb.ContainsKey(recordChangedParam.ProviderString)) continue;
                if (!LocalDb[recordChangedParam.ProviderString].ContainsKey(recordChangedParam.RecordKey))
                {
                    LocalDb[recordChangedParam.ProviderString].Add(recordChangedParam.RecordKey, (Dictionary<string, object>)recordChangedParam.RecordValues);
                    continue;
                }
                if (recordChangedParam.ChangedAction == RecordChangedAction.Removed)
                {
                    LocalDb[recordChangedParam.ProviderString].Remove(recordChangedParam.RecordKey);
                    continue;
                }
                LocalDb[recordChangedParam.ProviderString][recordChangedParam.RecordKey] =
                    (Dictionary<string, object>)recordChangedParam.RecordValues;
            }
        }

        public ServerInfo GetServerInfo()
        {
            throw new NotImplementedException();
        }

        private void PullData(ExcelPoint excelPoint)
        {
            if (!LocalDb.ContainsKey(excelPoint.DataSourceUrn)) return;
            if (!LocalDb[excelPoint.DataSourceUrn].ContainsKey(excelPoint.PrimaryKeyValue)) return;
            ExcelMessanger.SendData(excelPoint.DataSourceUrn, excelPoint.PrimaryKeyValue, excelPoint.FieldName, LocalDb[excelPoint.DataSourceUrn][excelPoint.PrimaryKeyValue][excelPoint.FieldName]);
        }

        private void OnSubscribed(ExcelPoint excelPoint)
        {
            if(IsRdpSubcribed) return;
            PullData(excelPoint);
            RegisterSource(excelPoint);
            IsRdpSubcribed = true;
        }

        #region for test
        private IObservable<RecordChangedParam[]> _serverObs; 

        public List<DataSourceInfo> DataSourceInfos { get; set; }
        public Dictionary<string,IDisposable> Subcribers { get; set; }
        public bool IsRdpSubcribed { get; set; }
        public Dictionary<string,object>[] SubmitedData { get; set; }

        public void AddDsForRegister(string dsjson,string tsvdata)
        {
            var ds = MockRepository.GetDataSourceFromJSON(dsjson);
            DataSourceInfos.Add(ds);
            var data = RecordHelpers.TsvToDictionary(ds, tsvdata);
            var dbData = new Dictionary<string, Dictionary<string, object>>();
            foreach (var record in data)
            {
                var key = record.WorkOutRecordKey(ds.GetKeyFieldsName());
                dbData.Add(key, record);
            }
            LocalDb.Add(ds.DataSourcePath, dbData);
        }

        #endregion for test
    }
}

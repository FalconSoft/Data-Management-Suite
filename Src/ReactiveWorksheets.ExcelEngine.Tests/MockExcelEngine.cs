using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExcelDna.Integration;
using ExcelDna.Integration.Rtd;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.ExcelAddIn.ReactiveExcel;
using ReactiveWorksheets.Server.Tests;

namespace ReactiveWorksheets.ExcelEngine.Tests
{

    class MockExcelEngine : IReactiveExcelEngine
    {
        public readonly ReactiveExcelMessanger ExcelMessanger;
        public readonly MockUpdateEvent MockUpdateEvent;

        public MockExcelEngine(IObservable<RecordChangedParam[]> serverObservable, AutoResetEvent autoResetEvent)
        {
            MockUpdateEvent = new MockUpdateEvent(autoResetEvent);
            ExcelMessanger = new ReactiveExcelMessanger(MockUpdateEvent);
            Subcribers = new Dictionary<string, IDisposable>();
            _serverObs = serverObservable;
            DataSourceInfos = new List<DataSourceInfo>();
            LocalDb = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, object>>> LocalDb { get; set; }

        public Array GetRtdData()
        {
            return ExcelMessanger.RtdData ;
        }

        public void RegisterSubject(int topicId, string dataSourceUrn, string primaryKey, string fieldName)
        {
            ExcelMessanger.RegisterSubject(topicId, dataSourceUrn, primaryKey.ToRecordKey(), fieldName);
            RegisterSource(dataSourceUrn);
            var point = new ExcelPoint(dataSourceUrn, primaryKey.ToRecordKey(), fieldName);
            if (!LocalDb.ContainsKey(point.DataSourceUrn))
                ExcelMessanger.SendData(new[] {point}, "Invalid DataSourcePath");
            PullData(point);
        }

        public void RegisterSource(string dataSourceUrn)
        {
            if(Subcribers.ContainsKey(dataSourceUrn)) return;
            var sub = _serverObs.Subscribe(s =>
            {
                UpdateDb(s);
                foreach (var recordChangedParam in s.Where(w => w.ChangedAction == RecordChangedAction.AddedOrUpdated))
                    foreach (var changedPropertyName in recordChangedParam.ChangedPropertyNames)
                        ExcelMessanger.SendData(recordChangedParam.ProviderString, recordChangedParam.RecordKey,
                            changedPropertyName, recordChangedParam.RecordValues[changedPropertyName]);
            });
            Subcribers.Add(dataSourceUrn, sub);
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

        private void PullData(ExcelPoint excelPoint)
        {
            if (!LocalDb.ContainsKey(excelPoint.DataSourceUrn)) return;
            if (!LocalDb[excelPoint.DataSourceUrn].ContainsKey(excelPoint.PrimaryKeyValue)) return;
            ExcelMessanger.SendData(excelPoint.DataSourceUrn, excelPoint.PrimaryKeyValue, excelPoint.FieldName, LocalDb[excelPoint.DataSourceUrn][excelPoint.PrimaryKeyValue][excelPoint.FieldName]);
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

    public class MockUpdateEvent :IRTDUpdateEvent
    {
        private readonly AutoResetEvent _autoResetEvent;
        public MockUpdateEvent(AutoResetEvent autoResetEvent)
        {
            _autoResetEvent = autoResetEvent;
        }

        public void UpdateNotify()
        {
            _autoResetEvent.Set();
        }

        public void Disconnect()
        {
            
        }

        public int HeartbeatInterval { get; set; }
    }

}

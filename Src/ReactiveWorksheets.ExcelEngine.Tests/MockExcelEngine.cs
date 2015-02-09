using System;
using System.Collections.Generic;
using System.Threading;
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

        public MockExcelEngine(IObservable<RecordChangedParam[]> serverObservable)
        {
            ExcelMessanger = new ReactiveExcelMessanger();
            Subcribers = new Dictionary<string, IDisposable>();
            _serverObs = serverObservable;
            DataSourceInfos = new List<DataSourceInfo>();
            LocalDb = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, object>>> LocalDb { get; set; }

        public object ResultValue { get; set; }

        public void RegisterSubject(ExcelRtdServer.Topic topic, string dataSourceUrn, string primaryKey, string fieldName)
        {
            var point = new ExcelPoint(dataSourceUrn, primaryKey.ToRecordKey(), fieldName);
            RegisterSource(dataSourceUrn);
            ResultValue = !LocalDb.ContainsKey(point.DataSourceUrn) ? "Invalid Urn" : LocalDb[dataSourceUrn][primaryKey.ToRecordKey()][fieldName];
        }


        public void RegisterSource(string dataSourceUrn)
        {
            if(Subcribers.ContainsKey(dataSourceUrn)) return;
            var sub = _serverObs.Subscribe(UpdateDb);
            Subcribers.Add(dataSourceUrn, sub);
        }

        private void UpdateDb(IEnumerable<RecordChangedParam> recordChangedParams)
        {
            foreach (var recordChangedParam in recordChangedParams)
            {
                if (!LocalDb.ContainsKey(recordChangedParam.ProviderString)) continue;
                if (!LocalDb[recordChangedParam.ProviderString].ContainsKey(recordChangedParam.RecordKey))
                {
                    LocalDb[recordChangedParam.ProviderString].Add(recordChangedParam.RecordKey, recordChangedParam.RecordValues);
                    continue;
                }
                if (recordChangedParam.ChangedAction == RecordChangedAction.Removed)
                {
                    LocalDb[recordChangedParam.ProviderString].Remove(recordChangedParam.RecordKey);
                    continue;
                }
                LocalDb[recordChangedParam.ProviderString][recordChangedParam.RecordKey] =
                    recordChangedParam.RecordValues;
            }
        }



        #region for test
        private readonly IObservable<RecordChangedParam[]> _serverObs; 

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
            LocalDb.Add(ds.Urn, dbData);
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

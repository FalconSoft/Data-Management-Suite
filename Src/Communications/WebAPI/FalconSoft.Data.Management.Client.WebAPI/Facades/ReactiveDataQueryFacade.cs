using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Text;
using System.Web.Script.Serialization;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class ReactiveDataQueryFacade :WebApiClientBase, IReactiveDataQueryFacade
    {
        private HttpClient _client;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ReactiveDataQueryFacade(string url)
            : base(url, "ReactiveDataQueryApi")
        {
            
        }
        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
            FilterRule[] filterRules = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetData<T>(string userToken, string dataSourcePath, FilterRule[] filterRules = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetData(string userToken, string dataSourcePath, string[] fields = null, FilterRule[] filterRules = null)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>>("GetData",
                new Dictionary<string, object>
                {
                    {"userToken", userToken},
                    {"dataSourcePath", dataSourcePath},
                    {"fields", fields},
                    {"filterRules", filterRules}
                });
        }

        public IEnumerable<string> GetFieldData(string userToken, string dataSourcePath, string field, string match, int elementsToReturn = 10)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetDataByKey(string userToken, string dataSourcePath, string[] recordKeys, string[] fields = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(string userToken, string dataSourcePath, string[] fields = null)
        {
            return new Subject<RecordChangedParam[]>();
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn, string userToken,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            throw new NotImplementedException();
        }

        public bool CheckExistence(string userToken, string dataSourceUrn, string fieldName, object value)
        {
            throw new NotImplementedException();
        }
    }
}
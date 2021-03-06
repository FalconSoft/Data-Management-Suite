﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Newtonsoft.Json;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class ReactiveDataQueryFacade : WebApiClientBase, IReactiveDataQueryFacade
    {

        private const string HubName = "ReactiveDataHub";
        private readonly SignalRHub _signalRHub;
        private readonly Subject<RecordChangedParam[]> _dataChangesObservable = new Subject<RecordChangedParam[]>();


        public ReactiveDataQueryFacade(string url, string pushUrl, ILogger log)
            : base(url, "ReactiveDataQueryApi", log)
        {
            _signalRHub = new SignalRHub(pushUrl, HubName, log, hubProxy => hubProxy.On<string, string>("UpdatesAreReady", OnUpdatesAreReady));
        }

        private void OnUpdatesAreReady(string pushKey, string dataSources)
        {

            var msg = GetWebApiCall<string>("GetPushMessage",
                new Dictionary<string, object>
                {
                    { "userToken", "" },
                    { "pushKey", pushKey }
                });

            var recordChangedParams = JsonConvert.DeserializeObject<RecordChangedParam[]>(msg);
            _dataChangesObservable.OnNext(recordChangedParams);
        }


        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
            FilterRule[] filterRules = null)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>, AggregatedWorksheetInfo>("GetAggregatedData", aggregatedWorksheet,
                new Dictionary<string, object>
                {
                    {"userToken", userToken},
                    {"dataSourcePath", dataSourcePath},
                    {"filterRules", filterRules}
                });
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
            return GetStreamDataToEnumerable<string>("GetFieldData",
                new Dictionary<string, object>
                {
                    {"userToken", userToken},
                    {"dataSourcePath", dataSourcePath},
                    {"field", field},
                    {"match", match},
                    {"elementsToReturn", elementsToReturn}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetDataByKey(string userToken, string dataSourcePath, string[] recordKeys, string[] fields = null)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>, string[]>("GetDataByKey", recordKeys,
                new Dictionary<string, object>
                {
                    {"userToken", userToken},
                    {"dataSourcePath", dataSourcePath},
                    {"fields", fields}
                });
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(string userToken, string dataSourcePath, string[] fields = null)
        {
            return _dataChangesObservable.Where(r=> r != null && r.Length > 0 && r[0].ProviderString == dataSourcePath);
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn, string userToken,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            PostWebApiCallMessage("ResolveRecordbyForeignKey", changedRecord, new Dictionary<string, object>
            {
                {"dataSourceUrn", dataSourceUrn},
                {"userToken", userToken}
            }).ContinueWith(completeTask =>
            {
                var responce = completeTask.Result;

                if (responce.StatusCode == HttpStatusCode.OK)
                {
                    if (onSuccess != null)
                    {
                        responce.Content
                            .ReadAsAsync<RecordChangedParam[]>()
                            .ContinueWith(t => onSuccess(responce.ReasonPhrase, t.Result));
                    }
                }
                else if (responce.StatusCode == HttpStatusCode.InternalServerError)
                {
                    if (onFail != null)
                    {
                        responce.Content
                            .ReadAsAsync<Exception>()
                            .ContinueWith(t => onFail(responce.ReasonPhrase, t.Result));
                    }
                }
                else responce.EnsureSuccessStatusCode();
            });
            //ResolveRecordbyForeignKeyGet(changedRecord, dataSourceUrn, userToken, onSuccess, onFail);
        }

        public bool CheckExistence(string userToken, string dataSourceUrn, string fieldName, object value)
        {
            return GetWebApiCall<bool>("CheckExistence",
               new Dictionary<string, object>
                {
                    {"userToken", userToken}, 
                    {"dataSourceUrn", dataSourceUrn},
                    {"fieldName",fieldName},
                    {"value", value}
                });
        }

        public void Dispose()
        {

        }
    }
}
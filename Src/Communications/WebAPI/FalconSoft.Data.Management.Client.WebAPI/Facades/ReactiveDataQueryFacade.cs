﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class ReactiveDataQueryFacade : WebApiClientBase, IReactiveDataQueryFacade
    {
       
        private const string GetDataChangesTopic = "GetDataChangesTopic";

        public ReactiveDataQueryFacade(string url, IRabbitMQClient rabbitMQClient)
            : base(url, "ReactiveDataQueryApi", rabbitMQClient)
        {
           
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
            GetWebApiAsyncCall("GetDataChanges", new Dictionary<string, object>
            {
                {"userToken", userToken},
                {"dataSourcePath", dataSourcePath},
                {"fields", fields}
            }).Wait();

            var routingKey = fields != null ? string.Format("{0}.{1}.", dataSourcePath, userToken) + fields.Aggregate("", (cur, next) => string.Format("{0}.{1}", cur, next)).GetHashCode()
               : string.Format("{0}.{1}", dataSourcePath, userToken);


            var observable =  CreateExchngeObservable<RecordChangedParam[]>(
                GetDataChangesTopic, "topic", routingKey);

            return Observable.Create<RecordChangedParam[]>(subj =>
            {
                var disposable = observable.Subscribe(subj);

                var keepAlive = new EventHandler<ServerReconnectionArgs>((obj, evArgs) =>
                {
                    disposable.Dispose();

                    observable = CreateExchngeObservable<RecordChangedParam[]>(
                    GetDataChangesTopic, "topic", routingKey);

                    disposable = observable.Subscribe(subj);

                    GetWebApiAsyncCall("GetDataChanges", new Dictionary<string, object>
                    {
                        {"userToken", userToken},
                        {"dataSourcePath", dataSourcePath},
                        {"fields", fields}
                    });
                });

                ServerReconnectedEvent += keepAlive;

                return Disposable.Create(() =>
                {
                    ServerReconnectedEvent -= keepAlive;
                    disposable.Dispose();

                });
            });
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
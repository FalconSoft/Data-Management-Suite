using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Newtonsoft.Json;

namespace FalconSoft.ReactiveWorksheets.Client.SignalR
{
    internal class MetaDataFacade : IMetaDataAdminFacade
    {
        private readonly HubConnection _connection;
        private readonly IHubProxy _proxy;
        private readonly Task _startConnectionTask;

        public MetaDataFacade(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("IMetaDataAdminFacade");

            _connection.Reconnecting += OnReconnecting;
            _connection.Reconnected += OnReconnected;
            _connection.Closed += OnClosed;

            _proxy.On<SourceObjectChangedEventArgs>("ObjectInfoChanged", evArgs =>
            {
                evArgs.SourceObjectInfo = JsonDeserializer(evArgs.SourceObjectInfo.ToString(), evArgs.ChangedObjectType);
                if (ObjectInfoChanged != null)
                    ObjectInfoChanged(this, evArgs);
            });

            _startConnectionTask = _connection.Start();
        }

        private object JsonDeserializer(string jsonObject, ChangedObjectType jsonObjectType)
        {
            switch (jsonObjectType)
            {
                case ChangedObjectType.WorksheetInfo:
                    return JsonConvert.DeserializeObject<WorksheetInfo>(jsonObject);
                case ChangedObjectType.DataSourceInfo:
                    return JsonConvert.DeserializeObject<DataSourceInfo>(jsonObject);
                case ChangedObjectType.AggregatedWorksheetInfo:
                    return JsonConvert.DeserializeObject<AggregatedWorksheetInfo>(jsonObject);
                default:
                    return JsonConvert.DeserializeObject<WorksheetInfo>(jsonObject);
            }
        }

        private void OnClosed() { }

        private void OnReconnected()
        {
            Trace.WriteLine("*******   IMetaDataAdminFacade reconected");
        }

        private void OnReconnecting()
        {
            Trace.WriteLine("******   IMetaDataAdminFacade reconecting");
        }

        public DataSourceInfo[] GetAvailableDataSources(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetAvailableDataSourcesServerCall(userId, minAccessLevel);
            }
            _startConnectionTask.Wait();
            return GetAvailableDataSourcesServerCall(userId, minAccessLevel);
        }

        private DataSourceInfo[] GetAvailableDataSourcesServerCall(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            var tcs = new TaskCompletionSource<DataSourceInfo[]>();
            var task = tcs.Task;
            _proxy.Invoke<DataSourceInfo[]>("GetAvailableDataSources", userId, minAccessLevel)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    var data = t.Result;
                    tcs.SetResult(data);
                });


            return task.Result;
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetDataSourceInfoServerCall(dataSourceUrn);
            }
            _startConnectionTask.Wait();
            return GetDataSourceInfoServerCall(dataSourceUrn);
        }

        private DataSourceInfo GetDataSourceInfoServerCall(string dataSourceUrn)
        {
            var tcs = new TaskCompletionSource<DataSourceInfo>();
            var task = tcs.Task;
            _proxy.Invoke<DataSourceInfo>("GetDataSourceInfo", dataSourceUrn)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    tcs.SetResult(t.Result);

                });
            return task.Result;
        }

        public DataSourceInfo[] GetDependentDataSources(string dataSourceUrn)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetDependentDataSourcesServerCall(dataSourceUrn);
            }
            _startConnectionTask.Wait();
            return GetDependentDataSourcesServerCall(dataSourceUrn);
        }

        private DataSourceInfo[] GetDependentDataSourcesServerCall(string dataSourceUrn)
        {
            var tcs = new TaskCompletionSource<DataSourceInfo[]>();
            var task = tcs.Task;
            _proxy.Invoke<DataSourceInfo[]>("GetDependentDataSources", dataSourceUrn)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    tcs.SetResult(t.Result);

                });
            return task.Result;
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("UpdateDataSourceInfo", dataSource, oldDataSourceUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("UpdateDataSourceInfo", dataSource, oldDataSourceUrn, userId));
            }
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("CreateDataSourceInfo", dataSource, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                    _proxy.Invoke("CreateDataSourceInfo", dataSource, userId));
            }
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("DeleteDataSourceInfo", dataSourceUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("DeleteDataSourceInfo", dataSourceUrn, userId));
            }
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetWorksheetInfoServerCall(worksheetUrn);
            }
            _startConnectionTask.Wait();
            return GetWorksheetInfoServerCall(worksheetUrn);
        }
        
        private WorksheetInfo GetWorksheetInfoServerCall(string worksheetUrn)
        {
            var tcs = new TaskCompletionSource<WorksheetInfo>();
            var task = tcs.Task;
            _proxy.Invoke<WorksheetInfo>("GetWorksheetInfo", worksheetUrn)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    t.Result.Columns.ForEach(c=>c.Update(t.Result.DataSourceInfo.Fields[c.FieldName]));
                    tcs.SetResult(t.Result);
                });
            return task.Result;
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetAvailableWorksheetsServerCall(userId, minAccessLevel);
            }
            _startConnectionTask.Wait();
            return GetAvailableWorksheetsServerCall(userId, minAccessLevel);
        }

        private WorksheetInfo[] GetAvailableWorksheetsServerCall(string userId, AccessLevel minAccessLevel)
        {
            var tcs = new TaskCompletionSource<WorksheetInfo[]>();
            var task = tcs.Task;
            _proxy.Invoke<WorksheetInfo[]>("GetAvailableWorksheets", userId, minAccessLevel)
                       .ContinueWith(t =>
                       {
                           if (t.IsFaulted)
                           {
                               tcs.SetCanceled();
                               return;
                           }
                           foreach (var ws in t.Result)
                           {
                               ws.Columns.ForEach(c=>c.Update(ws.DataSourceInfo.Fields[c.FieldName]));
                           }
                           tcs.SetResult(t.Result);
                       });
            return task.Result;
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("UpdateWorksheetInfo", wsInfo, oldWorksheetUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("UpdateWorksheetInfo", wsInfo, oldWorksheetUrn, userId));
            }
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("CreateWorksheetInfo", wsInfo, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("CreateWorksheetInfo", wsInfo, userId));
            }
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("DeleteWorksheetInfo", worksheetUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("DeleteWorksheetInfo", worksheetUrn, userId));
            }
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetAvailableAggregatedWorksheetsServerCall(userId, minAccessLevel);
            }
            _startConnectionTask.Wait();
            return GetAvailableAggregatedWorksheetsServerCall(userId, minAccessLevel);
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheetsServerCall(string userId, AccessLevel minAccessLevel)
        {
            var tcs = new TaskCompletionSource<AggregatedWorksheetInfo[]>();
            var task = tcs.Task;
            _proxy.Invoke<AggregatedWorksheetInfo[]>("GetAvailableAggregatedWorksheets", userId, minAccessLevel)
                       .ContinueWith(t =>
                       {
                           if (t.IsFaulted)
                           {
                               tcs.SetCanceled();
                               return;
                           }
                           foreach (var aws in t.Result)
                           {
                               aws.Columns.ForEach(c => c.Value.Update(aws.DataSourceInfo.Fields[c.Value.FieldName]));
                               aws.GroupByColumns.ForEach(c => c.Update(aws.DataSourceInfo.Fields[c.FieldName]));
                           }
                           tcs.SetResult(t.Result);
                       });
            return task.Result;
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("UpdateAggregatedWorksheetInfo", wsInfo, oldWorksheetUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("UpdateAggregatedWorksheetInfo", wsInfo, oldWorksheetUrn, userId));
            }
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("CreateAggregatedWorksheetInfo", wsInfo, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("CreateAggregatedWorksheetInfo", wsInfo, userId));
            }
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("DeleteAggregatedWorksheetInfo", worksheetUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("DeleteAggregatedWorksheetInfo", worksheetUrn, userId));
            }
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetAggregatedWorksheetInfoServerCall(worksheetUrn);
            }
            _startConnectionTask.Wait();
            return GetAggregatedWorksheetInfoServerCall(worksheetUrn);
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfoServerCall(string worksheetUrn)
        {
            var tcs = new TaskCompletionSource<AggregatedWorksheetInfo>();
            var task = tcs.Task;
            _proxy.Invoke<AggregatedWorksheetInfo>("GetAggregatedWorksheetInfo", worksheetUrn)
                       .ContinueWith(t =>
                       {
                           if (t.IsFaulted)
                           {
                               tcs.SetCanceled();
                               return;
                           }
                           t.Result.Columns.ForEach(c=>c.Value.Update(t.Result.DataSourceInfo.Fields[c.Value.FieldName]));
                           t.Result.GroupByColumns.ForEach(c=>c.Update(t.Result.DataSourceInfo.Fields[c.FieldName]));
                           tcs.SetResult(t.Result);
                       });
            return task.Result;
        }

        public ServiceSourceInfo[] GetAvailableServiceSources(string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {

                return GetAvailableServiceSourcesServerCall(userId);
            }
            _startConnectionTask.Wait(TimeSpan.FromSeconds(3));
            return GetAvailableServiceSourcesServerCall(userId);
        }

        private ServiceSourceInfo[] GetAvailableServiceSourcesServerCall(string userId)
        {
            var tcs = new TaskCompletionSource<ServiceSourceInfo[]>();
            var task = tcs.Task;
            _proxy.Invoke<ServiceSourceInfo[]>("GetAvailableServiceSources", userId)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    tcs.SetResult(t.Result);
                });

            return task.Result;
        }

        public ServiceSourceInfo GetServiceSourceInfo(string serviceSourceUrn)
        {
            if (_startConnectionTask.IsCompleted)
            {
                return GetServiceSourceInfoServerCall(serviceSourceUrn);
            }
            _startConnectionTask.Wait();
            return GetServiceSourceInfoServerCall(serviceSourceUrn);
        }

        private ServiceSourceInfo GetServiceSourceInfoServerCall(string serviceSourceUrn)
        {
            var tcs = new TaskCompletionSource<ServiceSourceInfo>();
            var task = tcs.Task;
            _proxy.Invoke<ServiceSourceInfo>("GetServiceSourceInfo", serviceSourceUrn)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetCanceled();
                        return;
                    }
                    tcs.SetResult(t.Result);
                });
            return task.Result;
        }

        public void CreateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("CreateServiceSourceInfo", serviceSourceInfo, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("CreateServiceSourceInfo", serviceSourceInfo, userId));
            }
        }

        public void UpdateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string oldUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("UpdateServiceSourceInfo", serviceSourceInfo, oldUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("UpdateServiceSourceInfo", serviceSourceInfo, oldUrn, userId));
            }
        }

        public void DeleteServiceSourceInfo(string serviceSourceUrn, string userId)
        {
            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("DeleteServiceSourceInfo", serviceSourceUrn, userId);
            }
            else
            {
                _startConnectionTask.ContinueWith(t =>
                _proxy.Invoke("DeleteServiceSourceInfo", serviceSourceUrn, userId));
            }
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public void Dispose()
        {
            _connection.Stop();
        }
    }
}
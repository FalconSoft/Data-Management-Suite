using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Server.Common.Facade;
using FalconSoft.Data.Server.Common.Metadata;
using FalconSoft.Data.Server.Common.Security;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Newtonsoft.Json;

namespace FalconSoft.ReactiveWorksheets.Client.SignalR
{
    internal class MetaDataFacade : IMetaDataAdminFacade
    {
        private readonly string _connectionString;
        private HubConnection _connection;
        private IHubProxy _proxy;
        private Task _startConnectionTask;
        private Action _onCompleteAction;
        private Timer _keepAliveTimer;

        public MetaDataFacade(string connectionString)
        {
            _connectionString = connectionString;
            InitialiseConnection(connectionString);

            _keepAliveTimer = new Timer(OnTick,null,5000,3000);
        }

        private void OnTick(object state)
        {
            CheckConnectionToServer();
        }

        private void InitialiseConnection(string connectionString)
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

            _proxy.On("OnComplete", () =>
            {
                if (_onCompleteAction != null)
                    _onCompleteAction();
            });

            _startConnectionTask = _connection.Start();
        }

        private void CheckConnectionToServer()
        {

            if (_connection.State == ConnectionState.Disconnected)
            {
                InitialiseConnection(_connectionString);
            }
            if (!_startConnectionTask.IsCompleted)
                _startConnectionTask.Wait();
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
            CheckConnectionToServer();
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
            CheckConnectionToServer();
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
            CheckConnectionToServer();
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
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };
            CheckConnectionToServer();
            _proxy.Invoke("UpdateDataSourceInfo", dataSource, oldDataSourceUrn, userId);
            task.Wait();
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
            _proxy.Invoke("CreateDataSourceInfo", dataSource, userId);
           
            task.Wait();
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
            _proxy.Invoke("DeleteDataSourceInfo", dataSourceUrn, userId);

            task.Wait();
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn)
        {
            CheckConnectionToServer();
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
                    tcs.SetResult(t.Result);
                });
            return task.Result;
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            CheckConnectionToServer();
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
                           tcs.SetResult(t.Result);
                       });
            return task.Result;
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
            _proxy.Invoke("UpdateWorksheetInfo", wsInfo, oldWorksheetUrn, userId);
           
            task.Wait();
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
            _proxy.Invoke("CreateWorksheetInfo", wsInfo, userId);
           
            task.Wait();
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
            _proxy.Invoke("DeleteWorksheetInfo", worksheetUrn, userId);
           
            task.Wait();
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            CheckConnectionToServer();
            return GetAvailableAggregatedWorksheetsServerCall(userId, minAccessLevel);
        }

        private AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheetsServerCall(string userId, AccessLevel minAccessLevel)
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
                           tcs.SetResult(t.Result);
                       });
            return task.Result;
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
            _proxy.Invoke("UpdateAggregatedWorksheetInfo", wsInfo, oldWorksheetUrn, userId);
            
            task.Wait();
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
            _proxy.Invoke("CreateAggregatedWorksheetInfo", wsInfo, userId);
            
            task.Wait();
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => { if (!task.IsCompleted) tcs.SetResult(new object()); };

            CheckConnectionToServer();
                _proxy.Invoke("DeleteAggregatedWorksheetInfo", worksheetUrn, userId);
            
            task.Wait();
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn)
        {
            CheckConnectionToServer();
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
                           tcs.SetResult(t.Result);
                       });
            return task.Result;
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;

        public void Dispose()
        {
            _keepAliveTimer.Dispose();
            _connection.Stop();
        }
    }
}
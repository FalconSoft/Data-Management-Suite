using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.ReactiveWorksheets.Client.SignalR
{
    internal class TemporalDataQueryFacade : ITemporalDataQueryFacade
    {
        private readonly string _connectionString;
        private HubConnection _connection;
        private IHubProxy _proxy;
        private Task _startConnectionTask;

        //for GetRecordsHistory method
        private Action<Dictionary<string, object>> _getRecordsHistoryOnNextAction;
        private Action _getRecordsHistoryOnCompleteAction;
        private Action<Exception> _getRecordsHistoryOnErrorAction;
        // ************************************************

        //for GetDataHistoryByTag method
        private Action<Dictionary<string, object>> _getDataHistoryByTagOnNextAction;
        private Action _getDataHistoryByTagOnCompleteAction;
        private Action<Exception> _getDataHistoryByTagOnErrorAction; 
        // ************************************************

        //for GetRecordsAsOf method
        private Action<Dictionary<string, object>> _getRecordsAsOfOnNextAction;
        private Action _getRecordsAsOfOnCompleteAction;
        private Action<Exception> _getRecordsAsOfOnErrorAction;
        // ************************************************

        //for GeTagInfos method
        private Action<TagInfo> _geTagInfosOnNextAction;
        private Action _geTagInfosOnCompleteAction;
        private Action<Exception> _geTagInfosOnErrorAction;
        
        private Action _onCompleteAction;
        private Action<Exception> _onFailedAction;

        public TemporalDataQueryFacade(string connectionString)
        {
            _connectionString = connectionString;
            InitialiseConnection(connectionString);
        }

        private void InitialiseConnection(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("ITemporalDataQueryFacade");

            //  for GetRecordsHistory method
            _proxy.On<Dictionary<string, object>>("GetRecordsHistoryOnNext", data =>
            {
                if (_getRecordsHistoryOnNextAction != null)
                    _getRecordsHistoryOnNextAction(data);
            });

            _proxy.On("GetRecordsHistoryOnComplete", () =>
            {
                if (_getRecordsHistoryOnCompleteAction != null)
                    _getRecordsHistoryOnCompleteAction();
            });

            _proxy.On("GetRecordsHistoryOnError", ex =>
            {
                if (_getRecordsHistoryOnErrorAction != null)
                    _getRecordsHistoryOnErrorAction(ex);
            });

            //*********************************************

            //  for GetDataHistoryByTag method
            _proxy.On<Dictionary<string, object>>("GetDataHistoryByTagOnNext", data =>
            {
                if (_getDataHistoryByTagOnNextAction != null)
                    _getDataHistoryByTagOnNextAction(data);
            });

            _proxy.On("GetDataHistoryByTagOnComplete", () =>
            {
                if (_getDataHistoryByTagOnCompleteAction != null)
                    _getDataHistoryByTagOnCompleteAction();
            });

            _proxy.On("GetDataHistoryByTagOnError", ex =>
            {
                if (_getDataHistoryByTagOnErrorAction != null)
                    _getDataHistoryByTagOnErrorAction(ex);
            });

            //*********************************************

            //  for GetRecordsAsOf method
            _proxy.On<Dictionary<string, object>>("GetRecordsAsOfOnNext", data =>
            {
                if (_getRecordsAsOfOnNextAction != null)
                    _getRecordsAsOfOnNextAction(data);
            });

            _proxy.On("GetRecordsAsOfOnComplete", () =>
            {
                if (_getRecordsAsOfOnCompleteAction != null)
                    _getRecordsAsOfOnCompleteAction();
            });

            _proxy.On("GetRecordsAsOfOnError", ex =>
            {
                if (_getRecordsAsOfOnErrorAction != null)
                    _getRecordsAsOfOnErrorAction(ex);
            });

            //*********************************************

            //  for GeTagInfos method
            _proxy.On<TagInfo>("GeTagInfosOnNext", data =>
            {
                if (_geTagInfosOnNextAction != null)
                    _geTagInfosOnNextAction(data);
            });

            _proxy.On("GeTagInfosOnComplete", () =>
            {
                if (_geTagInfosOnCompleteAction != null)
                    _geTagInfosOnCompleteAction();
            });

            _proxy.On("GeTagInfosOnError", ex =>
            {
                if (_geTagInfosOnErrorAction != null)
                    _geTagInfosOnErrorAction(ex);
            });

            //*********************************************

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

        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            CheckConnectionToServer();

            var subject = new Subject<Dictionary<string, object>>();
            _getRecordsHistoryOnNextAction = data => subject.OnNext(data);
            _getRecordsHistoryOnCompleteAction = () => subject.OnCompleted();
            _getRecordsHistoryOnErrorAction = ex => subject.OnError(ex);

            _proxy.Invoke("GetRecordsHistory", dataSourceInfo, recordKey);

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            CheckConnectionToServer();

            var subject = new Subject<Dictionary<string, object>>();
            _getDataHistoryByTagOnNextAction = data => subject.OnNext(data);
            _getDataHistoryByTagOnCompleteAction = () => subject.OnCompleted();
            _getDataHistoryByTagOnErrorAction = ex => subject.OnError(ex);

            _proxy.Invoke("GetDataHistoryByTag", dataSourceInfo, tagInfo);

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            CheckConnectionToServer();

            var subject = new Subject<Dictionary<string, object>>();
            _getRecordsAsOfOnNextAction = data => subject.OnNext(data);
            _getRecordsAsOfOnCompleteAction = () => subject.OnCompleted();
            _getRecordsAsOfOnErrorAction = ex => subject.OnError(ex);

            _proxy.Invoke("GetRecordsAsOf", dataSourceInfo, timeStamp);

            return subject.ToEnumerable();
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            CheckConnectionToServer();

            var subject = new Subject<TagInfo>();
            _geTagInfosOnNextAction = data => subject.OnNext(data);
            _geTagInfosOnCompleteAction = () => subject.OnCompleted();
            _geTagInfosOnErrorAction = ex => subject.OnError(ex);

            _proxy.Invoke("GeTagInfos");

            return subject.ToEnumerable();
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => tcs.SetResult(new object());

            CheckConnectionToServer();
            _proxy.Invoke("SaveTagInfo", tagInfo);
           
            task.Wait();
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _onCompleteAction = () => tcs.SetResult(new object());

            CheckConnectionToServer();
            _proxy.Invoke("RemoveTagInfo", tagInfo);
         
            task.Wait();
        }

        public void Dispose()
        {
           _connection.Stop();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FalconSoft.Data.Server.Common.Facade;
using FalconSoft.Data.Server.Common.Metadata;
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
        private int _getRecordsHistoryCount;
        private int _getRecordsHistoryCounter;
        private readonly object _getRecordsHistoryLock = new object();

        //for GetDataHistoryByTag method
        private Action<Dictionary<string, object>> _getDataHistoryByTagOnNextAction;
        private Action _getDataHistoryByTagOnCompleteAction;
        private Action<Exception> _getDataHistoryByTagOnErrorAction;
        private int _getDataHistoryByTagCount;
        private int _getDataHistoryByTagCounter;
        private readonly object _getDataHistoryByTagLock = new object();

        //for GetRecordsAsOf method
        private Action<Dictionary<string, object>> _getRecordsAsOfOnNextAction;
        private Action _getRecordsAsOfOnCompleteAction;
        private Action<Exception> _getRecordsAsOfOnErrorAction;
        private int _getRecordsAsOfCount;
        private int _getRecordsAsOfCounter;
        private readonly object _getRecordsAsOfLock = new object();

        //for GeTagInfos method
        private Action<TagInfo> _geTagInfosOnNextAction;
        private Action _geTagInfosOnCompleteAction;
        private Action<Exception> _geTagInfosOnErrorAction;
        private int _geTagInfosCount;
        private int _geTagInfosCounter;
        private readonly object _geTagInfosLock = new object();

        private Action _onCompleteAction;

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
            _proxy.On<Dictionary<string, object>[]>("GetRecordsHistoryOnNext", data =>
            {
                lock (_getRecordsHistoryLock)
                {
                    if (_getRecordsHistoryOnNextAction != null)
                        foreach (var dictionary in data)
                        {
                            ++_getRecordsHistoryCounter;
                            _getRecordsHistoryOnNextAction(dictionary);
                        }
                    if (_getRecordsHistoryCount != 0 &&
                        _getRecordsHistoryCounter != 0 &&
                        _getRecordsHistoryCount == _getRecordsHistoryCounter &&
                        _getRecordsHistoryOnCompleteAction != null)
                        _getRecordsHistoryOnCompleteAction();
                }
            });

            _proxy.On<int>("GetRecordsHistoryOnComplete", count =>
            {
                _getRecordsHistoryCount = count;

                if (_getRecordsHistoryCount != 0 &&
                    _getRecordsHistoryCounter != 0 &&
                    _getRecordsHistoryCount == _getRecordsHistoryCounter
                    && _getRecordsHistoryOnCompleteAction != null)
                    _getRecordsHistoryOnCompleteAction();

                if (count == 0 &&
                    _getRecordsHistoryOnCompleteAction != null)
                    _getRecordsHistoryOnCompleteAction();
            });

            _proxy.On("GetRecordsHistoryOnError", ex =>
            {
                if (_getRecordsHistoryOnErrorAction != null)
                    _getRecordsHistoryOnErrorAction(ex);
            });

            //  for GetDataHistoryByTag method
            _proxy.On<Dictionary<string, object>[]>("GetDataHistoryByTagOnNext", data =>
            {
                lock (_getDataHistoryByTagLock)
                {
                    if (_getDataHistoryByTagOnNextAction != null)
                        foreach (var dictionary in data)
                        {
                            ++_getDataHistoryByTagCounter;
                            _getDataHistoryByTagOnNextAction(dictionary);
                        }

                    if (_getDataHistoryByTagCount != 0 &&
                        _getDataHistoryByTagCounter != 0 &&
                        _getDataHistoryByTagCount == _getDataHistoryByTagCounter &&
                        _getDataHistoryByTagOnCompleteAction != null)
                        _getDataHistoryByTagOnCompleteAction();
                }
            });

            _proxy.On<int>("GetDataHistoryByTagOnComplete", count =>
            {
                _getDataHistoryByTagCount = count;
                if (_getDataHistoryByTagCount != 0 &&
                    _getDataHistoryByTagCounter != 0 &&
                    _getDataHistoryByTagCount == _getDataHistoryByTagCounter &&
                    _getDataHistoryByTagOnCompleteAction != null)
                    _getDataHistoryByTagOnCompleteAction();
                if (count == 0 &&
                    _getDataHistoryByTagOnCompleteAction != null)
                    _getDataHistoryByTagOnCompleteAction();
            });

            _proxy.On("GetDataHistoryByTagOnError", ex =>
            {
                if (_getDataHistoryByTagOnErrorAction != null)
                    _getDataHistoryByTagOnErrorAction(ex);
            });

            //  for GetRecordsAsOf method
            _proxy.On<Dictionary<string, object>[]>("GetRecordsAsOfOnNext", data =>
            {
                lock (_getRecordsAsOfLock)
                {
                    if (_getRecordsAsOfOnNextAction != null)
                        foreach (var dictionary in data)
                        {
                            ++_getRecordsAsOfCounter;
                            _getRecordsAsOfOnNextAction(dictionary);
                        }

                    if (_getRecordsAsOfCount != 0 &&
                        _getRecordsAsOfCounter != 0 &&
                        _getRecordsAsOfCounter == _getRecordsAsOfCount &&
                        _getRecordsAsOfOnCompleteAction != null)
                        _getRecordsAsOfOnCompleteAction();
                }
            });

            _proxy.On<int>("GetRecordsAsOfOnComplete", count =>
            {
                _getRecordsAsOfCount = count;
                if (_getRecordsAsOfCount != 0 &&
                    _getRecordsAsOfCounter != 0 &&
                    _getRecordsAsOfCounter == _getRecordsAsOfCount &&
                    _getRecordsAsOfOnCompleteAction != null)
                    _getRecordsAsOfOnCompleteAction();
                if (count == 0 &&
                    _getRecordsAsOfOnCompleteAction != null)
                    _getRecordsAsOfOnCompleteAction();
            });

            _proxy.On("GetRecordsAsOfOnError", ex =>
            {
                if (_getRecordsAsOfOnErrorAction != null)
                    _getRecordsAsOfOnErrorAction(ex);
            });
            
            //  for GeTagInfos method
            _proxy.On<TagInfo[]>("GeTagInfosOnNext", data =>
            {
                lock (_geTagInfosLock)
                {
                    if (_geTagInfosOnNextAction != null)
                        foreach (var tagInfo in data)
                        {
                            ++_geTagInfosCounter;
                            _geTagInfosOnNextAction(tagInfo);
                        }

                    if (_geTagInfosCount != 0 &&
                        _geTagInfosCounter != 0 &&
                        _geTagInfosCount == _geTagInfosCounter &&
                        _geTagInfosOnCompleteAction != null)
                        _geTagInfosOnCompleteAction();
                }
            });

            _proxy.On<int>("GeTagInfosOnComplete", count =>
            {
                _geTagInfosCount = count;
                if (_geTagInfosCount != 0 &&
                    _geTagInfosCounter != 0 &&
                    _geTagInfosCount == _geTagInfosCounter &&
                    _geTagInfosOnCompleteAction != null)
                    _geTagInfosOnCompleteAction();

                if (count != 0 &&
                    _geTagInfosOnCompleteAction != null)
                    _geTagInfosOnCompleteAction();
            });

            _proxy.On("GeTagInfosOnError", ex =>
            {
                if (_geTagInfosOnErrorAction != null)
                    _geTagInfosOnErrorAction(ex);
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

        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            var subject = new Subject<Dictionary<string, object>>();

            _getRecordsHistoryCount = 0;
            _getRecordsHistoryCounter = 0;

            _getRecordsHistoryOnNextAction = data => subject.OnNext(data);
            _getRecordsHistoryOnCompleteAction = () => subject.OnCompleted();
            _getRecordsHistoryOnErrorAction = ex => subject.OnError(ex);

            CheckConnectionToServer();

            _proxy.Invoke("GetRecordsHistory", dataSourceInfo, recordKey);

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            var subject = new Subject<Dictionary<string, object>>();

            _getDataHistoryByTagCount = 0;
            _getDataHistoryByTagCounter = 0;

            _getDataHistoryByTagOnNextAction = data => subject.OnNext(data);
            _getDataHistoryByTagOnCompleteAction = () => subject.OnCompleted();
            _getDataHistoryByTagOnErrorAction = ex => subject.OnError(ex);

            CheckConnectionToServer();

            _proxy.Invoke("GetDataHistoryByTag", dataSourceInfo, tagInfo);

            return subject.ToEnumerable();
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            var subject = new Subject<Dictionary<string, object>>();

            _getRecordsAsOfCount = 0;
            _getRecordsAsOfCounter = 0;

            _getRecordsAsOfOnNextAction = data => subject.OnNext(data);
            _getRecordsAsOfOnCompleteAction = () => subject.OnCompleted();
            _getRecordsAsOfOnErrorAction = ex => subject.OnError(ex);

            CheckConnectionToServer();

            _proxy.Invoke("GetRecordsAsOf", dataSourceInfo, timeStamp);

            return subject.ToEnumerable();
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            var subject = new Subject<TagInfo>();

            _geTagInfosCount = 0;
            _geTagInfosCounter = 0;

            _geTagInfosOnNextAction = data => subject.OnNext(data);
            _geTagInfosOnCompleteAction = () => subject.OnCompleted();
            _geTagInfosOnErrorAction = ex => subject.OnError(ex);

            CheckConnectionToServer();

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
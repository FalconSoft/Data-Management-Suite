using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace FalconSoft.ReactiveWorksheets.Client.SignalR
{
    internal class ReactiveDataQueryFacade : IReactiveDataQueryFacade
    {
        private readonly string _connectionString;

        private HubConnection _connection;
        private IHubProxy _proxy;
        private Task _startConnectionTask;
        private bool _allowToRestoreConnection;
        private readonly object _connectionLock = new object();
        private readonly object _keepAliveTimerLock = new object();
        private static object _initialiseLock;
        // For  method GetAggregatedData
        private Action<Dictionary<string, object>> _getAggregatedDataOnNextAction;
        private Action _getAggregatedDataOnCompletetAction;
        private Action<Exception> _getAggregatedDataOnErrorAction;

        // For etGenericData method
        private Action<object> _getGenericDataOnNextAction;
        private Action _getGenericDataOnCompleteAction;
        private Action<Exception> _getGenericDataOnErrorAction;

        // For GetData method
        private Action<Dictionary<string, object>> _getDataOnNextAction;
        private Action _getDataOnCompleteAction;
        private Action<Exception> _getDataOnErrorAction;

        // For GetDataChanges method
        private readonly Subject<RecordChangedParam[]> _getDataChangesSubject;

        // For ResolveRecordbyForeignKey method
        private Action<string, RecordChangedParam[]> _resolveRecordbyForeignKeySuccessAction;
        private Action<string, Exception> _resolveRecordbyForeignKeyFailedAction;

        public ReactiveDataQueryFacade(string connectionString)
        {
            if (_initialiseLock == null)
                _initialiseLock = new object();

            _getDataChangesSubject = new Subject<RecordChangedParam[]>();
            _connectionString = connectionString;
            InitialiseConnection(connectionString);
        }

        private void InitialiseConnection(string connectionString)
        {
           // lock (_initialiseLock)
            {
                _connection = new HubConnection(connectionString);
                _proxy = _connection.CreateHubProxy("IReactiveDataQueryFacade");

                // For  method GetAggregatedData
                _proxy.On<Dictionary<string, object>>("GetAggregatedDataOnNext", data =>
                {
                    if (_getAggregatedDataOnNextAction != null)
                        _getAggregatedDataOnNextAction(data);
                });

                _proxy.On("GetAggregatedDataOnComplete", () =>
                {
                    if (_getAggregatedDataOnCompletetAction != null)
                        _getAggregatedDataOnCompletetAction();

                });

                _proxy.On<Exception>("GetAggregatedDataOnError", ex =>
                {
                    if (_getAggregatedDataOnErrorAction != null)
                        _getAggregatedDataOnErrorAction(ex);
                });

                // For GetGenericData method
                _proxy.On<object>("GetGenericDataOnNext", data =>
                {
                    if (_getGenericDataOnNextAction != null)
                        _getGenericDataOnNextAction(data);

                });

                _proxy.On("GetGenericDataOnComplete", () =>
                {
                    if (_getGenericDataOnCompleteAction != null)
                        _getGenericDataOnCompleteAction();
                });

                _proxy.On<Exception>("GetGenericDataOnError", ex =>
                {
                    if (_getGenericDataOnErrorAction != null)
                        _getGenericDataOnErrorAction(ex);
                });

                // For GetData method
                _proxy.On<Dictionary<string, object>>("GetDataOnNext", data =>
                {
                    if (_getDataOnNextAction != null)
                        _getDataOnNextAction(data);

                });

                _proxy.On("GetDataOnComplete", () =>
                {
                    if (_getDataOnCompleteAction != null)
                        _getDataOnCompleteAction();
                });

                _proxy.On<Exception>("GetDataOnError", ex =>
                {
                    if (_getDataOnErrorAction != null)
                        _getDataOnErrorAction(ex);
                });

                // For GetDataChanges method
                _proxy.On<RecordChangedParam[]>("GetDataChangesOnNext", data =>
                {
                    _getDataChangesSubject.OnNext(data);
                });

                _proxy.On("GetDataChangesOnComplete", () =>
                {
                    _getDataChangesSubject.OnCompleted();
                });

                _proxy.On<Exception>("GetDataChangesOnError", ex =>
                {
                    _getDataChangesSubject.OnError(ex);
                });

                // For ResolveRecordbyForeignKey method
                _proxy.On<string, RecordChangedParam[]>("ResolveRecordbyForeignKeySuccess", (message, data) =>
                {
                    _resolveRecordbyForeignKeySuccessAction(message, data);

                });

                _proxy.On<string, Exception>("ResolveRecordbyForeignKeyFailed", (message, ex) =>
                {
                    _resolveRecordbyForeignKeyFailedAction(message, ex);
                });

                _startConnectionTask = _connection.Start();
            }
        }

        public void Dispose()
        {
            _connection.Stop();
        }

        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string dataSourcePath,
            AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null)
        {
            var subject = new Subject<Dictionary<string, object>>();

            _getAggregatedDataOnNextAction = subject.OnNext;
            _getAggregatedDataOnCompletetAction = subject.OnCompleted;
            _getAggregatedDataOnErrorAction = subject.OnError;

            CheckConnectionToServer();

            _proxy.Invoke("GetAggregatedData", dataSourcePath, aggregatedWorksheet, filterRules ?? new FilterRule[0]);
            return subject.ToEnumerable();
        }

        public IEnumerable<T> GetData<T>(string dataSourcePath, FilterRule[] filterRules = null)
        {
            var subject = new Subject<object>();

            _getGenericDataOnNextAction = data => subject.OnNext(data);
            _getGenericDataOnCompleteAction = () => subject.OnCompleted();
            _getGenericDataOnErrorAction = ex => subject.OnError(ex);

            CheckConnectionToServer();

            _proxy.Invoke("GetGenericData", dataSourcePath, typeof (T), filterRules ?? new FilterRule[0]);
            return subject.ToEnumerable() as IEnumerable<T>;
        }

        public IEnumerable<Dictionary<string, object>> GetData(string dataSourcePath, FilterRule[] filterRules = null)
        {
            var subject = new Subject<Dictionary<string, object>>();

            _getDataOnNextAction = data => subject.OnNext(data);
            _getDataOnCompleteAction = () => subject.OnCompleted();
            _getDataOnErrorAction = ex => subject.OnError(ex);

            CheckConnectionToServer();

            _proxy.Invoke("GetData", dataSourcePath, filterRules ?? new FilterRule[0]);
            return subject.ToEnumerable();
        }

        public IObservable<RecordChangedParam[]> GetDataChanges(string dataSourcePath, FilterRule[] filterRules = null)
        {
            var subject = new Subject<RecordChangedParam[]>();
            _getDataChangesSubject.Subscribe(data => subject.OnNext(data),
                ex => subject.OnError(ex),
                () => subject.OnCompleted());

            var func = new Func<IObserver<RecordChangedParam[]>, IDisposable>(subj =>
            {
                var providerString = string.Copy(dataSourcePath);
                var disp = subject.Subscribe(subj);
                
                var whereCondition = filterRules != null
                    ? filterRules.Select(CopyFilterRule).ToArray()
                    : new FilterRule[0];

                var action = new Action(() =>
                {
                    CheckConnectionToServer();
                    if (_allowToRestoreConnection)
                    {
                        _allowToRestoreConnection = false;
                        Trace.WriteLine(string.Format("   Client GetDataChanges  ConnectionId : {0} , DataSourceName : {1} , IsBackGround {2}", _connection.ConnectionId, dataSourcePath, Thread.CurrentThread.IsBackground));
                        _proxy.Invoke("GetDataChanges", providerString, whereCondition);
                    }

                });

                var keepAliveTimer = new Timer(OnKeepAliveTick,action,5000,3000);

                return Disposable.Create(() =>
                {
                    keepAliveTimer.Dispose();
                    disp.Dispose();
                });
            });

            var returnObservable = Observable.Create(func);

            CheckConnectionToServer();

            _proxy.Invoke("GetDataChanges", dataSourcePath, filterRules ?? new FilterRule[0]);
            
            return returnObservable;
        }

        private void OnKeepAliveTick(object onTickAction)
        {
            var action = onTickAction as Action;
            if (action != null)
                action();
        }

       
        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord, string dataSourceUrn,
            Action<string, RecordChangedParam[]> onSuccess, Action<string, Exception> onFail)
        {
            _resolveRecordbyForeignKeySuccessAction = onSuccess;
            _resolveRecordbyForeignKeyFailedAction = onFail;

            CheckConnectionToServer();

            _proxy.Invoke("ResolveRecordbyForeignKey", changedRecord, dataSourceUrn);
        }

        private void CheckConnectionToServer()
        {
            
                if (_connection.State == ConnectionState.Disconnected)
                {
                    InitialiseConnection(_connectionString);
                    _allowToRestoreConnection = true;
                }
                if (!_startConnectionTask.IsCompleted)
                    _startConnectionTask.Wait();
            
        }

        private FilterRule CopyFilterRule(FilterRule filterRule)
        {
            return new FilterRule
            {
                Combine = filterRule.Combine,
                FieldName = filterRule.FieldName,
                Operation = filterRule.Operation,
                RuleNumber = filterRule.RuleNumber,
                Value = filterRule.Value
            };
        }
    }
}
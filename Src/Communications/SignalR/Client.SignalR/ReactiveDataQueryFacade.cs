using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.AspNet.SignalR.Client;

namespace ReactiveWorksheets.Client.SignalR
{
    internal class ReactiveDataQueryFacade : IReactiveDataQueryFacade
    {
        private readonly HubConnection _connection;
        private readonly IHubProxy _proxy;
        private readonly Task _startConnectionTask;
        
        // For  method GetAggregatedData
        private Action<Dictionary<string,object>> _getAggregatedDataOnNextAction;
        private Action _getAggregatedDataOnCompletetAction;
        private Action<Exception> _getAggregatedDataOnErrorAction;
       
        // *******************************

        // For etGenericData method
        private Action<object> _getGenericDataOnNextAction;
        private Action _getGenericDataOnCompleteAction;
        private Action<Exception> _getGenericDataOnErrorAction;
        //********************************

        // For GetData method
        private Action<Dictionary<string, object>> _getDataOnNextAction;
        private Action _getDataOnCompleteAction;
        private Action<Exception> _getDataOnErrorAction; 
        //********************************

        // For GetDataChanges method
        private readonly Subject<RecordChangedParam> _getDataChangesSubject; 
        //********************************

        // For ResolveRecordbyForeignKey method
        private Action<string, RecordChangedParam> _resolveRecordbyForeignKeySuccessAction;
        private Action<string, Exception> _resolveRecordbyForeignKeyFailedAction; 
        //********************************
        public ReactiveDataQueryFacade(string connectionString)
        {
            _connection = new HubConnection(connectionString);
            _proxy = _connection.CreateHubProxy("IReactiveDataQueryFacade");
           _getDataChangesSubject = new Subject<RecordChangedParam>();
            // For  method GetAggregatedData
            _proxy.On<Dictionary<string, object>>("GetAggregatedDataOnNext", data =>
            {
                //Trace.WriteLine(" *********GetAggregatedDataOnNext " + datasourceProviderString);
                if (_getAggregatedDataOnNextAction != null)
                    _getAggregatedDataOnNextAction(data);
            });

            _proxy.On("GetAggregatedDataOnComplete", () =>
            {
                //Trace.WriteLine(" *********GetAggregatedDataOnComplete " + datasourceProviderString);
                if (_getAggregatedDataOnCompletetAction != null)
                    _getAggregatedDataOnCompletetAction();
                
            });

            _proxy.On<Exception>("GetAggregatedDataOnError", ex =>
            {
                //Trace.WriteLine("   *********** GetAggregatedDataFailed " + datasourceProviderString);
                if (_getAggregatedDataOnErrorAction != null)
                    _getAggregatedDataOnErrorAction(ex);
            });
            // *********************************

            // For GetGenericData method
            _proxy.On<object>("GetGenericDataOnNext", data =>
            {
                //Trace.WriteLine(" *********GetGenericDataOnNext " + datasourceProviderString);
                if (_getGenericDataOnNextAction != null)
                    _getGenericDataOnNextAction(data);

            });

            _proxy.On("GetGenericDataOnComplete", () =>
            {
                //Trace.WriteLine("   *********** GetGenericDataOnComplete " + datasourceProviderString);
                if (_getGenericDataOnCompleteAction != null)
                    _getGenericDataOnCompleteAction();
            });

            _proxy.On<Exception>("GetGenericDataOnError", ex =>
            {
                if (_getGenericDataOnErrorAction != null)
                    _getGenericDataOnErrorAction(ex);
            });

            // *********************************

            // For GetData method
            _proxy.On<Dictionary<string, object>>("GetDataOnNext", data =>
            {
                //Trace.WriteLine(" *********GetDataOnNext " + datasourceProviderString);
                if (_getDataOnNextAction != null)
                    _getDataOnNextAction(data);
               
            });

            _proxy.On("GetDataOnComplete", () =>
            {
                //Trace.WriteLine("   *********** GetDataSuccess " + datasourceProviderString);
                if (_getDataOnCompleteAction != null)
                    _getDataOnCompleteAction();
            });

            _proxy.On<Exception>("GetDataOnError", ex =>
            {
                if (_getDataOnErrorAction != null)
                    _getDataOnErrorAction(ex);
            });

            // *********************************

            // For GetDataChanges method
            _proxy.On<RecordChangedParam>("GetDataChangesOnNext", data =>
            {
                //Trace.WriteLine(" ********* GetDataChangesOnNext " + datasourceProviderString);
                _getDataChangesSubject.OnNext(data);
            });

            _proxy.On("GetDataChangesOnComplete", () =>
            {
                //Trace.WriteLine("   *********** GetDataChangesOnComplete " + datasourceProviderString);
                _getDataChangesSubject.OnCompleted();
            });

            _proxy.On<Exception>("GetDataChangesOnError", ex =>
            {
                _getDataChangesSubject.OnError(ex);
            });
            // *********************************

            // For ResolveRecordbyForeignKey method
            _proxy.On<string, RecordChangedParam>("ResolveRecordbyForeignKeySuccess", (message, data) =>
            {
                //Trace.WriteLine(" ********* ResolveRecordbyForeignKeySuccess " + datasourceProviderString);
                _resolveRecordbyForeignKeySuccessAction(message, data);

            });

            _proxy.On<string, Exception>("ResolveRecordbyForeignKeyFailed", (message, ex) =>
            {
                //Trace.WriteLine("   *********** ResolveRecordbyForeignKeyFailed " + datasourceProviderString);
                _resolveRecordbyForeignKeyFailedAction(message, ex);
            });
            // **********************************
            _startConnectionTask = _connection.Start();
        }
        
        public void Dispose()
        {
            _connection.Stop();
        }

        // ***************************************************************************************
        public IEnumerable<Dictionary<string, object>> GetAggregatedData(string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null)
        {
            var subject = new Subject<Dictionary<string, object>>();

            _getAggregatedDataOnNextAction = subject.OnNext;
            _getAggregatedDataOnCompletetAction = subject.OnCompleted;
            _getAggregatedDataOnErrorAction = subject.OnError;

            if (_startConnectionTask.IsCompleted)
            {
                GetAggregatedDataHelper(dataSourcePath, aggregatedWorksheet, filterRules);
                return subject.ToEnumerable();
            }
            _startConnectionTask.Wait();
            GetAggregatedDataHelper(dataSourcePath, aggregatedWorksheet, filterRules);
            return subject.ToEnumerable();
        }

        private void GetAggregatedDataHelper(string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null)
        {
            _proxy.Invoke("GetAggregatedData", dataSourcePath, aggregatedWorksheet, filterRules ?? new FilterRule[0]);
        }

        // ***************************************************************************************

        // ***************************************************************************************
        public IEnumerable<T> GetData<T>(string dataSourcePath, FilterRule[] filterRules = null)
        {
            var subject = new Subject<object>();

            _getGenericDataOnNextAction = data => subject.OnNext(data);
            _getGenericDataOnCompleteAction = () => subject.OnCompleted();
            _getGenericDataOnErrorAction = ex => subject.OnError(ex);

            if (_startConnectionTask.IsCompleted)
            {
                _proxy.Invoke("GetGenericData", dataSourcePath, typeof (T), filterRules ?? new FilterRule[0]);
                return subject.ToEnumerable() as IEnumerable<T>;
            }
            _startConnectionTask.Wait();
            _proxy.Invoke("GetGenericData", dataSourcePath, typeof (T), filterRules ?? new FilterRule[0]);
            return subject.ToEnumerable() as IEnumerable<T>;
        }

        // ***************************************************************************************

        // ***************************************************************************************

        public IEnumerable<Dictionary<string, object>> GetData(string dataSourcePath, FilterRule[] filterRules = null)
        {
            var subject = new Subject<Dictionary<string,object>>();
           
            _getDataOnNextAction = data => subject.OnNext(data);
            _getDataOnCompleteAction =() => subject.OnCompleted();
            _getDataOnErrorAction = ex => subject.OnError(ex);
            
            if (_startConnectionTask.IsCompleted)
            {
                GetDataHelper(dataSourcePath, filterRules);
                return subject.ToEnumerable();
            }
            _startConnectionTask.Wait();
            GetDataHelper(dataSourcePath, filterRules);
            return subject.ToEnumerable();
        }

        private void GetDataHelper(string dataSourcePath, FilterRule[] filterRules = null)
        {
            _proxy.Invoke("GetData", dataSourcePath, filterRules ?? new FilterRule[0]);
        }

        // ***************************************************************************************

        // ***************************************************************************************

        public IObservable<RecordChangedParam> GetDataChanges(string dataSourcePath, FilterRule[] filterRules = null)
        {
            var subject = new Subject<RecordChangedParam>();
           _getDataChangesSubject.Where(r => r.ProviderString == dataSourcePath)
                .Subscribe( data => subject.OnNext(data), 
                            ex => subject.OnError(ex), 
                            () => subject.OnCompleted());

            if (!_startConnectionTask.IsCompleted)
                _startConnectionTask.Wait();
            
            _proxy.Invoke("GetDataChanges", dataSourcePath, filterRules ?? new FilterRule[0]);

            return subject.AsObservable();
        }
        // ***************************************************************************************

        // ***************************************************************************************

        public void ResolveRecordbyForeignKey(RecordChangedParam changedRecord, Action<string, RecordChangedParam> onSuccess, Action<string, Exception> onFail)
        {
            _resolveRecordbyForeignKeySuccessAction = onSuccess;
            _resolveRecordbyForeignKeyFailedAction = onFail;
            _proxy.Invoke("ResolveRecordbyForeignKey", changedRecord);
        }
        // ***************************************************************************************
    }
}
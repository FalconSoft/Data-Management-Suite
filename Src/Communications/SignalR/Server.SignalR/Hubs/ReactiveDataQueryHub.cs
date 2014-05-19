using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs
{
    [HubName("IReactiveDataQueryFacade")]
    public class ReactiveDataQueryHub : Hub
    {
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly ILogger _logger;
        private readonly Dictionary<string, CompositeDisposable> _getDataChangesDisposables;
        private readonly Dictionary<string, string> _dataSourcePathDictionary;

        public override Task OnConnected()
        {
            Trace.WriteLine("   Connected : " + Context.ConnectionId);
            _logger.InfoFormat("Time {0} | Connected: ConnectionId {1}, User {2}", DateTime.Now, Context.ConnectionId,
                Context.User != null ? Context.User.Identity.Name : null);
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            var connectionId = string.Copy(Context.ConnectionId);
            Trace.WriteLine("   Disconnected : " + connectionId);
            if (_getDataChangesDisposables.ContainsKey(connectionId))
            {
                
                _getDataChangesDisposables[connectionId].Dispose();
                _getDataChangesDisposables.Remove(connectionId);
                _logger.Info("remove subscribe for " + connectionId);
            }
            _logger.InfoFormat("Time {0} | Disconnected: ConnectionId {1}, User {2}", DateTime.Now, Context.ConnectionId,
                Context.User != null ? Context.User.Identity.Name : null);

            return base.OnDisconnected();
        }

        public ReactiveDataQueryHub(IReactiveDataQueryFacade reactiveDataQueryFacade, ILogger logger)
        {
            _getDataChangesDisposables = new Dictionary<string, CompositeDisposable>();
            _dataSourcePathDictionary = new Dictionary<string, string>();
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _logger = logger;
        }

        public void GetAggregatedData(string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
            FilterRule[] filterRules = null)
        {
            try
            {
                var data = _reactiveDataQueryFacade.GetAggregatedData(dataSourcePath, aggregatedWorksheet, filterRules);

                foreach (var d in data)
                {
                    Clients.Caller.GetAggregatedDataOnNext(d);
                }

                Clients.Caller.GetAggregatedDataOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetAggregatedDataOnError(ex);
                throw;
            }
        }

        public void GetGenericData(string dataSourcePath, Type type, FilterRule[] filterRules)
        {
            
            try
            {
                var mi = typeof (IReactiveDataQueryFacade).GetMethod("GetData");
                var miConstructed = mi.MakeGenericMethod(type);
                var result = (IEnumerable)
                    miConstructed.Invoke(_reactiveDataQueryFacade,
                        new object[] {dataSourcePath, filterRules.Any() ? filterRules : null});

                foreach (var obj in result)
                {
                    Clients.Caller.GetGenericDataOnNext(obj);
                }

                Clients.Caller.GetGenericDataOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetGenericDataOnError(ex);
                throw;
            }

        }

        public void GetData(string connectionId, string dataSourcePath, FilterRule[] filterRules)
        {
            Task.Factory.StartNew(()=>
            {
                Trace.WriteLine("   GetData Start connection Id : " + connectionId);
                try
                {
                    var data = _reactiveDataQueryFacade.GetData(dataSourcePath,
                        filterRules.Any() ? filterRules : null);

                    foreach (var d in data)
                    {
                        Clients.Client(connectionId).GetDataOnNext(d);
                    }


                    Clients.Client(connectionId).GetDataOnComplete();
                    Trace.WriteLine("   GetData Complete connection Id : " + connectionId);
                }
                catch (Exception ex)
                {
                    Clients.Client(connectionId).GetDataOnError(ex);
                    Trace.WriteLine("   GetData Failed connection Id : " + connectionId);
                    throw;
                }
            });
        }

        public void GetDataChanges(string connectionId, string dataSourcePath, FilterRule[] filterRules)
        {
            Task.Factory.StartNew(localConnectionId =>
            {
                var providerString = string.Copy(dataSourcePath);

                Trace.WriteLine(
                    string.Format("   GetDataChanges  ConnectionId : {0} , DataSourceName : {1} , IsBackGround {2}",
                        localConnectionId.ToString(), dataSourcePath, Thread.CurrentThread.IsBackground));

                if (!_getDataChangesDisposables.ContainsKey(localConnectionId.ToString()))
                    _getDataChangesDisposables.Add(localConnectionId.ToString(), new CompositeDisposable());

                var disposable = _reactiveDataQueryFacade.GetDataChanges(providerString,
                    filterRules.Any() ? filterRules : null)
                    .Subscribe(recordChangedParams =>
                    {
                        foreach (var recordChangedParam in recordChangedParams)
                        {
                            Clients.Client(localConnectionId.ToString()).GetDataChangesOnNext(recordChangedParam);
                        }
                    });

                _getDataChangesDisposables[localConnectionId.ToString()].Add(disposable);

            }, string.Copy(connectionId));
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord,string dataSourceUrn)
        {
            _reactiveDataQueryFacade.ResolveRecordbyForeignKey(changedRecord,dataSourceUrn,
                (str, rcp) => Clients.Caller.ResolveRecordbyForeignKeySuccess(str, rcp),
                (str, ex) => Clients.Caller.ResolveRecordbyForeignKeyFailed(str, ex));
        }

        private RecordChangedParam CopyRecordChangedParam(RecordChangedParam param)
        {
            return new RecordChangedParam
            {
                ChangeSource = param.ChangeSource,
                ChangedAction = param.ChangedAction,
                ChangedPropertyNames = param.ChangedPropertyNames,
                IgnoreWorksheet = param.IgnoreWorksheet,
                OriginalRecordKey = param.OriginalRecordKey,
                ProviderString = param.ProviderString,
                RecordKey = param.RecordKey,
                RecordValues = new Dictionary<string, object>(param.RecordValues),
                UserToken = param.UserToken
            };
        }
    }
}

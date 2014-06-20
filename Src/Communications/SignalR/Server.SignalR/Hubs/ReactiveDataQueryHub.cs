using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("IReactiveDataQueryFacade")]
    public class ReactiveDataQueryHub : Hub
    {
        private const int Limit = 100;

        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly ILogger _logger;
        private readonly Dictionary<string, CompositeDisposable> _getDataChangesDisposables;

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
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _logger = logger;
        }

        public void GetAggregatedData(string userToken, string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet,
            FilterRule[] filterRules = null)
        {
            try
            {
                var data = _reactiveDataQueryFacade.GetAggregatedData(userToken, dataSourcePath, aggregatedWorksheet, filterRules);
                var list = new List<Dictionary<string, object>>();
                var counter = 0;
                var count = 0;
                foreach (var d in data)
                {
                    ++counter;
                    ++count;
                    list.Add(d);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GetAggregatedDataOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GetAggregatedDataOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GetAggregatedDataOnComplete(count);
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

                var list = new List<object>();
                var count = 0;
                var counter = 0;

                foreach (var obj in result)
                {
                    ++counter;
                    ++count;
                    list.Add(obj);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GetGenericDataOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GetGenericDataOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GetGenericDataOnComplete(count);
            }
            catch (Exception ex)
            {
                Clients.Caller.GetGenericDataOnError(ex);
                throw;
            }

        }

        public void GetData(string connectionId, string userToken, string dataSourcePath, FilterRule[] filterRules)
        {
            {
                Trace.WriteLine("   GetData Start connection Id : " + connectionId);
                try
                {
                    var data = _reactiveDataQueryFacade.GetData(userToken, dataSourcePath, filterRules.Any() ? filterRules : null);
                    var counter = 0;
                    var count = 0;
                    var list = new List<Dictionary<string, object>>(); 
                    foreach (var d in data)
                    {
                        ++counter;
                        list.Add(d);
                        if (counter == Limit)
                        {
                            counter = 0;
                            Clients.Client(connectionId).GetDataOnNext(list.ToArray());
                            list.Clear();
                        }
                        ++count;
                    }
                    if (counter != 0)
                    {
                        Clients.Client(connectionId).GetDataOnNext(list.ToArray());
                        list.Clear();
                    }


                    Clients.Client(connectionId).GetDataOnComplete(count);
                    Trace.WriteLine("   GetData Complete connection Id : " + connectionId);
                }
                catch (Exception ex)
                {
                    Clients.Client(connectionId).GetDataOnError(ex);
                    Trace.WriteLine("   GetData Failed connection Id : " + connectionId);
                    throw;
                }
            }
        }

        public void GetDataChanges(string connectionId, string userToken, string dataSourcePath, FilterRule[] filterRules)
        {
            Task.Factory.StartNew(localConnectionId =>
            {
                var providerString = string.Copy(dataSourcePath);

                Trace.WriteLine(string.Format("   GetDataChanges  ConnectionId : {0} , DataSourceName : {1} , IsBackGround {2}",
                        localConnectionId.ToString(), dataSourcePath, Thread.CurrentThread.IsBackground));

                if (!_getDataChangesDisposables.ContainsKey(localConnectionId.ToString()))
                    _getDataChangesDisposables.Add(localConnectionId.ToString(), new CompositeDisposable());

                var disposable = _reactiveDataQueryFacade.GetDataChanges(userToken, providerString, filterRules.Any() ? filterRules : null)
                    .Subscribe(recordChangedParams =>
                    {
                        var list = new List<RecordChangedParam>();
                        var counter = 0;
                        foreach (var recordChangedParam in recordChangedParams)
                        {
                            ++counter;
                            list.Add(recordChangedParam);
                            if (counter == Limit)
                            {
                                counter = 0;
                                Clients.Client(localConnectionId.ToString()).GetDataChangesOnNext(list.ToArray());
                                list.Clear();
                            }
                        }

                        if (counter != 0)
                        {
                            Clients.Client(localConnectionId.ToString()).GetDataChangesOnNext(list.ToArray());
                            list.Clear();
                        }
                    });

                _getDataChangesDisposables[localConnectionId.ToString()].Add(disposable);

            }, string.Copy(connectionId));
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam[] changedRecord,string dataSourceUrn, string userToken)
        {
            _reactiveDataQueryFacade.ResolveRecordbyForeignKey(changedRecord, dataSourceUrn, userToken,
                (str, rcp) => Clients.Caller.ResolveRecordbyForeignKeySuccess(str, rcp),
                (str, ex) => Clients.Caller.ResolveRecordbyForeignKeyFailed(str, ex));
        }
    }
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

        public override Task OnConnected()
        {
            //Groups.Add(Context.ConnectionId, Context.QueryString["providerString"]);
            _logger.InfoFormat("Time {0} | Connected: ConnectionId {1}, User {2}", DateTime.Now, Context.ConnectionId, Context.User !=null ? Context.User.Identity.Name:null);
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            //Groups.Remove(Context.ConnectionId, Context.QueryString["providerString"]);
            _logger.InfoFormat("Time {0} | Disconnected: ConnectionId {1}, User {2}", DateTime.Now, Context.ConnectionId, Context.User != null ? Context.User.Identity.Name : null);
            return base.OnDisconnected();
        }

        public ReactiveDataQueryHub(IReactiveDataQueryFacade reactiveDataQueryFacade, ILogger logger)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _logger = logger;
        }

        public void GetAggregatedData(string dataSourcePath, AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null)
        {
            try
            {
                var data = _reactiveDataQueryFacade.GetAggregatedData(dataSourcePath, aggregatedWorksheet, filterRules);

                foreach (var d in data)
                {
                    Clients.Caller.GetAggregatedDataResult(d);
                }

                Clients.Caller.GetAggregatedDataSuccess();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetAggregatedDataFailed(ex);
                throw ex;
            }
        }

        public void GetGenericData(string dataSourcePath, Type type, FilterRule[] filterRules = null)
        {
            try
            {
                var mi = typeof (IReactiveDataQueryFacade).GetMethod("GetData");
                var miConstructed = mi.MakeGenericMethod(type);
                var result = miConstructed.Invoke(_reactiveDataQueryFacade, new object[] {dataSourcePath, filterRules.Any() ? filterRules : null}) as IEnumerable;

                foreach (var obj in result)
                {
                    Clients.Caller.GetGenericDataOnNext(obj);
                }

                Clients.Caller.GetGenericDataOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetGenericDataOnError(ex);
                throw ex;
            }

        }

        public void GetData(string dataSourcePath, FilterRule[] filterRules = null)
        {
            try
            {
                var data = _reactiveDataQueryFacade.GetData(dataSourcePath, filterRules.Any() ? filterRules : null);
                
                foreach (var d in data)
                {
                    Clients.Caller.GetDataOnNext(d);
                }

                Clients.Caller.GetDataOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetDataOnError(ex);
                throw ex;
            }
        }

        public void GetDataChanges(string dataSourcePath, FilterRule[] filterRules = null)
        {
            _reactiveDataQueryFacade.GetDataChanges(dataSourcePath, filterRules.Any() ? filterRules : null)
                .Subscribe(r => Clients.Caller.GetDataChangesOnNext(r));
        }

        public void ResolveRecordbyForeignKey(RecordChangedParam changedRecord)
        {
            _reactiveDataQueryFacade.ResolveRecordbyForeignKey(changedRecord, 
                (str,rcp) => Clients.Caller.ResolveRecordbyForeignKeySuccess(str,rcp), 
                (str,ex) => Clients.Caller.ResolveRecordbyForeignKeyFailed(str,ex));
        }

      
    }
}

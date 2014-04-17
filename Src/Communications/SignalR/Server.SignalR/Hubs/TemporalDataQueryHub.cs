using System;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs
{
    [HubName("ITemporalDataQueryFacade")]
    public class TemporalDataQueryHub : Hub
    {
        private readonly ITemporalDataQueryFacade _temporalDataQueryFacade;

        public TemporalDataQueryHub(ITemporalDataQueryFacade temporalDataQueryFacade)
        {
            _temporalDataQueryFacade = temporalDataQueryFacade;
        }

        public void GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            try
            {
                var enumerator = _temporalDataQueryFacade.GetRecordsHistory(dataSourceInfo, recordKey);
                foreach (var data in enumerator)
                {
                    Clients.Caller.GetRecordsHistoryOnNext(data);
                }

                Clients.Caller.GetRecordsHistoryOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetRecordsHistoryOnError(ex);
                throw ex;
            }
        }

        public void GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            try
            {
                var enumerator = _temporalDataQueryFacade.GetDataHistoryByTag(dataSourceInfo, tagInfo);
                foreach (var data in enumerator)
                {
                    Clients.Caller.GetDataHistoryByTagOnNext(data);
                }

                Clients.Caller.GetDataHistoryByTagOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetDataHistoryByTagOnError(ex);
                throw ex;
            }
        }

        public void GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            try
            {
                var enumerator = _temporalDataQueryFacade.GetRecordsAsOf(dataSourceInfo, timeStamp);
                foreach (var data in enumerator)
                {
                    Clients.Caller.GetRecordsAsOfOnNext(data);
                }

                Clients.Caller.GetRecordsAsOfOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GetRecordsAsOfOnError(ex);
                throw ex;
            }
        }

        public void GeTagInfos()
        {
            try
            {
                var enumerator = _temporalDataQueryFacade.GeTagInfos();
                foreach (var data in enumerator)
                {
                    Clients.Caller.GeTagInfosOnNext(data);
                }

                Clients.Caller.GeTagInfosOnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.GeTagInfosOnError(ex);
                throw ex;
            }
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            _temporalDataQueryFacade.SaveTagInfo(tagInfo);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            _temporalDataQueryFacade.RemoveTagInfo(tagInfo);
        }

        
    }
}


using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("ITemporalDataQueryFacade")]
    public class TemporalDataQueryHub : Hub
    {
        private const int Limit = 100;

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
                var list = new List<Dictionary<string, object>>();
                var counter = 0;
                var count = 0;
                foreach (var data in enumerator)
                {
                    ++counter;
                    ++count;
                    list.Add(data);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GetRecordsHistoryOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GetRecordsHistoryOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GetRecordsHistoryOnComplete(count);
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
                var list = new List<Dictionary<string, object>>();
                var counter = 0;
                var count = 0;
                foreach (var data in enumerator)
                {
                    ++counter;
                    ++count;
                    list.Add(data);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GetDataHistoryByTagOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GetDataHistoryByTagOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GetDataHistoryByTagOnComplete(count);
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
                var list = new List<Dictionary<string, object>>();
                var counter = 0;
                var count = 0;
                foreach (var data in enumerator)
                {
                    ++counter;
                    ++count;
                    list.Add(data);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GetRecordsAsOfOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GetRecordsAsOfOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GetRecordsAsOfOnComplete(count);
            }
            catch (Exception ex)
            {
                Clients.Caller.GetRecordsAsOfOnError(ex);
                throw ex;
            }
        }

        public void GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo,object revisionId)
        {
            try
            {
                var enumerator = _temporalDataQueryFacade.GetTemporalDataByRevisionId(dataSourceInfo,revisionId);
                var list = new List<Dictionary<string, object>>();
                var counter = 0;
                var count = 0;
                foreach (var data in enumerator)
                {
                    ++counter;
                    ++count;
                    list.Add(data);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GetTemporalDataByRevisionIdOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GetTemporalDataByRevisionIdOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GetTemporalDataByRevisionIdOnComplete(count);
            }
            catch (Exception ex)
            {
                Clients.Caller.GetTemporalDataByRevisionIdOnError(ex);
                throw ex;
            }
        }

        public void GetRevisions(DataSourceInfo dataSourceInfo)
        {
            try
            {
                var enumerator = _temporalDataQueryFacade.GetRevisions(dataSourceInfo);
                var list = new List<Dictionary<string, object>>();
                var counter = 0;
                var count = 0;
                foreach (var data in enumerator)
                {
                    ++counter;
                    ++count;
                    list.Add(data);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GetRevisionsOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GetRevisionsOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GetRevisionsOnComplete(count);
            }
            catch (Exception ex)
            {
                Clients.Caller.GetRevisionsOnError(ex);
                throw ex;
            }
        }

        public void GeTagInfos()
        {
            try
            {
                var enumerator = _temporalDataQueryFacade.GeTagInfos();
                var list = new List<TagInfo>();
                var counter = 0;
                var count = 0;
                
                foreach (var data in enumerator)
                {
                    ++counter;
                    ++count;
                    list.Add(data);
                    if (counter == Limit)
                    {
                        counter = 0;
                        Clients.Caller.GeTagInfosOnNext(list.ToArray());
                        list.Clear();
                    }
                }

                if (counter != 0)
                {
                    Clients.Caller.GeTagInfosOnNext(list.ToArray());
                    list.Clear();
                }

                Clients.Caller.GeTagInfosOnComplete(count);
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
            Clients.Caller.OnComplete();
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            _temporalDataQueryFacade.RemoveTagInfo(tagInfo);
            Clients.Caller.OnComplete();
        }

        
    }
}

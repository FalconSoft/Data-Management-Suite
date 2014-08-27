using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    internal sealed class TemporalDataQueryFacade : RabbitMQFacadeBase, ITemporalDataQueryFacade
    {
        private const string TemporalDataQueryFacadeQueryName = "TemporalDataQueryFacadeRPC";

        public TemporalDataQueryFacade(string hostName, string userName, string password):base(hostName, userName, password)
        {
            InitializeConnection(TemporalDataQueryFacadeQueryName);
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, TemporalDataQueryFacadeQueryName,
               "GetRecordsHistory", null, new object[] { dataSourceInfo, recordKey });
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, TemporalDataQueryFacadeQueryName,
               "GetDataHistoryByTag", null, new object[] { dataSourceInfo, tagInfo });
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, TemporalDataQueryFacadeQueryName,
                "GetRecordsAsOf", null, new object[] { dataSourceInfo, timeStamp });
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo, object revisionId)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, TemporalDataQueryFacadeQueryName,
                "GetTemporalDataByRevisionId", null, new[] { dataSourceInfo, revisionId });
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            return RPCServerTaskExecuteEnumerable<Dictionary<string, object>>(Connection, TemporalDataQueryFacadeQueryName,
                "GetRevisions", null, new object[] { dataSourceInfo });
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            return RPCServerTaskExecute<TagInfo[]>(Connection, TemporalDataQueryFacadeQueryName, "GeTagInfos", null,
                null);
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            RPCServerTaskExecute(Connection, TemporalDataQueryFacadeQueryName, "SaveTagInfo", null,
               new object[] { tagInfo });
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            RPCServerTaskExecute(Connection, TemporalDataQueryFacadeQueryName, "RemoveTagInfo", null,
                new object[] { tagInfo });
        }

        public void Dispose()
        {
            
        }

        public new void Close()
        {
            base.Close(); 
        }

    }
}
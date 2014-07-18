using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class TemporalDataQueryFacade : ITemporalDataQueryFacade
    {
        public TemporalDataQueryFacade(string serverUrl)
        {
            
        }

        public void Dispose()
        {
            
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            return null;
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            return null;
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            return null;
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo, object revisionId)
        {
            return null;
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            return null;
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            return null;
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            
        }
    }
}
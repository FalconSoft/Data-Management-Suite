using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class TemporalDataQueryFacade : ITemporalDataQueryFacade
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo, object revisionId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            throw new NotImplementedException();
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            throw new NotImplementedException();
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            throw new NotImplementedException();
        }
    }
}
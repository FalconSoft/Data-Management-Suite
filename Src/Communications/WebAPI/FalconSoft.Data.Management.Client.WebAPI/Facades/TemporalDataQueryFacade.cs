using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class TemporalDataQueryFacade : WebApiClientBase, ITemporalDataQueryFacade
    {
        public TemporalDataQueryFacade(string url, IRabbitMQClient client)
            : base(url, "TemporalDataApi", client) { }
        
        public IEnumerable<Dictionary<string, object>> GetRecordsHistory(DataSourceInfo dataSourceInfo, string recordKey)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>, DataSourceInfo>("GetRecordsHistory", dataSourceInfo,
                new Dictionary<string, object>
                {
                    {"recordKey", recordKey}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>, DataSourceInfo>("GetDataHistoryByTag", dataSourceInfo,
                new Dictionary<string, object>
                {
                    {"tagInfo", tagInfo}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>, DataSourceInfo>("GetRecordsAsOf", dataSourceInfo,
                new Dictionary<string, object>
                {
                    {"timeStamp", timeStamp}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo, object revisionId)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>, DataSourceInfo>("GetTemporalDataByRevisionId", dataSourceInfo,
                new Dictionary<string, object>
                {
                    {"revisionId", revisionId}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>, DataSourceInfo>("GetRevisions", dataSourceInfo,
                 new Dictionary<string, object>());
        }

        public IEnumerable<TagInfo> GeTagInfos()
        {
            return GetWebApiCall<TagInfo[]>("GeTagInfos");
        }

        public void SaveTagInfo(TagInfo tagInfo)
        {
            PostWebApiCall("SaveTagInfo", tagInfo);
        }

        public void RemoveTagInfo(TagInfo tagInfo)
        {
            PostWebApiCall("RemoveTagInfo", tagInfo);
        }

        public void Dispose()
        {

        }
    }
}
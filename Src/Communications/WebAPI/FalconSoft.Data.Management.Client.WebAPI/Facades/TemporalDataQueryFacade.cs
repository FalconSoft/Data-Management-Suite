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
            return GetStreamDataToEnumerable<Dictionary<string, object>>("GetRecordsHistory",
                new Dictionary<string, object>
                {
                    {"dataSourceInfo", dataSourceInfo},
                    {"recordKey", recordKey}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetDataHistoryByTag(DataSourceInfo dataSourceInfo, TagInfo tagInfo)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>>("GetDataHistoryByTag",
                new Dictionary<string, object>
                {
                    {"dataSourceInfo", dataSourceInfo},
                    {"tagInfo", tagInfo}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetRecordsAsOf(DataSourceInfo dataSourceInfo, DateTime timeStamp)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>>("GetRecordsAsOf",
                new Dictionary<string, object>
                {
                    {"dataSourceInfo", dataSourceInfo},
                    {"timeStamp", timeStamp}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetTemporalDataByRevisionId(DataSourceInfo dataSourceInfo, object revisionId)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>>("GetTemporalDataByRevisionId",
                new Dictionary<string, object>
                {
                    {"dataSourceInfo", dataSourceInfo},
                    {"revisionId", revisionId}
                });
        }

        public IEnumerable<Dictionary<string, object>> GetRevisions(DataSourceInfo dataSourceInfo)
        {
            return GetStreamDataToEnumerable<Dictionary<string, object>>("GetRevisions",
                 new Dictionary<string, object>
                {
                    {"dataSourceInfo", dataSourceInfo}
                });
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
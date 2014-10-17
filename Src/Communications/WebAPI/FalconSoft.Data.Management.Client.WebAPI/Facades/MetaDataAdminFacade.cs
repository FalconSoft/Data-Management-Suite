using System;
using System.Collections.Generic;
using System.Net.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class MetaDataAdminFacade : WebApiClientBase, IMetaDataAdminFacade
    {
        public MetaDataAdminFacade(string url)
            : base(url, "MetaDataApi")
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return GetWebApiCall<DataSourceInfo[]>("GetAvailableDataSources",
                new Dictionary<string, object> { { "userToken", userToken }, { "minAccessLevel", minAccessLevel } });
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            return GetWebApiCall<DataSourceInfo>("GetDataSourceInfo",
                new Dictionary<string, object> {{"dataSourceUrn", dataSourceUrn}, {"userToken", userToken}});
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;
        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            PostWebApiCall("UpdateDataSourceInfo", userToken, new object[] { dataSource, oldDataSourceUrn });
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            PostWebApiCall("CreateDataSourceInfo", userToken, new object[]{ dataSource });
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            return GetWebApiCall<WorksheetInfo>("GetWorksheetInfo",
                new Dictionary<string, object> { { "worksheetUrn", worksheetUrn }, { "userToken", userToken } });
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return GetWebApiCall<WorksheetInfo[]>("GetAvailableWorksheets",
                new Dictionary<string, object> { { "userToken", userToken }, { "minAccessLevel", minAccessLevel } });
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            throw new NotImplementedException();
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return GetWebApiCall<AggregatedWorksheetInfo[]>("GetAvailableAggregatedWorksheets",
               new Dictionary<string, object> { { "userToken", userToken }, { "minAccessLevel", minAccessLevel } });
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            throw new NotImplementedException();
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            return GetWebApiCall<AggregatedWorksheetInfo>("GetAggregatedWorksheetInfo",
              new Dictionary<string, object> { { "worksheetUrn", worksheetUrn }, { "userToken", userToken } });
        }

        public ServerInfo GetServerInfo()
        {
            throw new NotImplementedException();
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}
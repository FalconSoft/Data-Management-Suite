﻿using System;
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
        private HttpClient _client;

        public MetaDataAdminFacade(string url)
            : base(url, "MetaDataApi")
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:8080");
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            throw new NotImplementedException();
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            return GetWebApiCall<DataSourceInfo>("GetDataSourceInfo",
                new Dictionary<string, object> {{"dataSourceUrn", dataSourceUrn}, {"userToken", userToken}});
        }

        public event EventHandler<SourceObjectChangedEventArgs> ObjectInfoChanged;
        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userToken)
        {
            throw new NotImplementedException();
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            throw new NotImplementedException();
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public ServerInfo GetServerInfo()
        {
            throw new NotImplementedException();
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}
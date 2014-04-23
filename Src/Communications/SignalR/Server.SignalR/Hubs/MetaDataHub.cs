﻿using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs
{
    [HubName("IMetaDataAdminFacade")]
    public class MetaDataHub : Hub
    {
        private readonly IMetaDataAdminFacade _metaDataFacade;
        public MetaDataHub(IMetaDataAdminFacade metaDataFacade)
        {
            _metaDataFacade = metaDataFacade;
            _metaDataFacade.ObjectInfoChanged += OnObjectInfoChanged;
        }
        
        public DataSourceInfo[] GetAvailableDataSources(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return  _metaDataFacade.GetAvailableDataSources(userId, minAccessLevel);
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn)
        {
            return  _metaDataFacade.GetDataSourceInfo(dataSourceUrn);
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userId)
        {
            _metaDataFacade.UpdateDataSourceInfo(dataSource,oldDataSourceUrn,userId);
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userId)
        {
            _metaDataFacade.CreateDataSourceInfo(dataSource, userId);
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userId)
        {
            _metaDataFacade.DeleteDataSourceInfo(dataSourceUrn, userId);
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn)
        {
            return _metaDataFacade.GetWorksheetInfo(worksheetUrn);
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId,AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return _metaDataFacade.GetAvailableWorksheets(userId, minAccessLevel);
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            _metaDataFacade.UpdateWorksheetInfo(wsInfo, oldWorksheetUrn, userId);
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            _metaDataFacade.CreateWorksheetInfo(wsInfo, userId);
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            _metaDataFacade.DeleteWorksheetInfo(worksheetUrn, userId);
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn)
        {
            return  _metaDataFacade.GetAggregatedWorksheetInfo(worksheetUrn);
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return  _metaDataFacade.GetAvailableAggregatedWorksheets(userId, minAccessLevel);
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            _metaDataFacade.UpdateAggregatedWorksheetInfo(wsInfo, oldWorksheetUrn, userId);
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            _metaDataFacade.CreateAggregatedWorksheetInfo(wsInfo, userId);
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            _metaDataFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userId);
        }

        public ServiceSourceInfo[] GetAvailableServiceSources(string userId)
        {
            return  _metaDataFacade.GetAvailableServiceSources(userId);
        }

        public ServiceSourceInfo GetServiceSourceInfo(string serviceSourceUrn)
        {
            return _metaDataFacade.GetServiceSourceInfo(serviceSourceUrn);
        }

        public void CreateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string userId)
        {
            _metaDataFacade.CreateServiceSourceInfo(serviceSourceInfo, userId);
        }

        public void UpdateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string oldUrn, string userId)
        {
            _metaDataFacade.UpdateServiceSourceInfo(serviceSourceInfo, oldUrn, userId);
        }

        public void DeleteServiceSourceInfo(string serviceSourceUrn, string userId)
        {
           _metaDataFacade.DeleteServiceSourceInfo(serviceSourceUrn, userId);
        }
      
        private void OnObjectInfoChanged(object sender, SourceObjectChangedEventArgs e)
        {
            Clients.All.ObjectInfoChanged(e);
        }
    }
}

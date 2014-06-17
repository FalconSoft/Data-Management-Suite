using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
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
            return _metaDataFacade.GetAvailableDataSources(userId, minAccessLevel) ?? new DataSourceInfo[0];
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn)
        {
            return  _metaDataFacade.GetDataSourceInfo(dataSourceUrn);
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userId)
        {
            _metaDataFacade.UpdateDataSourceInfo(dataSource,oldDataSourceUrn,userId);
            Clients.Caller.OnComplete();
        }

        public void CreateDataSourceInfo(DataSourceInfo dataSource, string userId)
        {
            _metaDataFacade.CreateDataSourceInfo(dataSource, userId);
            Clients.Caller.OnComplete();
        }

        public void DeleteDataSourceInfo(string dataSourceUrn, string userId)
        {
            _metaDataFacade.DeleteDataSourceInfo(dataSourceUrn, userId);
            Clients.Caller.OnComplete();
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
            Clients.Caller.OnComplete();
        }

        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            _metaDataFacade.CreateWorksheetInfo(wsInfo, userId);
            Clients.Caller.OnComplete();
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            _metaDataFacade.DeleteWorksheetInfo(worksheetUrn, userId);
            Clients.Caller.OnComplete();
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
            Clients.Caller.OnComplete();
        }

        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            _metaDataFacade.CreateAggregatedWorksheetInfo(wsInfo, userId);
            Clients.Caller.OnComplete();
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            _metaDataFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userId);
            Clients.Caller.OnComplete();
        }

        public ServerInfo GetServerInfo()
        {
            return _metaDataFacade.GetServerInfo();
        }
      
        private void OnObjectInfoChanged(object sender, SourceObjectChangedEventArgs e)
        {
            Clients.All.ObjectInfoChanged(e);
        }
    }
}

using System;
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
            try
            {
                return _metaDataFacade.GetAvailableDataSources(userId, minAccessLevel) ?? new DataSourceInfo[0];
            }
            catch (Exception ex )
            {
                Clients.Caller.ErrorMessageHandledAction("GetAvailableDataSources", ex.Message);
                return new DataSourceInfo[0];
            }
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            try
            {
                return _metaDataFacade.GetDataSourceInfo(dataSourceUrn, userToken);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetDataSourceInfo", ex.Message);
                return null;
            }
        }

        public object UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceUrn, string userId)
        {
            try
            {
                _metaDataFacade.UpdateDataSourceInfo(dataSource, oldDataSourceUrn, userId);
                return new object();
                //Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("UpdateDataSourceInfo", ex.Message);
                return new object();
            }
        }

        public object CreateDataSourceInfo(string connectionId, DataSourceInfo dataSource, string userId)
        {
            try
            {
                _metaDataFacade.CreateDataSourceInfo(dataSource, userId);
                return new object();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("UpdateDataSourceInfo", ex.Message);
                return new object();
            }
        }

        public object DeleteDataSourceInfo(string dataSourceUrn, string userId)
        {
            try
            {
                _metaDataFacade.DeleteDataSourceInfo(dataSourceUrn, userId);
                return new object();
                //Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("DeleteDataSourceInfo", ex.Message);
                return new object();
            }
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            try
            {
                return _metaDataFacade.GetWorksheetInfo(worksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetWorksheetInfo", ex.Message);
                return null;
            }
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId,AccessLevel minAccessLevel = AccessLevel.Read)
        {
            try
            {
                return _metaDataFacade.GetAvailableWorksheets(userId, minAccessLevel);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetAvailableWorksheets", ex.Message);
                return new WorksheetInfo[0];
            }
        }

        public object UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            try
            {
                _metaDataFacade.UpdateWorksheetInfo(wsInfo, oldWorksheetUrn, userId);
                return new object();
                //Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("UpdateWorksheetInfo", ex.Message); 
                return new object();
                //
            }
        }

        public object CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            try
            {
                _metaDataFacade.CreateWorksheetInfo(wsInfo, userId);
               return new object();
                // Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("CreateWorksheetInfo", ex.Message);
                return new object();
            }
        }


        public object DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            try
            {
                _metaDataFacade.DeleteWorksheetInfo(worksheetUrn, userId);
                return new object();
                //Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("DeleteWorksheetInfo", ex.Message);
                return new object();
                //
            }
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            try
            {
                return _metaDataFacade.GetAggregatedWorksheetInfo(worksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetAggregatedWorksheetInfo", ex.Message);
                return null;
            }
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel)
        {
            try
            {
                return  _metaDataFacade.GetAvailableAggregatedWorksheets(userId, minAccessLevel);
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetAvailableAggregatedWorksheets", ex.Message);
                return new AggregatedWorksheetInfo[0];
            }
        }

        public object UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userId)
        {
            try
            {
                _metaDataFacade.UpdateAggregatedWorksheetInfo(wsInfo, oldWorksheetUrn, userId);
                return new object();
                //Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("UpdateAggregatedWorksheetInfo", ex.Message);
                return new object();
                //
            }
        }

        public object CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            try
            {
                _metaDataFacade.CreateAggregatedWorksheetInfo(wsInfo, userId);
                return new object();
                //Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("CreateAggregatedWorksheetInfo", ex.Message);
                return new object();
                //
            }
        }

        public object DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            try
            {
                _metaDataFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userId);
                return new object();
                // Clients.Caller.OnComplete();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("DeleteAggregatedWorksheetInfo", ex.Message);
                return new object();
                //
            }
        }

        public ServerInfo GetServerInfo()
        {
            try
            {
                return _metaDataFacade.GetServerInfo();
            }
            catch (Exception ex)
            {
                Clients.Caller.ErrorMessageHandledAction("GetServerInfo", ex.Message);
                return null;
            }
        }
      
        private void OnObjectInfoChanged(object sender, SourceObjectChangedEventArgs e)
        {
            Clients.All.ObjectInfoChanged(e);
        }
    }
}

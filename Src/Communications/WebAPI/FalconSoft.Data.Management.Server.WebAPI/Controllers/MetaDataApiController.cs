using System;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using FalconSoft.Data.Management.Server.WebAPI.Attributes;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class MetaDataApiController : ApiController
    {
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ILogger _logger;

        public MetaDataApiController(IMetaDataAdminFacade metaDataAdminFacade, ILogger logger)
        {
            _metaDataAdminFacade = metaDataAdminFacade;
            _logger = logger;
        }

        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            try
            {
                return _metaDataAdminFacade.GetAvailableDataSources(userToken, minAccessLevel);
            }
            catch (Exception ex)
            {
                _logger.Error("GetAvailableDataSources failed", ex);
                return new DataSourceInfo[0];
            }
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            try
            {
                return _metaDataAdminFacade.GetDataSourceInfo(dataSourceUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("GetDataSourceInfo failed", ex);
                return null;
            }
        }

        [HttpPost]
        public void UpdateDataSourceInfo(MethodArgs method)
        {
            try
            {
                _metaDataAdminFacade.UpdateDataSourceInfo(method.MethodsArgs[0] as DataSourceInfo, method.MethodsArgs[1] as string, method.UserToken);
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateDataSourceInfo failed", ex);
            }
        }

        [HttpPost]
        public void CreateDataSourceInfo(MethodArgs method)
        {
            try
            {
                _metaDataAdminFacade.CreateDataSourceInfo(method.MethodsArgs[0] as DataSourceInfo, method.UserToken);
            }
            catch (Exception ex)
            {
                _logger.Error("CreateDataSourceInfo failed", ex);
            }
        }

        [HttpPost]
        public void DeleteDataSourceInfo(string dataSourceUrn, string userToken)
        {
            try
            {
                _metaDataAdminFacade.DeleteDataSourceInfo(dataSourceUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("DeleteDataSourceInfo failed", ex);
            }
        }

        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            try
            {
                return _metaDataAdminFacade.GetWorksheetInfo(worksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("GetWorksheetInfo failed", ex);
                return null;
            }
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            try
            {
                return _metaDataAdminFacade.GetAvailableWorksheets(userToken, minAccessLevel);
            }
            catch (Exception ex)
            {
                _logger.Error("GetAvailableWorksheets failed", ex);
                return new WorksheetInfo[0];
            }
        }

        [BindJson(typeof(WorksheetInfo), "wsInfo")]
        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            try
            {
                _metaDataAdminFacade.UpdateWorksheetInfo(wsInfo, oldWorksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateWorksheetInfo failed", ex);
            }
        }

        [BindJson(typeof(WorksheetInfo), "wsInfo")]
        public void CreateWorksheetInfo(WorksheetInfo wsInfo, string userToken)
        {
            try
            {
                _metaDataAdminFacade.CreateWorksheetInfo(wsInfo, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("CreateWorksheetInfo failed", ex);
            }
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userToken)
        {
            try
            {
                _metaDataAdminFacade.DeleteWorksheetInfo(worksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("DeleteWorksheetInfo failed", ex);
            }
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            try
            {
                return _metaDataAdminFacade.GetAvailableAggregatedWorksheets(userToken, minAccessLevel);
            }
            catch (Exception ex)
            {
                _logger.Error("GetAvailableAggregatedWorksheets failed", ex);
                return new AggregatedWorksheetInfo[0];
            }
        }

        [BindJson(typeof(AggregatedWorksheetInfo), "wsInfo")]
        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string oldWorksheetUrn, string userToken)
        {
            try
            {
                _metaDataAdminFacade.UpdateAggregatedWorksheetInfo(wsInfo, oldWorksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateAggregatedWorksheetInfo failed", ex);
            }
        }

        [BindJson(typeof(AggregatedWorksheetInfo), "wsInfo")]
        public void CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userToken)
        {
            try
            {
                _metaDataAdminFacade.CreateAggregatedWorksheetInfo(wsInfo, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("CreateAggregatedWorksheetInfo failed", ex);
            }
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            try
            {
                _metaDataAdminFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("DeleteAggregatedWorksheetInfo failed", ex);
            }
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn, string userToken)
        {
            try
            {
                return _metaDataAdminFacade.GetAggregatedWorksheetInfo(worksheetUrn, userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("GetAggregatedWorksheetInfo failed", ex);
                return null;
            }
        }

        public ServerInfo GetServerInfo()
        {
            try
            {
                return _metaDataAdminFacade.GetServerInfo();
            }
            catch (Exception ex)
            {
                _logger.Error("GetServerInfo failed", ex);
                return null;
            }
        }
    }
}

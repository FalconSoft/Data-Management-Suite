using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using FalconSoft.Data.Management.Server.WebAPI.Hubs;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class MetaDataApiController : ApiController
    {
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ILogger _logger;
        private readonly Lazy<IHubContext> _metaDataHub = new Lazy<IHubContext>(() => GlobalHost.ConnectionManager.GetHubContext<MetaDataHub>());
        private volatile static Dictionary<string, string> _pushMessages = null;
        private static object _locker = new object();
        private static bool _subscribed = false;

        public Dictionary<string, string> PushMessages
        {
            get
            {
                if (_pushMessages == null)
                {
                    lock (_locker)
                    {
                        if (_pushMessages == null)
                            _pushMessages = new Dictionary<string, string>();
                    }
                }
                return _pushMessages;
            }
        }

        public MetaDataApiController()
        {
            _metaDataAdminFacade = FacadesFactory.MetaDataAdminFacade;
            _logger = FacadesFactory.Logger;

            if (!_subscribed)
            {
                _subscribed = true;
                FacadesFactory.MetaDataAdminFacade.ObjectInfoChanged += MetaDataAdminFacade_ObjectInfoChanged;
            }
        }

        private void MetaDataAdminFacade_ObjectInfoChanged(object sender, SourceObjectChangedEventArgs e)
        {
            string msg = JsonConvert.SerializeObject(e.SourceObjectInfo);
            string pushKey = DateTime.Now.Ticks.ToString();

            PushMessages.Add(pushKey, msg);

            _metaDataHub.Value.Clients.All.ObjectInfoChanged(pushKey, e.OldObjectUrn, e.ChangedActionType.ToString(),
                e.ChangedObjectType.ToString());
        }

        [HttpGet]
        public string GetPushedMetaDataObject(string userToken, string pushKey)
        {
            return PushMessages.ContainsKey(pushKey) ? PushMessages[pushKey] : null;
        }

        [HttpGet]
        public DataSourceInfo[] GetAvailableDataSources(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            _logger.Debug("Call MetaDataApiController GetAvailableDataSources");
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

        [HttpGet]
        public DataSourceInfo GetDataSourceInfo(string dataSourceUrn, string userToken)
        {
            _logger.Debug("Call MetaDataApiController GetDataSourceInfo");
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
        public HttpResponseMessage UpdateDataSourceInfo([FromBody]DataSourceInfo dataSource, [FromUri]string oldDataSourceUrn, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController UpdateDataSourceInfo");
            try
            {
                _metaDataAdminFacade.UpdateDataSourceInfo(dataSource, oldDataSourceUrn, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateDataSourceInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage CreateDataSourceInfo([FromBody]DataSourceInfo dataSource, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController CreateDataSourceInfo");
            try
            {
                _metaDataAdminFacade.CreateDataSourceInfo(dataSource, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("CreateDataSourceInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteDataSourceInfo([FromUri]string dataSourceUrn, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController DeleteDataSourceInfo");
            try
            {
                _metaDataAdminFacade.DeleteDataSourceInfo(dataSourceUrn, userToken); 
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("DeleteDataSourceInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public WorksheetInfo GetWorksheetInfo(string worksheetUrn, string userToken)
        {
            _logger.Debug("Call MetaDataApiController GetWorksheetInfo");
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

        [HttpGet]
        public WorksheetInfo[] GetAvailableWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            _logger.Debug("Call MetaDataApiController GetAvailableWorksheets");
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

        [HttpPost]
        public HttpResponseMessage UpdateWorksheetInfo([FromBody]WorksheetInfo wsInfo, [FromUri]string oldWorksheetUrn, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController UpdateWorksheetInfo");
            try
            {
                _metaDataAdminFacade.UpdateWorksheetInfo(wsInfo, oldWorksheetUrn, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateWorksheetInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage CreateWorksheetInfo([FromBody]WorksheetInfo wsInfo, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController CreateWorksheetInfo");
            try
            {
                _metaDataAdminFacade.CreateWorksheetInfo(wsInfo, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("CreateWorksheetInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteWorksheetInfo([FromUri]string worksheetUrn, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController DeleteWorksheetInfo");
            try
            {
                _metaDataAdminFacade.DeleteWorksheetInfo(worksheetUrn, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("DeleteWorksheetInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userToken, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            _logger.Debug("Call MetaDataApiController GetAvailableAggregatedWorksheets");
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

        [HttpPost]
        public HttpResponseMessage UpdateAggregatedWorksheetInfo([FromBody]AggregatedWorksheetInfo wsInfo, [FromUri]string oldWorksheetUrn, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController UpdateAggregatedWorksheetInfo");
            try
            {
                _metaDataAdminFacade.UpdateAggregatedWorksheetInfo(wsInfo, oldWorksheetUrn, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("UpdateAggregatedWorksheetInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage CreateAggregatedWorksheetInfo([FromBody]AggregatedWorksheetInfo wsInfo, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController CreateAggregatedWorksheetInfo");
            try
            {
                _metaDataAdminFacade.CreateAggregatedWorksheetInfo(wsInfo, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("CreateAggregatedWorksheetInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteAggregatedWorksheetInfo([FromUri]string worksheetUrn, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController DeleteAggregatedWorksheetInfo");
            try
            {
                _metaDataAdminFacade.DeleteAggregatedWorksheetInfo(worksheetUrn, userToken);
                var responce = new HttpResponseMessage(HttpStatusCode.OK);
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("DeleteAggregatedWorksheetInfo failed", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo([FromUri]string worksheetUrn, [FromUri]string userToken)
        {
            _logger.Debug("Call MetaDataApiController GetAggregatedWorksheetInfo");
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

        [HttpGet]
        public ServerInfo GetServerInfo()
        {
            _logger.Debug("Call MetaDataApiController GetServerInfo");
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

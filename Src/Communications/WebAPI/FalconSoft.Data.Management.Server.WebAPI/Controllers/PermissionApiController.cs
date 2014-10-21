using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class PermissionApiController : ApiController
    {
        private readonly IPermissionSecurityFacade _permissionFacade;
        private readonly IFalconSoftBroker _falconSoftBroker;
        private readonly ILogger _logger;

        public PermissionApiController(IPermissionSecurityFacade searchFacade, IFalconSoftBroker falconSoftBroker, ILogger logger)
        {
            _permissionFacade = searchFacade;
            _falconSoftBroker = falconSoftBroker;
            _logger = logger;
        }

        [HttpGet]
        public Permission GetUserPermissions(string userToken)
        {
            _logger.Debug("Call PermissionApiController GetUserPermissions");
            try
            {
                return _permissionFacade.GetUserPermissions(userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("GetUserPermissions failed ", ex);
                return new Permission();
            }
        }

        [HttpPost]
        public HttpResponseMessage SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction)
        {
            _logger.Debug("Call PermissionApiController SaveUserPermissions");
            try
            {
                var responce = new HttpResponseMessage();
                _permissionFacade.SaveUserPermissions(permissions, targetUserToken, grantedByUserToken,
                    msg =>
                    {
                        responce.StatusCode = HttpStatusCode.InternalServerError;
                        responce.Content = new ObjectContent(typeof(string), msg, new JsonMediaTypeFormatter());
                    });
                return responce;
            }
            catch (Exception ex)
            {
                _logger.Error("SaveUserPermissions failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetPermissionChanged(string userToken)
        {
            _logger.Debug("Call PermissionApiController GetPermissionChanged");
            try
            {
                _falconSoftBroker.SubscribeOnGetPermissionChanges(userToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.Error("GetPermissionChanged failed ", ex);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public AccessLevel CheckAccess(string userToken, string urn)
        {
            _logger.Debug("Call PermissionApiController CheckAccess");
            try
            {
                return _permissionFacade.CheckAccess(userToken, urn);
            }
            catch (Exception ex)
            {
                _logger.Error("GetUserPermissions failed ", ex);
                return new AccessLevel();
            }
        }
    }
}
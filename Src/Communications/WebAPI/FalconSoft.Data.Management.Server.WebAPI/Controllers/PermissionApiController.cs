using System;
using System.Collections.Generic;
using System.Reactive;
using System.Security;
using System.Web.Http;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Server.WebAPI.Controllers
{
    public class PermissionApiController : ApiController
    {
        private readonly IPermissionSecurityFacade _permissionFacade;
        private readonly ILogger _logger;

        public PermissionApiController(IPermissionSecurityFacade searchFacade, ILogger logger)
        {
            _permissionFacade = searchFacade;
            _logger = logger;
        }

        public Permission GetUserPermissions(string userToken)
        {
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


        public void SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction)
        {
            try
            {
                 _permissionFacade.SaveUserPermissions(permissions, targetUserToken, grantedByUserToken,
                    messageAction);
            }
            catch (Exception ex)
            {
                _logger.Error("SaveUserPermissions failed ", ex);
                return;
            }
        }


        public AccessLevel CheckAccess(string userToken, string urn)
        {
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

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            try
            {
                return _permissionFacade.GetPermissionChanged(userToken);
            }
            catch (Exception ex)
            {
                _logger.Error("GetUserPermissions failed ", ex);
                return new AnonymousObservable<Dictionary<string, AccessLevel>>();
            }
        }
        
    }
}
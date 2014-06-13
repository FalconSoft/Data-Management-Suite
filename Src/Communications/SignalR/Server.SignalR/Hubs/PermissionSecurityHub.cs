using System.Linq;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("IPermissionSecurityFacade")]
    public class PermissionSecurityHub : Hub
    {
        private IPermissionSecurityFacade _permissionSecurityPersistance;
        public PermissionSecurityHub(IPermissionSecurityFacade permissionSecurityPersistance)
        {
            _permissionSecurityPersistance = permissionSecurityPersistance;
        }

        public Permission[] GetUserPermissions(string userToken)
        {
            return _permissionSecurityPersistance.GetUserPermissions(userToken).ToArray();
        }

        public void SaveUserPermissions(string connectionId, Permission[] permissions, string targetUserToken,
            string grantedByUserToken)
        {
            _permissionSecurityPersistance.SaveUserPermissions(permissions, targetUserToken, grantedByUserToken, message => Clients.Client(connectionId).MessageAction(message));
            Clients.Client(connectionId).OnComplete();
        }
    }
}

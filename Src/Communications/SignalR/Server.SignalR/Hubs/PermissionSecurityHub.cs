using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FalconSoft.Data.Management.Server.SignalR.Hubs
{
    [HubName("IPermissionSecurityHub")]
    public class PermissionSecurityHub : Hub
    {
        private readonly IPermissionSecurityFacade _permissionSecurityPersistance;
        private readonly Dictionary<string, IDisposable> _disposables; 
        public PermissionSecurityHub(IPermissionSecurityFacade permissionSecurityPersistance)
        {
            _disposables = new Dictionary<string, IDisposable>();
            _permissionSecurityPersistance = permissionSecurityPersistance;
        }

        public override Task OnDisconnected()
        {
            if (_disposables.ContainsKey(Context.ConnectionId))
                _disposables[Context.ConnectionId].Dispose();

            return base.OnDisconnected();
        }

        public Permission GetUserPermissions(string userToken)
        {
            var data = _permissionSecurityPersistance.GetUserPermissions(userToken);
            return data ?? new Permission();
        }

        public void SaveUserPermissions(string connectionId, Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken)
        {
            _permissionSecurityPersistance.SaveUserPermissions(permissions, targetUserToken, grantedByUserToken, message => Clients.Client(connectionId).OnMessageAction(message));
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            return _permissionSecurityPersistance.CheckAccess(userToken, urn);
        }

        public void GetPermissionChanged(string connectionId, string userId)
        {
            var disposable  = _permissionSecurityPersistance.GetPermissionChanged(userId).Subscribe(
                dictionary => Clients.Client(connectionId).GetPermissionChangedOnNext(dictionary));
            _disposables.Add(connectionId,disposable);
        }
    }
}

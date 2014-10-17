using System;
using System.Collections.Generic;
using System.Net.Http;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class PermissionSecurityFacade : WebApiClientBase, IPermissionSecurityFacade
    {
        
        public PermissionSecurityFacade(string url)
            : base(url, "PermissionApi")
        {
            
        }
        

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Permission GetUserPermissions(string userToken)
        {
            return GetWebApiCall<Permission>("GetUserPermissions", new Dictionary<string, object> { { "userToken", userToken } });
        }

        public void SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction)
        {
            GetWebApiCall<Permission>("SaveUserPermissions", new Dictionary<string, object> { { "permissions", permissions }, { "targetUserToken", targetUserToken }, { "grantedByUserToken", grantedByUserToken }, { "messageAction", messageAction } });
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            return GetWebApiCall<AccessLevel>("CheckAccess", new Dictionary<string, object> { { "userToken", userToken }, { "urn", urn } });
        }

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            return GetWebApiCall<IObservable<Dictionary<string, AccessLevel>>>("GetPermissionChanged", new Dictionary<string, object> { { "userToken", userToken } });
        }
    }
}
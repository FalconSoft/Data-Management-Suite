using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class PermissionSecurityFacade : IPermissionSecurityFacade
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Permission GetUserPermissions(string userToken)
        {
            throw new NotImplementedException();
        }

        public void SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction)
        {
            throw new NotImplementedException();
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            throw new NotImplementedException();
        }

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            throw new NotImplementedException();
        }
    }
}
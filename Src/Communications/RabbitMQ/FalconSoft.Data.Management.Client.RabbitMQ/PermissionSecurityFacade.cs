using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.RabbitMQ
{
    public class PermissionSecurityFacade : IPermissionSecurityFacade
    {
        public PermissionSecurityFacade(string serverUrl)
        {
            
        }

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
            return AccessLevel.Read;
        }

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            var subjects = new Subject<Dictionary<string, AccessLevel>>();
            return subjects.AsObservable();
        }
    }
}
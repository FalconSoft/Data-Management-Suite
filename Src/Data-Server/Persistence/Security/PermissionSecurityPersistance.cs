using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Server.Persistence.Security
{
    public class PermissionSecurityPersistance : IPermissionSecurityPersistance
    {
        public PermissionSecurityPersistance(string connectionString)
        {
            
        }

        public IEnumerable<Management.Common.Security.Permission> GetUserPermissions(string userToken)
        {
            return new[]
            {
                new Permission
                {
                    PermissionName = "Read",
                    TargetDataSourcePath = "NoDS/NoPath",
                    AccessLevel = AccessLevel.Read
                },
                new Permission
                {
                    PermissionName = "MetaDataModify",
                    TargetDataSourcePath = "NoDS/NoPath",
                    AccessLevel = AccessLevel.MetaDataModify
                },
                new Permission
                {
                    PermissionName = "DataModify",
                    TargetDataSourcePath = "NoDS/NoPath",
                    AccessLevel = AccessLevel.DataModify
                },
            };
        }

        public void SaveUserPermissions(IEnumerable<Management.Common.Security.Permission> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction = null)
        {
            throw new NotImplementedException();
        }
    }
}

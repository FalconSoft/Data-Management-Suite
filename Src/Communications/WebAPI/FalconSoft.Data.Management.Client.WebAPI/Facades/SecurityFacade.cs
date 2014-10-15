using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class SecurityFacade : ISecurityFacade
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Authenticate(string userName, string password)
        {
            throw new NotImplementedException();
        }

        public List<User> GetUsers(string userToken)
        {
            throw new NotImplementedException();
        }

        public User GetUser(string userName)
        {
            throw new NotImplementedException();
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            throw new NotImplementedException();
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            throw new NotImplementedException();
        }

        public void RemoveUser(User user, string userToken)
        {
            throw new NotImplementedException();
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}
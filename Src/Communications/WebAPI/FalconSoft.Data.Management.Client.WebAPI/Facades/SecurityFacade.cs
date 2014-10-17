using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class SecurityFacade :WebApiClientBase, ISecurityFacade
    {
        public SecurityFacade(string url)
            : base(url, "SecurityApi")
        {
            
        }
        public void Dispose()
        {
            
        }

        public KeyValuePair<bool, string> Authenticate(string userName, string password)
        {
            return GetWebApiCall<KeyValuePair<bool, string>>("Authenticate", new Dictionary<string, object>
            {
                {"userName", userName},
                {"password", password}
            });
        }

        public List<User> GetUsers(string userToken)
        {
            return GetWebApiCall<List<User>>("GetUsers", new Dictionary<string, object>
            {
                {"userToken", userToken}
            });
        }

        public User GetUser(string userName)
        {
            return GetWebApiCall<User>("GetUser", new Dictionary<string, object>
            {
                {"userName", userName}
            });
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
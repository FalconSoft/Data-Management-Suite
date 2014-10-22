using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class SecurityFacade :WebApiClientBase, ISecurityFacade
    {
        private const string ExceptionsExchangeName = "SecurityFacadeExceptionsExchangeName";

        public SecurityFacade(string url, IRabbitMQClient rabbitMQClient)
            : base(url, "SecurityApi", rabbitMQClient)
        {
            if (rabbitMQClient!=null)
                rabbitMQClient.SubscribeOnExchange(ExceptionsExchangeName, "fanout", "", ErrorMessageHandledAction);
            
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
            return PostWebApiCallMessage("SaveNewUser", user, new Dictionary<string, object>
            {
                {"userRole", userRole},
                {"userToken", userToken}
            }).Result.Content.ReadAsStringAsync().Result;
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            PostWebApiCall("UpdateUser", user, new Dictionary<string, object>
            {
                {"userRole", userRole},
                {"userToken", userToken}
            });
        }

        public void RemoveUser(User user, string userToken)
        {
            PostWebApiCall("RemoveUser", user, new Dictionary<string, object>
            {
                {"userToken", userToken}
            });
        }

        public Action<string, string> ErrorMessageHandledAction { get; set; }
    }
}
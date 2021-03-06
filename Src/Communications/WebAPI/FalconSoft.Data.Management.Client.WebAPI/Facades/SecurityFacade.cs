﻿using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class SecurityFacade : WebApiClientBase, ISecurityFacade
    {
        public SecurityFacade(string url, ILogger log)
            : base(url, "SecurityApi", log) { }

        public void Dispose()
        {
            
        }

        public AuthenticationResult Authenticate(string companyName, string userName, string password)
        {
            var authResult = PostWithResultWebApiCall<Tuple<string, string, string>, AuthenticationResult>("AuthenticationPost", Tuple.Create(companyName, userName, password));

            return authResult;
        }

        public User[] GetUsers(string userToken)
        {
            return GetWebApiCall<User[]>("GetUsers", new Dictionary<string, object>
            {
                {"userToken", userToken}
            });
        }

        public Dictionary<string, string> GetUserSettings(string userToken)
        {
            return GetWebApiCall<Dictionary<string, string>>("GetUserSettings", new Dictionary<string, object>
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
            var taskResult = PostWebApiCallMessage("SaveNewUser", user, new Dictionary<string, object>
                {
                    {"userRole", userRole},
                    {"userToken", userToken}
                })
                .Result;
            taskResult.EnsureSuccessStatusCode();
            var result = taskResult.Content.ReadAsStringAsync().Result;
            return result;
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
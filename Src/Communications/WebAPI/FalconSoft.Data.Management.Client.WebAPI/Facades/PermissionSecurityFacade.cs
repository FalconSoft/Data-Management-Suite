using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reactive.Subjects;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class PermissionSecurityFacade : WebApiClientBase, IPermissionSecurityFacade
    {
        public PermissionSecurityFacade(string url)
            : base(url, "PermissionApi") { }

        public void Dispose()
        {
           
        }

        public Permission GetUserPermissions(string userToken)
        {
            return GetWebApiCall<Permission>("GetUserPermissions", new Dictionary<string, object>
            {
                { "userToken", userToken }
            });
        }

        public void SaveUserPermissions(Dictionary<string, AccessLevel> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction)
        {
            GetWebApiAsyncCall("SaveUserPermissions", 
                new Dictionary<string, object>
                {
                    { "permissions", permissions },
                    { "targetUserToken", targetUserToken }, 
                    { "grantedByUserToken", grantedByUserToken }
                }).ContinueWith(responceTask =>
                {
                    var responce = responceTask.Result;
                    if (responce.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        if (messageAction != null)
                        {
                            responce.Content.ReadAsAsync<string>()
                                .ContinueWith(readTask => messageAction(readTask.Result));
                        }
                    }
                });
        }

        public AccessLevel CheckAccess(string userToken, string urn)
        {
            return GetWebApiCall<AccessLevel>("CheckAccess", new Dictionary<string, object>
            {
                { "userToken", userToken }, 
                { "urn", urn }
            });
        }

        public IObservable<Dictionary<string, AccessLevel>> GetPermissionChanged(string userToken)
        {
            return new Subject<Dictionary<string, AccessLevel>>();
        }
    }
}
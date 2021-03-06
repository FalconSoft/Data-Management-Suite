﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI.Facades
{
    internal sealed class PermissionSecurityFacade : WebApiClientBase, IPermissionSecurityFacade
    {
        private const string PermissionSecurityFacadeExchangeName = "PermissionSecurityFacadeExchange";

        public PermissionSecurityFacade(string url, ILogger log)
            : base(url, "PermissionApi", log)
        {
        }

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
            PostWebApiCallMessage("SaveUserPermissions", permissions,
                new Dictionary<string, object>
                {
                    { "permissions", permissions },
                    { "targetUserToken", targetUserToken }, 
                    { "grantedByUserToken", grantedByUserToken }
                }).ContinueWith(responceTask =>
                {
                    var responce = responceTask.Result;
                    
                    if (messageAction != null)
                    {
                        responce.Content.ReadAsAsync<string>()
                            .ContinueWith(readTask => messageAction(readTask.Result));
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
            GetWebApiAsyncCall("GetPermissionChanged", new Dictionary<string, object>
            {
                {"userToken", userToken}
            }).Wait();


            var observable = CreateExchngeObservable<Dictionary<string, AccessLevel>>(
                PermissionSecurityFacadeExchangeName, "direct", userToken);

            return Observable.Create<Dictionary<string, AccessLevel>>(subj =>
            {
                var disposable = observable.Subscribe(subj);

                var keepAlive = new EventHandler<ServerReconnectionArgs>((obj, evArgs) =>
                {
                    disposable.Dispose();

                    observable = CreateExchngeObservable<Dictionary<string, AccessLevel>>(
                PermissionSecurityFacadeExchangeName, "direct", userToken);

                    disposable = observable.Subscribe(subj);

                    GetWebApiAsyncCall("GetPermissionChanged", new Dictionary<string, object>
                    {
                        {"userToken", userToken}
                    });
                });

                ServerReconnectedEvent += keepAlive;

                return Disposable.Create(() =>
                {
                    ServerReconnectedEvent -= keepAlive;
                    disposable.Dispose();

                });
            });
        }
    }
}
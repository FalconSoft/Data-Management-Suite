using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using FalconSoft.Data.Management.Client.WebAPI.Facades;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Security;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiFacadeFactory : IFacadesFactory
    {
        private string _url ;
        private string _pushUrl;
        private ICommandFacade _commandFacade;
        private IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private ITemporalDataQueryFacade _temporalDataQueryFacade;
        private IMetaDataAdminFacade _metaDataAdmnFacade;
        private ISearchFacade _searchFacade;
        private ISecurityFacade _securityFacade;
        private IPermissionSecurityFacade _permissionSecurityFacade;
        private readonly ILogger _log;

        public WebApiFacadeFactory(ILogger log)
        {
            _log = log;
        }

        public ICommandFacade CreateCommandFacade()
        {
            return _commandFacade ?? (_commandFacade = new CommandFacade(_url, _log));
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return _reactiveDataQueryFacade ?? (_reactiveDataQueryFacade = new ReactiveDataQueryFacade(_url, _pushUrl, _log));
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return _temporalDataQueryFacade ?? (_temporalDataQueryFacade = new TemporalDataQueryFacade(_url, _log));
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return _metaDataAdmnFacade ?? (_metaDataAdmnFacade = new MetaDataAdminFacade(_url, _pushUrl, _log));
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return _metaDataAdmnFacade ?? (_metaDataAdmnFacade = new MetaDataAdminFacade(_url, _pushUrl, _log));
        }

        public ISearchFacade CreateSearchFacade()
        {
            return _searchFacade ?? (_searchFacade = new SearchFacade(_url, _log));
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            return _securityFacade ?? (_securityFacade = new SecurityFacade(_url, _log));
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            return _permissionSecurityFacade ?? (_permissionSecurityFacade = new PermissionSecurityFacade(_url, _log));
        }

        public ITestFacade CreateTestFacade()
        {
            return new TestFacade();
        }

        public void Dispose()
        {
        }

        public User Authenticate(string url, string companyName, string userName, string password)
        {
            _url = url;
            var security = CreateSecurityFacade();

            var user = security.Authenticate(companyName, userName,  password);

            if (!string.IsNullOrWhiteSpace(user.Id))
            {
                var userSettings = security.GetUserSettings(user.Id);
                _pushUrl = userSettings["pushUrl"];
                return user;
            }

            return user;
        }

    }
}

using System;
using FalconSoft.Data.Management.Client.WebAPI.Facades;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiFacadeFactory : IFacadesFactory
    {
        private readonly string _url ;
        private ICommandFacade _commandFacade;
        private IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private ITemporalDataQueryFacade _temporalDataQueryFacade;
        private IMetaDataAdminFacade _metaDataAdmnFacade;
        private ISearchFacade _searchFacade;
        private ISecurityFacade _securityFacade;
        private IPermissionSecurityFacade _permissionSecurityFacade;
        private ILogger _log;

        public WebApiFacadeFactory(string url, ILogger log)
        {
            _url = url;
            _log = log;
        }

        public ICommandFacade CreateCommandFacade()
        {
            return _commandFacade ?? (_commandFacade = new CommandFacade(_url, _log));
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return _reactiveDataQueryFacade ?? (_reactiveDataQueryFacade = new ReactiveDataQueryFacade(_url, _log));
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return _temporalDataQueryFacade ?? (_temporalDataQueryFacade = new TemporalDataQueryFacade(_url, _log));
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return _metaDataAdmnFacade ?? (_metaDataAdmnFacade = new MetaDataAdminFacade(_url, _log));
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return _metaDataAdmnFacade ?? (_metaDataAdmnFacade = new MetaDataAdminFacade(_url, _log));
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
    }
}

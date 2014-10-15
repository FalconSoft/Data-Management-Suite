using System;
using System.Linq;
using System.Text;
using FalconSoft.Data.Management.Client.WebAPI.Facades;
using FalconSoft.Data.Management.Common.Facades;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class WebApiFacadeFactory : IFacadesFactory
    {
        public WebApiFacadeFactory()
        {
            
        }
        public ICommandFacade CreateCommandFacade()
        {
            return new CommandFacade();
        }

        public IReactiveDataQueryFacade CreateReactiveDataQueryFacade()
        {
            return new ReactiveDataQueryFacade();
        }

        public ITemporalDataQueryFacade CreateTemporalDataQueryFacade()
        {
            return new TemporalDataQueryFacade();
        }

        public IMetaDataAdminFacade CreateMetaDataAdminFacade()
        {
            return new MetaDataAdminFacade();
        }

        public IMetaDataFacade CreateMetaDataFacade()
        {
            return new MetaDataAdminFacade();
        }

        public ISearchFacade CreateSearchFacade()
        {
            return new SearchFacade();
        }

        public ISecurityFacade CreateSecurityFacade()
        {
            return new SecurityFacade();
        }

        public IPermissionSecurityFacade CreatePermissionSecurityFacade()
        {
            return new PermissionSecurityFacade();
        }

        public ITestFacade CreateTestFacade()
        {
            return new TestFacade();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

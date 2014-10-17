using System;
using System.Web.Http;
using System.Web.Http.SelfHost;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Server.WebAPI.Controllers;
using FalconSoft.Data.Management.Server.WebAPI.IoC;
using Ninject;
using Ninject.Web.Common;

namespace FalconSoft.Data.Management.Server.WebAPI
{
    public class SelfHostServer : IDisposable
    {
        private readonly IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private readonly IMetaDataAdminFacade _metaDataAdminFacade;
        private readonly ISearchFacade _searchFacade;
        private readonly ISecurityFacade _securityFacade;
        private readonly IPermissionSecurityFacade _permissionSecurityFacade;
        private readonly ITemporalDataQueryFacade _temporalDataQueryFacade;
        private readonly ILogger _logger;
        private HttpSelfHostServer _server;
        private ReactiveDataQueryApiController _reactiveDataQueryWebApi;
        private MetaDataApiController _metaDataWebApi;

        public SelfHostServer(IReactiveDataQueryFacade reactiveDataQueryFacade,
            IMetaDataAdminFacade metaDataAdminFacade,
            ISearchFacade searchFacade,
            ISecurityFacade securityFacade,
            IPermissionSecurityFacade permissionSecurityFacade,
            ITemporalDataQueryFacade temporalDataQueryFacade,
            ILogger logger)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _metaDataAdminFacade = metaDataAdminFacade;
            _searchFacade = searchFacade;
            _securityFacade = securityFacade;
            _permissionSecurityFacade = permissionSecurityFacade;
            _temporalDataQueryFacade = temporalDataQueryFacade;
            _logger = logger;
        }

        public void Start(string url)
        {
            var config = new HttpSelfHostConfiguration(url);

            var kernel = new StandardKernel();
            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            
            RegisterServices(kernel);

            config.DependencyResolver = new NinjectDependencyResolver(kernel);

            config.Routes.MapHttpRoute(
                "API Default", "api/{controller}/{action}/{id}",
                new { id = RouteParameter.Optional });

            using (_server = new HttpSelfHostServer(config))
            {
                _server.OpenAsync().Wait();

                Console.WriteLine("Press Enter to quit.");
                Console.ReadLine();
            }

        }

        public void Dispose()
        {
            _server.Dispose();
        }

        private void RegisterServices(IKernel kernel)
        {
            // This is where we tell Ninject how to resolve service requests
            kernel.Bind<IReactiveDataQueryFacade>().ToConstant(_reactiveDataQueryFacade);
            kernel.Bind<IMetaDataAdminFacade>().ToConstant(_metaDataAdminFacade);
            kernel.Bind<ISearchFacade>().ToConstant(_searchFacade);
            kernel.Bind<ISecurityFacade>().ToConstant(_securityFacade);
            kernel.Bind<IPermissionSecurityFacade>().ToConstant(_permissionSecurityFacade);
            kernel.Bind<ITemporalDataQueryFacade>().ToConstant(_temporalDataQueryFacade);
            kernel.Bind<ILogger>().ToConstant(_logger);
        }
    }
}

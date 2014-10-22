using System;
using System.ServiceModel;
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
        private readonly ICommandFacade _commandFacade;
        private readonly ILogger _logger;
        private HttpSelfHostServer _server;
        private readonly GlobalBroker _globalBroker;
        private StandardKernel _kernel;

        public SelfHostServer(IReactiveDataQueryFacade reactiveDataQueryFacade,
            IMetaDataAdminFacade metaDataAdminFacade,
            ISearchFacade searchFacade,
            ISecurityFacade securityFacade,
            IPermissionSecurityFacade permissionSecurityFacade,
            ITemporalDataQueryFacade temporalDataQueryFacade,
            ICommandFacade commandFacade,
            ILogger logger,
            string hostName,
            string userName,
            string password,
            string virtualHost)
        {
            _reactiveDataQueryFacade = reactiveDataQueryFacade;
            _metaDataAdminFacade = metaDataAdminFacade;
            _searchFacade = searchFacade;
            _securityFacade = securityFacade;
            _permissionSecurityFacade = permissionSecurityFacade;
            _temporalDataQueryFacade = temporalDataQueryFacade;
            _commandFacade = commandFacade;
            _logger = logger;

            var rabbitMq = new RabbitMQBroker(hostName, userName, password, virtualHost);

            _globalBroker = new GlobalBroker(rabbitMq,
                _reactiveDataQueryFacade,
                _metaDataAdminFacade,
                _permissionSecurityFacade,
                _securityFacade);
        }

        public void Start(string url)
        {
            var config = new HttpSelfHostConfiguration(url);
            config.TransferMode = TransferMode.StreamedRequest;
            config.MaxBufferSize = Int32.MaxValue;
            config.MaxReceivedMessageSize = Int32.MaxValue;

            _kernel = new StandardKernel();
            _kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);

            RegisterServices(_kernel);

            config.DependencyResolver = new NinjectDependencyResolver(_kernel);

            config.Routes.MapHttpRoute(
                "API Default", "api/{controller}/{action}/{id}",
                new { id = RouteParameter.Optional });

            _server = new HttpSelfHostServer(config);

            _server.OpenAsync().Wait();

            _logger.Debug("Web Api server is running ");
        }

        public void Dispose()
        {
            _server.CloseAsync().Wait();
            _server.Dispose();
            _kernel.Dispose();
            _globalBroker.Close();
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
            kernel.Bind<ICommandFacade>().ToConstant(_commandFacade);
            kernel.Bind<IFalconSoftBroker>().ToConstant(_globalBroker);
            kernel.Bind<ILogger>().ToConstant(_logger);
        }
    }
}

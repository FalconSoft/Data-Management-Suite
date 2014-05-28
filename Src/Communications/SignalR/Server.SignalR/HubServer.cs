using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FalconSoft.Data.Management.Server.SignalR.Hubs;
using FalconSoft.Data.Server.Common;
using FalconSoft.Data.Server.Common.Facade;
using Microsoft.AspNet.SignalR;
using Owin;

namespace FalconSoft.Data.Management.Server.SignalR
{
    internal class HubServer
    {
        public static ILogger Logger { get; set; }
        public static ICommandFacade CommandFacade { get; set; }
        public static IMetaDataAdminFacade MetaDataAdminFacade { get; set; }
        public static IReactiveDataQueryFacade ReactiveDataQueryFacade { get; set; }
        public static ITemporalDataQueryFacade TemporalDataQueryFacade { get; set; }
        public static ISearchFacade SearchFacade { get; set; }
        public static ISecurityFacade SecurityFacade { get; set; }
        public static string ConnectionString { get; set; }

        public void Configuration(IAppBuilder app)
        {
            var commandHub = new CommandsHub(CommandFacade);
            var metaDataHub = new MetaDataHub(MetaDataAdminFacade);
            var reactiveDataQueryHub = new ReactiveDataQueryHub(ReactiveDataQueryFacade, Logger);
            var temporalDataQueryHub = new TemporalDataQueryHub(TemporalDataQueryFacade);
            var searchHub = new SearchHub(SearchFacade);
            var securityHub = new SecurityHub(SecurityFacade);

            GlobalHost.DependencyResolver.Register(typeof(CommandsHub), () => commandHub);
            GlobalHost.DependencyResolver.Register(typeof(MetaDataHub), () => metaDataHub);
            GlobalHost.DependencyResolver.Register(typeof(ReactiveDataQueryHub), () => reactiveDataQueryHub);
            GlobalHost.DependencyResolver.Register(typeof(TemporalDataQueryHub), () => temporalDataQueryHub);
            GlobalHost.DependencyResolver.Register(typeof(SearchHub), () => searchHub);
            GlobalHost.DependencyResolver.Register(typeof(SecurityHub), () => securityHub);

            var hubConfiguration = new HubConfiguration { EnableDetailedErrors = true, EnableCrossDomain = true };

            GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule());

            app.MapHubs(hubConfiguration);
        }
    }
}

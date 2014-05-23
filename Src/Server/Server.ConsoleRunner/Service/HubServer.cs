﻿using FalconSoft.ReactiveWorksheets.Server.Bootstrapper;
using FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs;
using Microsoft.AspNet.SignalR;
using Owin;

namespace ReactiveWorksheets.Server.ConsoleRunner.Service
{
    internal class HubServer
    {
        public void Configuration(IAppBuilder app)
        {
            var commandHub = new CommandsHub(ServerApp.CommandFacade);
            var metaDataHub = new MetaDataHub(ServerApp.MetaDataFacade);
            var reactiveDataQueryHub = new ReactiveDataQueryHub(ServerApp.ReactiveDataQueryFacade, ServerApp.Logger);
            var temporalDataQueryHub = new TemporalDataQueryHub(ServerApp.TemporalQueryFacade);
            var searchHub = new SearchHub(ServerApp.SearchFacade);

            var securityHub = new SecurityHub(ServerApp.SecurityFacade);

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

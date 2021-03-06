﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Server.SignalR.Hubs;
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
        public static IPermissionSecurityFacade PermissionSecurityFacade{ get; set; }

        public void Configuration(IAppBuilder app)
        {
            var commandHub = new CommandsHub(CommandFacade, Logger);
            var metaDataHub = new MetaDataHub(MetaDataAdminFacade);
            var reactiveDataQueryHub = new ReactiveDataQueryHub(ReactiveDataQueryFacade, Logger);
            var temporalDataQueryHub = new TemporalDataQueryHub(TemporalDataQueryFacade);
            var searchHub = new SearchHub(SearchFacade);
            var securityHub = new SecurityHub(SecurityFacade);
            var permissionSecurityFacade = new PermissionSecurityHub(PermissionSecurityFacade);

            GlobalHost.DependencyResolver.Register(typeof(CommandsHub), () => commandHub);
            GlobalHost.DependencyResolver.Register(typeof(MetaDataHub), () => metaDataHub);
            GlobalHost.DependencyResolver.Register(typeof(ReactiveDataQueryHub), () => reactiveDataQueryHub);
            GlobalHost.DependencyResolver.Register(typeof(TemporalDataQueryHub), () => temporalDataQueryHub);
            GlobalHost.DependencyResolver.Register(typeof(SearchHub), () => searchHub);
            GlobalHost.DependencyResolver.Register(typeof(SecurityHub), () => securityHub);
            GlobalHost.DependencyResolver.Register(typeof(PermissionSecurityHub), ()=> permissionSecurityFacade);

            var hubConfiguration = new HubConfiguration { EnableDetailedErrors = true, EnableCrossDomain = true };

            GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule());

            app.MapHubs(hubConfiguration);
        }
    }
}

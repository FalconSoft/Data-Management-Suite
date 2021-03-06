﻿using System;
using System.ServiceModel;
using System.Web.Http;
using System.Web.Http.SelfHost;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Server.WebAPI.Controllers;
using Owin;
using Microsoft.Owin.Cors;
using Microsoft.AspNet.SignalR;
using FalconSoft.Data.Management.Server.WebAPI.Hubs;
using Microsoft.Owin.Hosting;

namespace FalconSoft.Data.Management.Server.WebAPI
{
    public static class FacadesFactory
    {
        public static IReactiveDataQueryFacade ReactiveDataQueryFacade { get; set; }
        
        public static IMetaDataAdminFacade MetaDataAdminFacade { get; set; }
        
        public static ISearchFacade SearchFacade { get; set; }
        
        public static ISecurityFacade SecurityFacade { get; set; }
        
        public static IPermissionSecurityFacade PermissionSecurityFacade { get; set; }
        
        public static ITemporalDataQueryFacade TemporalDataQueryFacade { get; set; }
        
        public static ICommandFacade CommandFacade { get; set; }
        
        public static ILogger Logger { get; set; }
        
        public static IMessageBus MessageBus { get; set; }

        public static string PushUrl { get; set; }

    }

    public class SelfHostServer : IDisposable
    {
        private HttpSelfHostServer _server;
        private IDisposable _signalRDisposable = null;

        public SelfHostServer(IReactiveDataQueryFacade reactiveDataQueryFacade,
            IMetaDataAdminFacade metaDataAdminFacade,
            ISearchFacade searchFacade,
            ISecurityFacade securityFacade,
            IPermissionSecurityFacade permissionSecurityFacade,
            ITemporalDataQueryFacade temporalDataQueryFacade,
            ICommandFacade commandFacade,
            ILogger logger,
            IMessageBus messageBus,
            string pushUrl)
        {
            FacadesFactory.ReactiveDataQueryFacade = reactiveDataQueryFacade;
            FacadesFactory.MetaDataAdminFacade = metaDataAdminFacade;
            FacadesFactory.SearchFacade = searchFacade;
            FacadesFactory.SecurityFacade = securityFacade;
            FacadesFactory.PermissionSecurityFacade = permissionSecurityFacade;
            FacadesFactory.TemporalDataQueryFacade = temporalDataQueryFacade;
            FacadesFactory.CommandFacade = commandFacade;
            FacadesFactory.Logger = logger;
            FacadesFactory.MessageBus = messageBus;
            FacadesFactory.PushUrl = pushUrl;
        }

        public void Start(string url)
        {
            var config = new HttpSelfHostConfiguration(url);
            config.TransferMode = TransferMode.StreamedRequest;
            config.MaxBufferSize = Int32.MaxValue;
            config.MaxReceivedMessageSize = Int32.MaxValue;


            config.Routes.MapHttpRoute(
                "API Default", "api/{controller}/{action}/{id}",
                new { id = RouteParameter.Optional });

            _server = new HttpSelfHostServer(config);

            _signalRDisposable = WebApp.Start<Startup>(FacadesFactory.PushUrl);

            _server.OpenAsync().Wait();
            FacadesFactory.Logger.Info("Web Api server is running ");
        }

        public void Dispose()
        {
            _signalRDisposable.Dispose();
            _server.CloseAsync().Wait();
            _server.Dispose();
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}

﻿using System;
using System.ServiceModel;
using System.Web.Http;
using System.Web.Http.SelfHost;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Server.WebAPI.Controllers;

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
    }

    public class SelfHostServer : IDisposable
    {

        private HttpSelfHostServer _server;

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
            FacadesFactory.ReactiveDataQueryFacade = reactiveDataQueryFacade;
            FacadesFactory.MetaDataAdminFacade = metaDataAdminFacade;
            FacadesFactory.SearchFacade = searchFacade;
            FacadesFactory.SecurityFacade = securityFacade;
            FacadesFactory.PermissionSecurityFacade = permissionSecurityFacade;
            FacadesFactory.TemporalDataQueryFacade = temporalDataQueryFacade;
            FacadesFactory.CommandFacade = commandFacade;
            FacadesFactory.Logger = logger;

            //var rabbitMq = new RabbitMQBroker(hostName, userName, password, virtualHost);

            //_globalBroker = new GlobalBroker(rabbitMq,
            //    ReactiveDataQueryFacade,
            //    MetaDataAdminFacade,
            //    PermissionSecurityFacade,
            //    SecurityFacade);
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

            _server.OpenAsync().Wait();

            FacadesFactory.Logger.Info("Web Api server is running ");
        }

        public void Dispose()
        {
            _server.CloseAsync().Wait();
            _server.Dispose();
        }
    }
}

using System;
using System.Configuration;
using FalconSoft.ReactiveWorksheets.Server.Bootstrapper;
using FalconSoft.ReactiveWorksheets.Server.SignalR.Hubs;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;
using Owin;

namespace ReactiveWorksheets.Server.ConsoleRunner
{
    internal class Program
    {
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => ServerApp.Logger.Error("UnhandledException -> ", (Exception)args.ExceptionObject);
            ServerApp.Logger.InfoFormat("Server...");
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Configure(ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"], ConfigurationManager.AppSettings["PersistenceDataConnectionString"], ConfigurationManager.AppSettings["MongoDataConnectionString"]);
                bootstrapper.Run();
            }
            catch (Exception ex)
            {
                ServerApp.Logger.Error("Failed to Configure and Run Bootstrapper", ex);
                throw;
            }
            using (WebApp.Start<HubServer>(ConfigurationManager.AppSettings["ConnectionString"]))
            {
                Console.WriteLine("Server is running, Press <Enter> to stop");
                Console.ReadLine();
            }
        }
    }

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

            GlobalHost.DependencyResolver.Register(typeof (CommandsHub), () => commandHub);
            GlobalHost.DependencyResolver.Register(typeof (MetaDataHub), () => metaDataHub);
            GlobalHost.DependencyResolver.Register(typeof (ReactiveDataQueryHub), () => reactiveDataQueryHub);
            GlobalHost.DependencyResolver.Register(typeof (TemporalDataQueryHub), () => temporalDataQueryHub);
            GlobalHost.DependencyResolver.Register(typeof (SearchHub), () => searchHub);
            GlobalHost.DependencyResolver.Register(typeof (SecurityHub), () => securityHub);
            
            var hubConfiguration = new HubConfiguration {EnableDetailedErrors = true};

            GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule());
            app.MapSignalR(hubConfiguration);
        }
    }

    internal class LoggingPipelineModule : HubPipelineModule
    {
        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            Console.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }
        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            Console.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }
    }
}

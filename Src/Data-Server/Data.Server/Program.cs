using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Server.RabbitMQ;
using FalconSoft.Data.Server.Installers;

namespace FalconSoft.Data.Server
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            switch (ConfigurationManager.AppSettings["serverMessagingType"])
            {
                case "SignalR": RunSignalRServer(); break;
                case "RabbitMQ": RunRabbitMQServer(); break;
            }
        }

        private static void RunRabbitMQServer()
        {
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Configure(ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"], ConfigurationManager.AppSettings["PersistenceDataConnectionString"],
                    ConfigurationManager.AppSettings["MongoDataConnectionString"], ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["CatalogDlls"]);
                ServerApp.Logger.Info("Bootstrapper configured...");
                bootstrapper.Run();
                ServerApp.Logger.Info("Bootstrapper started running...");
            }
            catch (Exception ex)
            {
                ServerApp.Logger.Error("Failed to Configure and Run Bootstrapper", ex);
                throw;
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, args) => ServerApp.Logger.Error("UnhandledException -> ", (Exception)args.ExceptionObject);
            ServerApp.Logger.Info("Server...");
            var hostName = ConfigurationManager.AppSettings["ConnectionString"];
            var userName = ConfigurationManager.AppSettings["RadditMqAdminLogin"];
            var password = ConfigurationManager.AppSettings["RadditMqAdminPass"];
            var handlers = new ManualResetEvent[7];
            handlers[0] = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                var reactiveDataQueryBroker = new ReactiveDataQueryBroker(hostName, userName, password,
                    ServerApp.ReactiveDataQueryFacade, ServerApp.Logger, handlers[0]);
            });

            handlers[1] = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
               var metaDataAdminBroker = new MetaDataBroker(hostName, userName, password, ServerApp.MetaDataFacade, ServerApp.Logger, handlers[1]);
            });

            handlers[2] = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                var commandBroker = new CommandBroker(hostName, userName, password, ServerApp.CommandFacade, ServerApp.Logger, handlers[2]);
            });

            handlers[3] = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                var sucurityBroker = new SecurityBroker(hostName, userName, password, ServerApp.SecurityFacade, ServerApp.Logger, handlers[3]);
            });

            handlers[4] = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                var permissionSecurityBroker = new PermissionSecurityBroker(hostName, userName, password, ServerApp.PermissionSecurityFacade, ServerApp.Logger, handlers[4]);

            });
            handlers[5] = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                var serchBroker = new SearchBroker(hostName, userName, password, ServerApp.SearchFacade, ServerApp.Logger, handlers[5]);
            });

            handlers[6] = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                var temporalDataQueryBroker = new TemporalDataQueryBroker(hostName, userName, password, ServerApp.TemporalQueryFacade, ServerApp.Logger, handlers[6]);
            });

            Task.Factory.StartNew(() =>
            {
                var temporalDataQueryBroker = new TemporalDataQueryBroker(hostName, userName, password, ServerApp.TemporalQueryFacade,
                    ServerApp.Logger, handlers[6]);
            });

            foreach (var manualResetEvent in handlers)
            {
                manualResetEvent.WaitOne();
            }

            Console.WriteLine("Server runs. Press 'Enter' to stop server work.");

            Console.ReadLine();
        }

        private static void RunSignalRServer()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => ServerApp.Logger.Error("UnhandledException -> ", (Exception)args.ExceptionObject);
            ServerApp.Logger.Info("Server...");

            var serverService = new ServerService();

            if (Environment.UserInteractive)
            {
                Console.WindowWidth *= 2;
                Console.WindowHeight *= 2;
                serverService.Start();
                if (Console.ReadLine() == "\n")
                    serverService.Stop();
            }
            else
            {
                var servicesToRun = new ServiceBase[]
                {
                    new ServerService()
                };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}

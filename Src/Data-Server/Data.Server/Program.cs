using System;
using System.Configuration;
using System.ServiceProcess;
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
            Task.Factory.StartNew(() =>
            {
                var reactiveDataQueryBroker = new ReactiveDataQueryBroker(hostName, userName, password, ServerApp.ReactiveDataQueryFacade, ServerApp.Logger);
            });
            Task.Factory.StartNew(() =>
            {
                var metaDataAdminBroker = new MetaDataBroker(hostName, userName, password, ServerApp.MetaDataFacade, ServerApp.Logger);
            });

            Task.Factory.StartNew(() =>
            {
                var commandBroker = new CommandBroker(hostName, userName, password, ServerApp.CommandFacade, ServerApp.Logger);
            });

            Task.Factory.StartNew(() =>
            {
                var sucurityBroker = new SecurityBroker(hostName, userName, password, ServerApp.SecurityFacade, ServerApp.Logger);
            });

            Task.Factory.StartNew(() =>
            {
                var permissionSecurityBroker = new PermissionSecurityBroker(hostName, userName, password, ServerApp.PermissionSecurityFacade, ServerApp.Logger);
            });

            Task.Factory.StartNew(() =>
            {
                var serchBroker = new SearchBroker(hostName, userName, password, ServerApp.SearchFacade, ServerApp.Logger);
            });

            Task.Factory.StartNew(() =>
            {
                var temporalDataQueryBroker = new TemporalDataQueryBroker(hostName, userName, password, ServerApp.TemporalQueryFacade, ServerApp.Logger);
            });

            Console.WriteLine("Server is running. Press 'Enter' to stop server.");
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

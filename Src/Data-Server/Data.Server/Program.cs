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

            var reactiveDataQueryBroker = new ReactiveDataQueryBroker(hostName, userName, password,
                ServerApp.ReactiveDataQueryFacade, ServerApp.Logger);
            Console.WriteLine("ReactiveDataQueryBroker starts");

            var metaDataAdminBroker = new MetaDataBroker(hostName, userName, password, ServerApp.MetaDataFacade, ServerApp.Logger);
            Console.WriteLine("MetaDataBroker starts");
           
            var commandBroker = new CommandBroker(hostName, userName, password, ServerApp.CommandFacade, ServerApp.Logger);
            Console.WriteLine("CommandBroker starts");

            var securityBroker = new SecurityBroker(hostName, userName, password, ServerApp.SecurityFacade, ServerApp.Logger);
            Console.WriteLine("SecurityBroker starts");
           
            var permissionSecurityBroker = new PermissionSecurityBroker(hostName, userName, password, ServerApp.PermissionSecurityFacade, ServerApp.Logger);
            Console.WriteLine("PermissionSecurityBroker starts");
           
            var serchBroker = new SearchBroker(hostName, userName, password, ServerApp.SearchFacade, ServerApp.Logger);
            Console.WriteLine("SearchBroker started.");
           
            var temporalDataQueryBroker = new TemporalDataQueryBroker(hostName, userName, password, ServerApp.TemporalQueryFacade, ServerApp.Logger);
            Console.WriteLine("TemporalDataQueryBroker starts");

            Console.WriteLine("Server runs. Press 'Enter' to stop server work.");

            Console.ReadLine();

            commandBroker.Dispose();
            reactiveDataQueryBroker.Dispose();
            metaDataAdminBroker.Dispose();
            securityBroker.Dispose();
            permissionSecurityBroker.Dispose();
            serchBroker.Dispose();
            temporalDataQueryBroker.Dispose();
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

using System;
using System.Configuration;
using FalconSoft.Data.Management.Server.RabbitMQ;

namespace FalconSoft.Data.Server.RabbitMQ
{
    internal class Program
    {
        public static void Main()
        {
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Configure(ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"], ConfigurationManager.AppSettings["PersistenceDataConnectionString"], ConfigurationManager.AppSettings["MongoDataConnectionString"]);
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

            const string hostName = "localhost";
            var reactiveDataQueryBroker = new ReactiveDataQueryBroker(hostName, ServerApp.ReactiveDataQueryFacade,
                ServerApp.Logger);
            Console.WriteLine("Server runs. Press 'Enter' to stop server work.");
            Console.ReadLine();
        }
    }
}

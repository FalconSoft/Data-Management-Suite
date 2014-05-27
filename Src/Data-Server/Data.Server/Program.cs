using System;
using System.Configuration;

namespace FalconSoft.Data.Server
{
    internal class Program
    {
        private static void Main(string[] arguments)
        {
            if (Environment.UserInteractive)
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, args) => ServerApp.Logger.Error("UnhandledException -> ", (Exception)args.ExceptionObject);
                ServerApp.Logger.Info("Server...");

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

                //using (WebApp.Start<HubServer>(ConfigurationManager.AppSettings["ConnectionString"]))
                //{
                //    ServerApp.Logger.InfoFormat("Server is running, on address {0} Press <Enter> to stop", ConfigurationManager.AppSettings["ConnectionString"]);
                //    Console.ReadLine();
                //}
            }
            else
            {
                //var servicesToRun = new ServiceBase[]
                //{
                //    new ServerService()
                //};
                //ServiceBase.Run(servicesToRun);
            }
        }
    }
}

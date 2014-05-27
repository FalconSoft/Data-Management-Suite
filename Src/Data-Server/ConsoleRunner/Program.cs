using System;
using System.Configuration;
using System.ServiceProcess;
using FalconSoft.ReactiveWorksheets.Server.Bootstrapper;
using Microsoft.Owin.Hosting;
using ReactiveWorksheets.Server.ConsoleRunner.Service;

namespace ReactiveWorksheets.Server.ConsoleRunner
{
    internal class Program
    {
        private static void Main(string[] arguments)
        {
            //arguments = new[] {"-u"};
            //foreach (var argument in arguments)
            //{
            //    switch (argument)
            //    {
            //        case "-istart":
            //            //Installs and starts the service "Data Management Suite Server Service"
            //            ServiceInstaller.InstallAndStart("DMSServer", "DMSServer", Path.GetFullPath(Assembly.GetEntryAssembly().Location));
            //            return;
            //        case "-u":
            //            //Removes the service
            //            ServiceInstaller.Uninstall("DMSServer");
            //            Console.WriteLine("DMSServer is Uninstalled");
            //            Console.ReadLine();
            //            return;
            //        case "-status":
            //            //Checks the status of the service
            //            Console.WriteLine("Status - {0}", ServiceInstaller.GetServiceStatus("DMSServer"));
            //            Console.ReadLine();
            //            return;
            //        case "-start":
            //            //Starts the service
            //            ServiceInstaller.StartService("DMSServer");
            //            Console.WriteLine("DMSServer is Started");
            //            Console.ReadLine();
            //            return;
            //        case "-stop":
            //            //Stops the service
            //            ServiceInstaller.StopService("DMSServer");
            //            Console.WriteLine("DMSServer is Stopped");
            //            Console.ReadLine();
            //            return;
            //        case "-isinstalled":
            //            //Check if service is installed
            //            Console.WriteLine("Is Instaled - {0}", ServiceInstaller.ServiceIsInstalled("DMSServer"));
            //            Console.ReadLine();
            //            return;
            //    }
            //}

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

                using (WebApp.Start<HubServer>(ConfigurationManager.AppSettings["ConnectionString"]))
                {
                    ServerApp.Logger.InfoFormat("Server is running, on address {0} Press <Enter> to stop", ConfigurationManager.AppSettings["ConnectionString"]);
                    Console.ReadLine();
                }
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

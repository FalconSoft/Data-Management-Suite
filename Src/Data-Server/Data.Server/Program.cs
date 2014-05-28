using System;
using System.ServiceProcess;
using FalconSoft.Data.Server.Installers;

namespace FalconSoft.Data.Server
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => ServerApp.Logger.Error("UnhandledException -> ", (Exception)args.ExceptionObject);
            ServerApp.Logger.Info("Server...");

            var serverService = new ServerService();

            if (Environment.UserInteractive)
            {
                serverService.Start();
                if (Console.ReadLine()== "\n")
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

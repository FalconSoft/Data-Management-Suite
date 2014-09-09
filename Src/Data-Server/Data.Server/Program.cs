using System;
using System.ServiceProcess;
using System.Threading;
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
                Console.WindowWidth *= 2;
                Console.WindowHeight *= 2;
                serverService.Start();
                if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    serverService.Stop();
                    Thread.Sleep(2000);
                }
            }
            else
            {
                var servicesToRun = new ServiceBase[] { new ServerService() };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}

using System;
using System.Configuration;
using System.ServiceProcess;
using FalconSoft.ReactiveWorksheets.Server.Bootstrapper;
using Microsoft.Owin.Hosting;

namespace ReactiveWorksheets.Server.ConsoleRunner.Service
{
    public class ServerService : ServiceBase
    {
        public IDisposable AppStart { get; set; }

        public ServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, arguments) => ServerApp.Logger.Error("UnhandledException -> ", (Exception)arguments.ExceptionObject);
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
            using (AppStart = WebApp.Start<HubServer>(ConfigurationManager.AppSettings["ConnectionString"]))
            {
                ServerApp.Logger.Info("Server is running, Press <Enter> to stop");
                Console.ReadLine();
            }
        }

        protected override void OnStop() { }

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = "ServerService";
        }
        #endregion
    }
}

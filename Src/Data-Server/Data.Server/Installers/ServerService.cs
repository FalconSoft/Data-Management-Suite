using System;
using System.Configuration;
using System.ServiceProcess;
using FalconSoft.Data.Management.Server.WebAPI;

namespace FalconSoft.Data.Server.Installers
{
    public class ServerService : ServiceBase
    {

        private SelfHostServer SelfHostServer { get; set; }
        public ServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            Stop();
        }

        public void Start()
        {
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Configure(
                    ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"],
                    ConfigurationManager.AppSettings["PersistenceDataConnectionString"],
                    ConfigurationManager.AppSettings["MongoDataConnectionString"],
                    ConfigurationManager.AppSettings["ConnectionString"],
                    ConfigurationManager.AppSettings["CatalogDlls"]
                    );

                ServerApp.Logger.Info("Bootstrapper configured...");
                bootstrapper.Run();
                ServerApp.Logger.Info("Bootstrapper started running...");
            }
            catch (Exception ex)
            {
                ServerApp.Logger.Error("Failed to Configure and Run Bootstrapper", ex);
                throw;
            }

            switch (ConfigurationManager.AppSettings["serverMessagingType"])
            {
                case "WebApi": RunWebApiSelfHost(); break;
            }
        }

        private void RunWebApiSelfHost()
        {
            var url = ConfigurationManager.AppSettings["WebApiConnectionString"];
            var pushUrl = ConfigurationManager.AppSettings["SignalRURL"];

            SelfHostServer = new SelfHostServer(ServerApp.ReactiveDataQueryFacade,
                ServerApp.MetaDataFacade,
                ServerApp.SearchFacade,
                ServerApp.SecurityFacade,
                ServerApp.PermissionSecurityFacade,
                ServerApp.TemporalQueryFacade,
                ServerApp.CommandFacade,
                ServerApp.Logger,
                ServerApp.MessageBus,
                pushUrl);
            
            SelfHostServer.Start(url);
        }

        public new void Stop()
        {
            switch (ConfigurationManager.AppSettings["serverMessagingType"])
            {
                case "WebApi":
                {
                    ServerApp.Logger.Info("Server stopped running...");
                    SelfHostServer.Dispose();
                    break;
                }
            }
        }

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

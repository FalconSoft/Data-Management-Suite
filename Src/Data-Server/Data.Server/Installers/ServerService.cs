using System;
using System.Configuration;
using System.ServiceProcess;
using FalconSoft.Data.Management.Server.RabbitMQ;
using FalconSoft.Data.Management.Server.SignalR;

namespace FalconSoft.Data.Server.Installers
{
    public class ServerService : ServiceBase
    {
        DataServerHost DataServerHost { get; set; }
        ReactiveDataQueryBroker ReactiveDataQueryBroker { get; set; }
        MetaDataBroker MetaDataBroker { get; set; }
        CommandBroker CommandBroker { get; set; }
        SecurityBroker SecurityBroker { get; set; }
        PermissionSecurityBroker PermissionSecurityBroker { get; set; }
        SearchBroker SearchBroker { get; set; }
        TemporalDataQueryBroker TemporalDataQueryBroker { get; set; }

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
                case "SignalR": RunSignalRServer(); break;
                case "RabbitMQ": RunRabbitMQServer(); break;
            }
        }

        private void RunSignalRServer()
        {

            DataServerHost = new DataServerHost
                            (
                                ConfigurationManager.AppSettings["ConnectionString"],
                                ServerApp.Logger,
                                ServerApp.CommandFacade,
                                ServerApp.MetaDataFacade,
                                ServerApp.ReactiveDataQueryFacade,
                                ServerApp.TemporalQueryFacade,
                                ServerApp.SearchFacade,
                                ServerApp.SecurityFacade,
                                ServerApp.PermissionSecurityFacade
                            );

            DataServerHost.StartServer();
        }

        private void RunRabbitMQServer()
        {
            ServerApp.Logger.Info("Server...");
            var hostName = ConfigurationManager.AppSettings["ConnectionString"];
            var userName = ConfigurationManager.AppSettings["RabbitMqAdminLogin"];
            var password = ConfigurationManager.AppSettings["RabbitMqAdminPass"];

            ReactiveDataQueryBroker = new ReactiveDataQueryBroker(hostName, userName, password, ServerApp.ReactiveDataQueryFacade, ServerApp.Logger);
            ServerApp.Logger.Info("ReactiveDataQueryBroker starts");

            MetaDataBroker = new MetaDataBroker(hostName, userName, password, ServerApp.MetaDataFacade, ServerApp.Logger);
            ServerApp.Logger.Info("MetaDataBroker starts");

            CommandBroker = new CommandBroker(hostName, userName, password, ServerApp.CommandFacade, ServerApp.Logger);
            ServerApp.Logger.Info("CommandBroker starts");

            SecurityBroker = new SecurityBroker(hostName, userName, password, ServerApp.SecurityFacade, ServerApp.Logger);
            ServerApp.Logger.Info("SecurityBroker starts");

            PermissionSecurityBroker = new PermissionSecurityBroker(hostName, userName, password, ServerApp.PermissionSecurityFacade, ServerApp.Logger);
            ServerApp.Logger.Info("PermissionSecurityBroker starts");

            SearchBroker = new SearchBroker(hostName, userName, password, ServerApp.SearchFacade, ServerApp.Logger);
            ServerApp.Logger.Info("SearchBroker started.");

            TemporalDataQueryBroker = new TemporalDataQueryBroker(hostName, userName, password, ServerApp.TemporalQueryFacade, ServerApp.Logger);
            ServerApp.Logger.Info("TemporalDataQueryBroker starts");

            ServerApp.Logger.Info("Server runs. Press 'Enter' to stop server work.");
        }

        public new void Stop()
        {
            ServerApp.Logger.Info("Server stopped running...");
            switch (ConfigurationManager.AppSettings["serverMessagingType"])
            {
                case "SignalR":
                    if (DataServerHost != null) DataServerHost.StopServer();
                    break;
                case "RabbitMQ":
                    ReactiveDataQueryBroker.Dispose();
                    MetaDataBroker.Dispose();
                    CommandBroker.Dispose();
                    SecurityBroker.Dispose();
                    PermissionSecurityBroker.Dispose();
                    SearchBroker.Dispose();
                    TemporalDataQueryBroker.Dispose();
                    break;
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

            if (DataServerHost != null)
            {
                DataServerHost.StopServer();
            }
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

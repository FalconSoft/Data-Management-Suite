using System.ComponentModel;
using System.Configuration.Install;

namespace FalconSoft.Data.Server.Installers
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private readonly IContainer components = null;

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
            this.ServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this._serverServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ServiceProcessInstaller
            // 
            this.ServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ServiceProcessInstaller.Password = null;
            this.ServiceProcessInstaller.Username = null;
            this.ServiceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.ServiceProcessInstaller_AfterInstall);
            // 
            // _serverServiceInstaller
            // 
            this._serverServiceInstaller.Description = "Server for FalconSoft Data Management Suite";
            this._serverServiceInstaller.DisplayName = "DMS Server Service";
            this._serverServiceInstaller.ServiceName = "DMSServerService";
            this._serverServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ServiceProcessInstaller,
            this._serverServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceInstaller _serverServiceInstaller;
        protected System.ServiceProcess.ServiceProcessInstaller ServiceProcessInstaller;

        private void ServiceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}

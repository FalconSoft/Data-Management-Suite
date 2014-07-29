using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using FalconSoft.Data.Management.Components;

namespace FalconSoft.Data.Server.RabbitMQ
{
    public class Bootstrapper
    {
        private static ICommandsAggregator _commandAggregator;

        public void Configure(string metaDataPersistenceConnectionString,
            string persistenceDataConnectionString, string mongoDataConnectionString)
        {

            ServerApp.SetConfiguration(metaDataPersistenceConnectionString,
                persistenceDataConnectionString, mongoDataConnectionString, Assembly.GetExecutingAssembly().GetName().Version.ToString(), ConfigurationManager.AppSettings["ConnectionString"], DateTime.Now);

            _commandAggregator = ServerApp.CommandAggregator;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName))
            {
                ServerApp.Logger.InfoFormat("Loaded {0}", assembly.FullName);
            }
            ServerApp.Logger.Debug("Configure");
        }

        public void Run()
        {
            _commandAggregator.LoadDataSources();
        }
    }
}

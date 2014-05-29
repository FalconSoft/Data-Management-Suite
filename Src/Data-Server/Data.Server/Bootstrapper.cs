using System;
using System.Linq;
using FalconSoft.Data.Management.Components;

namespace FalconSoft.Data.Server
{
    public class Bootstrapper
    {
        private static ICommandsAggregator _commandAggregator;

        public void Configure(string metaDataPersistenceConnectionString,
            string persistenceDataConnectionString, string mongoDataConnectionString)
        {
            ServerApp.SetConfiguration(metaDataPersistenceConnectionString, 
                persistenceDataConnectionString, mongoDataConnectionString);

            _commandAggregator = ServerApp.CommandAggregator;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x=>x.FullName))
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

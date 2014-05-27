using FalconSoft.Data.Server.Components;

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
            ServerApp.Logger.Debug("Configure");
        }

        public void Run()
        {
            _commandAggregator.LoadDataSources();
        }
    }
}

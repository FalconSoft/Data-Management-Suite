using FalconSoft.ReactiveWorksheets.Server.Core;

namespace FalconSoft.ReactiveWorksheets.Server.Bootstrapper
{
    public class Bootstrapper
    {
        private static ICommandsAggregator _commandAggregator;
        public void Configure()
        {
            _commandAggregator = ServerApp.CommandAggregator;
            ServerApp.Logger.Debug("Configure");
        }

        public void Run()
        {
            _commandAggregator.LoadDataSources();
        }
    }
}

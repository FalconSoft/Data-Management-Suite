using System;
using System.Linq;
using ReactiveWorksheets.Client.SignalR;

namespace ReactiveWorksheets.Console.Client
{
    class Program
    {
        const string ConnectionString = @"http://localhost:8081";

        static void Main(string[] args)
        {
            while (true)
            {
                System.Console.Write(">");
                string commandLine = System.Console.ReadLine();

                var commandLineParser = new CommandLineParser();

                if (commandLineParser.Parse(commandLine))
                {
                    switch (commandLineParser.Command)
                    {
                        case CommandLineParser.CommandType.get:
                            Get(commandLineParser.GetArguments);
                            break;
                            
                        case CommandLineParser.CommandType.submit:
                            Submit(commandLineParser.SubmitArguments);
                            break;
                        
                        case CommandLineParser.CommandType.subscribe:
                            Subscribe(commandLineParser.SubscribeArguments);
                            break;
                        case CommandLineParser.CommandType.help:
                            WriteHelpInfoToConsole(commandLineParser);
                            break;
                        case CommandLineParser.CommandType.exit:
                            return;
                    }
                }
                else
                {
                    System.Console.WriteLine(commandLineParser.ErrorMessage);
                }
            }

        }

        private static void WriteHelpInfoToConsole(CommandLineParser commandLineParser)
        {
            string help = commandLineParser.Help();
            System.Console.WriteLine(help);
        }

        private static void Subscribe(CommandLineParser.SubscribeParams subscribeArguments)
        {
            System.Console.WriteLine("sorry but this command doesnot support yet");
        }

        private static void Get(CommandLineParser.GetParams getArguments)
        {
            var reactiveDataQueryFacade = FacadeFactory.CreateReactiveDataQueryFacade(ConnectionString);
            
            var startTime = DateTime.Now;

            var data = reactiveDataQueryFacade.GetData(getArguments.DataSourceUrn).ToArray();
            
            TimeSpan executionSpan = DateTime.Now - startTime;

            CSVHelper.WriteRecords(data, getArguments.FileName, getArguments.Separator);
            
            System.Console.WriteLine("get records from data source {0} take {1} seconds", getArguments.DataSourceUrn, executionSpan);
            
        }

        private static void Submit(CommandLineParser.SubmitParams submitParams)
        {
            //get datasourceinfo 
            var metaDataFacade = FacadeFactory.CreateMetaDataFacade(ConnectionString);
            var dsInfo = metaDataFacade.GetDataSourceInfo(submitParams.DataSourceUrn);

            var recordsToUpdate = CSVHelper.ReadRecords(dsInfo, submitParams.UpdateFileName, submitParams.Separator);
            var recordsToDelete = CSVHelper.ReadRecordsToDelete(submitParams.DeleteFileName);
            
            var commandFacade = FacadeFactory.CreateReactiveDataCommandFacade(ConnectionString);
            commandFacade.SubmitChanges(submitParams.DataSourceUrn, "console", recordsToUpdate);
        }
    }
}

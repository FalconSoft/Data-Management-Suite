using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using FalconSoft.Data.Management.Client.SignalR;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.InProcessServer.Client;

namespace FalconSoft.ReactiveWorksheets.Console.Client
{
    class Program
    {
        private static IFacadesFactory GetFacadesFactory(string facadeType)
        {
            if (facadeType.Equals("SignalR", StringComparison.OrdinalIgnoreCase))
            {
                return new SignalRFacadesFactory(ConfigurationManager.AppSettings["ConnectionString"]);
            }
            else if (facadeType.Equals("Inprocess", StringComparison.OrdinalIgnoreCase))
            {
                // this is hardcode for testing only
                const string metaDataPersistenceConnectionString = "mongodb://localhost/rw_metadata";
                const string persistenceDataConnectionString = "mongodb://localhost/rw_data";
                const string mongoDataConnectionString = "mongodb://localhost/MongoData";
                return new InProcessServerFacadesFactory();
            }
            else
            {
                throw new ConfigurationException("Unsupported facade type - >" + facadeType);
            }
        }

        public static IFacadesFactory FacadesFactory { get; private set; }

        static void Main(string[] args)
        {
            FacadesFactory = GetFacadesFactory(ConfigurationManager.AppSettings["FacadeType"]);
            var commandLineParser = new CommandLineParser();

            while (true)
            {
                System.Console.Write(">");
                string[] commandArgs = (args == null || args.Length == 0)? System.Console.ReadLine().Split(' ') : args.ToArray();

                args = null;

                if (commandLineParser.Parse(commandArgs))
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

        private static IReactiveDataQueryFacade _reactiveDataProvider2;
        private static void Subscribe(CommandLineParser.SubscribeParams subscribeArguments)
        {
            _reactiveDataProvider2 = FacadesFactory.CreateReactiveDataQueryFacade();

            _reactiveDataProvider2.GetDataChanges(subscribeArguments.DataSourceUrn)
                        .Buffer(TimeSpan.FromMilliseconds(1000))
                        .Subscribe(s =>
                        {
                            if (s.Any())
                            {
                                foreach (var recordChangedParamse in s)
                                {
                                    CSVHelper.AppendRecords(recordChangedParamse.Select(r => r.RecordValues), subscribeArguments.FileName, subscribeArguments.Separator);
                                }
                            }
                        });

            System.Console.WriteLine(string.Format("Listening updates to [{0}] and writing to [{1}]", subscribeArguments.DataSourceUrn, subscribeArguments.FileName));
        }


        private static void Get(CommandLineParser.GetParams getArguments)
        {
            var reactiveDataQueryFacade = FacadesFactory.CreateReactiveDataQueryFacade();
            
            var startTime = DateTime.Now;

            var data = reactiveDataQueryFacade.GetData(getArguments.DataSourceUrn).ToArray();
            
            TimeSpan executionSpan = DateTime.Now - startTime;

            CSVHelper.WriteRecords(data, getArguments.FileName, getArguments.Separator);
            
            System.Console.WriteLine("Data loaded from [{0}] data source to [{1}] in {2} seconds.", getArguments.DataSourceUrn, getArguments.FileName, executionSpan);            
        }

        private static void Submit(CommandLineParser.SubmitParams submitParams)
        {
            //get datasourceinfo 
            var metaDataFacade = FacadesFactory.CreateMetaDataFacade();
            var dsInfo = metaDataFacade.GetDataSourceInfo(submitParams.DataSourceUrn);

            var sw = new Stopwatch();
            Trace.WriteLine("   Parsing start");
            
            var recordsToUpdate = CSVHelper.ReadRecords(dsInfo, submitParams.UpdateFileName, submitParams.Separator);
            var recordsToDelete = CSVHelper.ReadRecordsToDelete(submitParams.DeleteFileName);
            
            var commandFacade = FacadesFactory.CreateCommandFacade();
            commandFacade.SubmitChanges(submitParams.DataSourceUrn, "console", recordsToUpdate);
        }
    }
}

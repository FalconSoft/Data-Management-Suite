using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using FalconSoft.Data.Management.Client.SignalR;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Utils;
using Newtonsoft.Json;

namespace FalconSoft.Data.Console
{
    class Program
    {
        private static IFacadesFactory GetFacadesFactory(string facadeType)
        {
            if (facadeType.Equals("SignalR", StringComparison.OrdinalIgnoreCase))
            {
                return new SignalRFacadesFactory(ConfigurationManager.AppSettings["ConnectionString"]);
            }
            if (facadeType.Equals("Inprocess", StringComparison.OrdinalIgnoreCase))
            {
                AppDomainAssemblyTypeScanner.SetLogger(new Logger());
                foreach (var assembly in AppDomainAssemblyTypeScanner.TypesOf(typeof(IFacadesFactory), ConfigurationManager.AppSettings["FacadeFactory"]))
                {
                    IFacadesFactory factory;
                    try
                    {
                        factory = (IFacadesFactory)Activator.CreateInstance(assembly, new object[] { ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"], ConfigurationManager.AppSettings["PersistenceDataConnectionString"], ConfigurationManager.AppSettings["MongoDataConnectionString"] });
                    }
                    catch (MissingMethodException)
                    {
                        continue;
                    }
                    return factory;
                }
                throw new FileNotFoundException("Facade not Found");
            }
            throw new ConfigurationException("Unsupported facade type - >" + facadeType);
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
                        case CommandLineParser.CommandType.create:
                            Create(commandLineParser.CreateArguments);
                            break;

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

        private static void Create(CommandLineParser.CreateParams createArguments)
        {
            if (!File.Exists(createArguments.SchemaPath))
            {
                System.Console.WriteLine("Schema file doesn't exist");
                return;
            }
            string dsInfoJson;
            
            using (var reader = new StreamReader(createArguments.SchemaPath))
            {
                dsInfoJson = reader.ReadToEnd();
            }

            var datasource = JsonConvert.DeserializeObject<DataSourceInfo>(dsInfoJson);

            var metadataAdminFacade = FacadesFactory.CreateMetaDataAdminFacade();

            metadataAdminFacade.CreateDataSourceInfo(datasource, createArguments.UserName);

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

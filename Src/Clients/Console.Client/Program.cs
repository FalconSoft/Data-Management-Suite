using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Client.RabbitMQ;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Utils;
using Newtonsoft.Json;

namespace FalconSoft.Data.Console
{
    class Program
    {
        private static ILogger _logger;

        public static ILogger Logger
        {
            get { return _logger ?? (_logger = new Logger()); }
        }

        private static IFacadesFactory GetFacadesFactory(string facadeType)
        {
            if (facadeType.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
            {
                return new RabbitMqFacadesFactory(ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["RabbitMqAdminLogin"], ConfigurationManager.AppSettings["RabbitMqAdminPass"]);
            }
            if (facadeType.Equals("InProcess", StringComparison.OrdinalIgnoreCase))
            {
                AppDomainAssemblyTypeScanner.SetLogger(Logger);
                foreach (var assembly in AppDomainAssemblyTypeScanner.TypesOf(typeof(IFacadesFactory), ConfigurationManager.AppSettings["FacadeFactory"]))
                {
                    IFacadesFactory factory;
                    try
                    {
                        factory = (IFacadesFactory)Activator.CreateInstance(assembly, new object[] { ConfigurationManager.AppSettings["MetaDataPersistenceConnectionString"], 
                            ConfigurationManager.AppSettings["PersistenceDataConnectionString"], ConfigurationManager.AppSettings["MongoDataConnectionString"],
                            ConfigurationManager.AppSettings["ConnectionString"], ConfigurationManager.AppSettings["CatalogDlls"]});
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
        public static string ConsoleClientToken;

        static void Main(string[] args)
        {
            FacadesFactory = GetFacadesFactory(ConfigurationManager.AppSettings["FacadeType"]);

            ConsoleClientToken = FacadesFactory.CreateSecurityFacade().Authenticate("consoleClient", "console").Value;

            var commandLineParser = new CommandLineParser();
            var withArgs = false;
            while (true)
            {
                System.Console.Write(">");
                string[] commandArgs;
                if (args == null || args.Length == 0)
                    commandArgs = System.Console.ReadLine().Split(' ');
                else
                {
                    commandArgs = args.ToArray();
                    withArgs = true;
                }

                args = null;

                if (commandLineParser.Parse(commandArgs))
                {
                    switch (commandLineParser.Command)
                    {
                        case CommandLineParser.CommandType.Create:
                            Create(commandLineParser.CreateArguments);
                            if (withArgs) return;
                            break;
                        case CommandLineParser.CommandType.Get:
                            Get(commandLineParser.GetArguments);
                            if (withArgs) return;
                            break;
                        case CommandLineParser.CommandType.Submit:
                            Submit(commandLineParser.SubmitArguments);
                            if (withArgs) return;
                            break;
                        case CommandLineParser.CommandType.Subscribe:
                            Subscribe(commandLineParser.SubscribeArguments);
                            break;
                        case CommandLineParser.CommandType.Help:
                            WriteHelpInfoToConsole(commandLineParser);
                            break;
                        case CommandLineParser.CommandType.Exit:
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
            DataSourceInfo dataSource;
            try
            {
                dataSource = JsonConvert.DeserializeObject<DataSourceInfo>(dsInfoJson);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return;
            }
            var metadataAdminFacade = FacadesFactory.CreateMetaDataAdminFacade();
            metadataAdminFacade.CreateDataSourceInfo(dataSource, createArguments.UserName ?? "1");
            System.Console.WriteLine("Create Success " + dataSource.DataSourcePath);
        }

        private static void WriteHelpInfoToConsole(CommandLineParser commandLineParser)
        {
            var help = commandLineParser.Help();
            System.Console.WriteLine(help);
        }

        private static IReactiveDataQueryFacade _reactiveDataProvider2;

        private static void Subscribe(CommandLineParser.SubscribeParams subscribeArguments)
        {
            _reactiveDataProvider2 = FacadesFactory.CreateReactiveDataQueryFacade();
            _reactiveDataProvider2.GetDataChanges(ConsoleClientToken, subscribeArguments.DataSourceUrn)
                        .Buffer(TimeSpan.FromMilliseconds(1000))
                        .Subscribe(s =>
                        {
                            if (!s.Any()) return;
                            foreach (var recordChangedParamse in s)
                            {
                                CSVHelper.AppendRecords(recordChangedParamse.Select(r => r.RecordValues), subscribeArguments.FileName, subscribeArguments.Separator);
                            }
                        });
            System.Console.WriteLine("Listening updates to [{0}] and writing to [{1}]", subscribeArguments.DataSourceUrn, subscribeArguments.FileName);
        }


        private static void Get(CommandLineParser.GetParams getArguments)
        {
            var reactiveDataQueryFacade = FacadesFactory.CreateReactiveDataQueryFacade();
            var startTime = DateTime.Now;
            var data = reactiveDataQueryFacade.GetData(ConsoleClientToken, getArguments.DataSourceUrn);
            CSVHelper.WriteRecords(data, getArguments.FileName, getArguments.Separator);
            var executionSpan = DateTime.Now - startTime;
            System.Console.WriteLine("Data loaded from [{0}] data source to [{1}] in {2} seconds.", getArguments.DataSourceUrn, getArguments.FileName, executionSpan);
        }

        private static void Submit(CommandLineParser.SubmitParams submitParams)
        {
            //get datasourceinfo 
            var metaDataFacade = FacadesFactory.CreateMetaDataFacade();
            var dsInfo = metaDataFacade.GetDataSourceInfo(submitParams.DataSourceUrn, ConsoleClientToken);

            Trace.WriteLine("   Parsing start");

            var fileToUpdateExist = File.Exists(submitParams.UpdateFileName);
            var fileToDeleteExist = File.Exists(submitParams.DeleteFileName);

            if (!fileToDeleteExist && !fileToUpdateExist)
            {
                System.Console.WriteLine("Files not Exist");
                return;
            }

            var commandFacade = FacadesFactory.CreateCommandFacade();

            if (new FileInfo(submitParams.UpdateFileName).Length < 2000000)
            {
                var recordsToUpdate = fileToUpdateExist
                    ? CSVHelper.ReadRecords(dsInfo, submitParams.UpdateFileName, submitParams.Separator)
                    : null;
                var recordsToDelete = fileToDeleteExist
                    ? CSVHelper.ReadRecordsToDelete(submitParams.DeleteFileName)
                    : null;

                commandFacade.SubmitChanges(submitParams.DataSourceUrn, ConsoleClientToken, recordsToUpdate,
                    recordsToDelete,
                    r => System.Console.WriteLine("Submit Success"), ex => System.Console.WriteLine("Submit Failed"),
                    (key, msg) => System.Console.WriteLine(msg));
            }
            else
            {
                if (fileToDeleteExist)
                {
                    var recordsToDelete = CSVHelper.ReadRecordsToDelete(submitParams.DeleteFileName);
                    commandFacade.SubmitChanges(submitParams.DataSourceUrn, ConsoleClientToken, null, recordsToDelete,
                    r => System.Console.WriteLine("Submit Of Deleted Records Success"), ex => System.Console.WriteLine("Submit Of Deleted Records Failed"),
                    (key, msg) => System.Console.WriteLine(msg));
                }
                if (!fileToUpdateExist) return;
                foreach (var portionOfData in CSVHelper.ReadBigRecords(dsInfo, submitParams.UpdateFileName, submitParams.Separator))
                {
                    System.Console.WriteLine("Read {0} Lines from file", portionOfData.Count());
                    var are = new AutoResetEvent(false);
                    Task.Factory.StartNew(() =>
                    commandFacade.SubmitChanges(submitParams.DataSourceUrn, ConsoleClientToken, portionOfData, null,
                        r =>
                        {
                            System.Console.WriteLine("Submit Of Updated Records Success");
                            are.Set();
                        },
                        ex =>
                        {
                            System.Console.WriteLine(
                                "Submit Of Updated Records Failed, Exception Message - {0}, Inner Exception - {1}",
                                ex.Message, ex.InnerException);
                            are.Set();
                        },
                        (key, msg) => System.Console.WriteLine(msg)));
                    are.WaitOne();
                }
            }
        }
    }
}

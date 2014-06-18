using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Client.SignalR;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Utils;
using Newtonsoft.Json;

namespace FalconSoft.Data.Console.FileCrawler
{
    class Program
    {
        private static ILogger _logger;

        private static string Urn_main = @"ok\CorporateActions"; //hardcode
        private static string Urn_AGM = @"ok\Company Meeting_AGM"; //hardcode
        private static string Urn_DIV = @"ok\Dividend_DIV"; //hardcode
        private static string Urn_Sec = @"ok\SecurityRefData"; //hardcode
        private static string[] exFields = new[] { "paytype", "rdid", "priority", "defaultopt", "outturnsecid", "outturnisin", "ratioold", "rationew", "fractions", "currency", "rate1type", "rate1", "rate2type", "rate2" }; //hardcode

        public static ILogger Logger
        {
            get { return _logger ?? (_logger = new Logger()); }
        }

        private static IFacadesFactory GetFacadesFactory(string facadeType)
        {
            if (facadeType.Equals("SignalR", StringComparison.OrdinalIgnoreCase))
            {
                return new SignalRFacadesFactory(ConfigurationManager.AppSettings["ConnectionString"]);
            }
            if (facadeType.Equals("InProcess", StringComparison.OrdinalIgnoreCase))
            {
                AppDomainAssemblyTypeScanner.SetLogger(Logger);
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


        //MAIN
        static void Main(string[] args)
        {
            FacadesFactory = GetFacadesFactory(ConfigurationManager.AppSettings["FacadeType"]);
            System.Console.WriteLine("Hello!");
            var commandLineParser = new CommandLineParser();

            while (true)
            {
                System.Console.Write(">");
                var commandArgs = (args == null || args.Length == 0) ? System.Console.ReadLine().Split(' ') : args.ToArray();

                args = null;

                if (commandLineParser.Parse(commandArgs))
                {
                    switch (commandLineParser.Command)
                    {
                        case CommandLineParser.CommandType.Start:
                            if (commandLineParser.StartParameters != null)
                            {
                                StartFileWatcher(commandLineParser.StartParameters.Path,
                                    commandLineParser.StartParameters.Filter);
                                System.Console.WriteLine("Start File Watcher . . .");
                            }
                            break;
                        case CommandLineParser.CommandType.Test:
                            Test();
                            break;
                        case CommandLineParser.CommandType.Help:
                            commandLineParser.Help();
                            break;
                        case CommandLineParser.CommandType.Exit:
                            return;
                    }
                }
                else
                {
                    System.Console.WriteLine("Error");
                }
            }   
        }

        private static void StartFileWatcher(string path,string filter)
        {
            var fsw = new FileSystemWatcher {Path = path, Filter = filter};
            //fsw.NotifyFilter = NotifyFilters.LastWrite;
            fsw.Created += OnCreated;
            fsw.Deleted += OnChanged;
            fsw.EnableRaisingEvents = true;
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            //todo mb nothing :)
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            var metaDataFacade = FacadesFactory.CreateMetaDataFacade();
            var dsInfo = metaDataFacade.GetDataSourceInfo(Urn_main);
            var keyFieldName = dsInfo.GetKeyFieldsName().First();
            var rows = FileParser.ParseTxtToRows("\t", e.FullPath);
            var preparedData = PrepareData(rows, keyFieldName);
            var records = ConvertDataToStrongTyped(preparedData, Urn_main).ToArray();

            var recordsAgm = ParseInheritData(records, Urn_AGM, "AGM");
            FacadesFactory.CreateCommandFacade().SubmitChanges(Urn_AGM, "console", recordsAgm, null, (r) => System.Console.WriteLine("Submit Success -> " + Urn_AGM), (ex) => System.Console.WriteLine("Submit Failed"),
                (key, msg) => System.Console.WriteLine(msg));

            var recordsDiv = ParseInheritData(records, Urn_DIV, "DIV");
            FacadesFactory.CreateCommandFacade().SubmitChanges(Urn_DIV, "console", recordsDiv, null, (r) => System.Console.WriteLine("Submit Success -> " + Urn_DIV), (ex) => System.Console.WriteLine("Submit Failed"),
                (key, msg) => System.Console.WriteLine(msg));

            var secData = PrepareData(rows, "secid"); // hardcode
            var recordsSec = ParseDataSourceData(Urn_Sec, secData);
            FacadesFactory.CreateCommandFacade().SubmitChanges(Urn_Sec, "console", recordsSec, null, (r) => System.Console.WriteLine("Submit Success -> " + Urn_Sec), (ex) => System.Console.WriteLine("Submit Failed"),
                (key, msg) => System.Console.WriteLine(msg));
        }

        private static IEnumerable<Dictionary<string, object>> ConvertDataToStrongTyped(IEnumerable<Dictionary<string, object>> data, string urn)
        {
            var metaDataFacade = FacadesFactory.CreateMetaDataFacade();
            var dsInfo = metaDataFacade.GetDataSourceInfo(urn);
            foreach (var row in data)
            {
                foreach (var fieldInfo in dsInfo.Fields)
                {
                    row[fieldInfo.Key] = FileParser.TryConvert(row[fieldInfo.Key].ToString(),
                        dsInfo.Fields[fieldInfo.Key].DataType);
                }
                yield return row;
            }
        }
        private static IEnumerable<Dictionary<string, object>> ParseInheritData(IEnumerable<Dictionary<string, object>> data, string urn, string eventName)
        {
            var metaDataFacade = FacadesFactory.CreateMetaDataFacade();
            var dsInfo = metaDataFacade.GetDataSourceInfo(urn);
            foreach (var row in data.Where(w => w["eventcd"].ToString() == eventName))
            {
                var dict = new Dictionary<string, object>();
                foreach (var keyValuePair in row)
                {
                    if(exFields.Any(a=>a == keyValuePair.Key)) continue; // except fields
                    if (keyValuePair.Key.Contains("type"))
                    {
                        var fieldName = keyValuePair.Key.Replace("type", string.Empty);
                        if (keyValuePair.Value.ToString() == string.Empty) continue;
                        dict.Add(dsInfo.Fields.First(f=>String.Equals(f.Key, keyValuePair.Value.ToString(), StringComparison.CurrentCultureIgnoreCase)).Key, dsInfo.Fields.Keys.Contains(keyValuePair.Value.ToString()) ? FileParser.TryConvert(row[fieldName].ToString(), dsInfo.Fields[keyValuePair.Value.ToString()].DataType) : string.Empty);
                        continue;
                    }
                    if (keyValuePair.Key.Contains("field") && keyValuePair.Key.Contains("name"))
                    {
                        var fieldName = keyValuePair.Key.Replace("name", string.Empty);
                        if (keyValuePair.Value.ToString() == string.Empty) continue;
                        dict.Add(dsInfo.Fields.First(f => String.Equals(f.Key, keyValuePair.Value.ToString(), StringComparison.CurrentCultureIgnoreCase)).Key, dsInfo.Fields.Keys.Contains(keyValuePair.Value.ToString()) ? FileParser.TryConvert(row[fieldName].ToString(), dsInfo.Fields[keyValuePair.Value.ToString()].DataType) : string.Empty);
                        continue;
                    }
                    if (keyValuePair.Key.Contains("field") || keyValuePair.Key.Contains("date")) continue;
                    dict.Add(keyValuePair.Key,keyValuePair.Value);
                }
                yield return dict;
            }
        }

        private static IEnumerable<Dictionary<string, object>> ParseDataSourceData(string urn,
            IEnumerable<Dictionary<string, object>> data, string[] fields = null)
        {
            var metaDataFacade = FacadesFactory.CreateMetaDataFacade();
            var dsInfo = metaDataFacade.GetDataSourceInfo(urn);
            var dsFields = fields ?? dsInfo.Fields.Keys.ToArray();
            foreach (var row in data)
            {
                var dict = new Dictionary<string, object>();
                row.Join(dsFields, j1 => j1.Key.ToLower(), j2 => j2.ToLower(), (j1, j2) =>
                {
                    dict.Add(j2, FileParser.TryConvert(row[j2].ToString(), dsInfo.Fields[j2].DataType));
                    return j1;
                }).Count();
                yield return dict;
            }
        }

        private static IEnumerable<Dictionary<string, object>> PrepareData(IEnumerable<Dictionary<string, object>> data,string keyFieldName)
        {
            return data.GroupBy(gr => gr[keyFieldName].ToString()).Select(gr => gr.Last());  
        }

        private static void Test()
        {
            CreateSource(@"c:\data\CorporateActions_json.txt");
            CreateSource(@"c:\data\AGM_json.txt");
            CreateSource(@"c:\data\DIV_json.txt");
            CreateSource(@"c:\data\SecurityRefData_json.txt");
            StartFileWatcher("C:\\data\\", "*690.txt");
            System.Console.WriteLine("Start File Watcher . . .");
        }

        private static void CreateSource(string path)
        {
            if (!File.Exists(path))
            {
                System.Console.WriteLine("Schema file doesn't exist");
                return;
            }
            string dsInfoJson;
            using (var reader = new StreamReader(path))
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
            metadataAdminFacade.CreateDataSourceInfo(dataSource, "server");
            System.Console.WriteLine("Save Success " + dataSource.DataSourcePath);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using Newtonsoft.Json;

namespace FalconSoft.Data.Server.EDI.Feeds
{
    public class EDIProviderCatalog : IDataProvidersCatalog
    {
        private FeedWatcher _watcher;

        public EDIProviderCatalog()
        {
            _watcher = new FeedWatcher();
        }

        public IEnumerable<DataProvidersContext> GetProviders()
        {
            return CreateDataProviders(@"C:\DMS\Data-Management-Suite\Src\Data-Server\DataSources\EDI\Sources\");
        }

        public event EventHandler<DataProvidersContext> DataProviderAdded;
        public event EventHandler<StringEventArg> DataProviderRemoved;
        public DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId)
        {
            return null;
        }

        public void RemoveDataSource(string providerString)
        {
            
        }

        private DataSourceInfo ReadSource(string filePath)
        {  
            string dsInfoJson;
            using (var reader = new StreamReader(filePath)) //path
            {
                dsInfoJson = reader.ReadToEnd();
            }
            try
            {
                var dataSource = JsonConvert.DeserializeObject<DataSourceInfo>(dsInfoJson);
                return dataSource;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private IEnumerable<DataProvidersContext> CreateDataProviders(string sourcePath)
        {
            _watcher.StartFileWatcher(@"C:\data\", "*690.txt");
            var providers = new List<DataProvidersContext>();
            //if (Directory.Exists(sourcePath))
            //{
            //    string[] files = Directory.GetFiles(sourcePath);
            //    foreach (string s in files)
            //    {
            //        var fileName = Path.GetFileName(s);
            //        var ds = ReadSource(sourcePath+fileName);
            //        if (ds != null)
            //        {
            //            var provider = new DataProvidersContext()
            //            {
            //                Urn = ds.DataSourcePath,
            //                DataProvider =
            //                    new EDIDataProvider(ds, ds.Name.Substring(ds.Name.Length - 3), @"C:\data\", "*690.txt"),//hardcode Path,Symbol,Filter
            //                ProviderInfo = ds
            //            };
            //            providers.Add(provider);
            //            FeedWatcher.DataProviders.Add(provider);
            //        }
            //    }
            //}
            foreach (var source in JsonSource.Sources)
            {
                var dataSource = JsonConvert.DeserializeObject<DataSourceInfo>(source);
                var provider = new DataProvidersContext()
                        {
                            Urn = dataSource.DataSourcePath,
                            DataProvider =
                                new EDIDataProvider(dataSource, dataSource.Name.Substring(dataSource.Name.Length - 3), @"C:\data\", "*690.txt"),//hardcode Path,Symbol,Filter
                            ProviderInfo = dataSource
                        };
                providers.Add(provider);
                FeedWatcher.DataProviders.Add(provider);
            }
            return providers;
        }
    }
}

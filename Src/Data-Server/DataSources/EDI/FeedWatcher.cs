using System.Collections.Generic;
using System.IO;
using System.Linq;
using FalconSoft.Data.Management.Common;

namespace FalconSoft.Data.Server.EDI.Feeds
{
    public class FeedWatcher
    {
        public static List<DataProvidersContext> DataProviders;
        private FileSystemWatcher _fileSystemWatcher;


        public FeedWatcher()
        {
            DataProviders = new List<DataProvidersContext>();
        }
        public void StartFileWatcher(string path, string filter)
        {
            
            _fileSystemWatcher = new FileSystemWatcher { Path = path, Filter = filter };
            //fsw.NotifyFilter = NotifyFilters.LastWrite;
            _fileSystemWatcher.Created += OnCreated;
            _fileSystemWatcher.Deleted += OnChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;

        }

        readonly object _locker = new object();
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            lock (_locker)
            {
                foreach (var dataProvider in DataProviders.OrderBy(k => k.ProviderInfo.ParentDataSourcePath))
                {
                    if (!string.IsNullOrEmpty(dataProvider.ProviderInfo.ParentDataSourcePath))
                    {
                        (dataProvider.DataProvider as EDIDataProvider).SendInheritData(e.FullPath);
                    }
                    else (dataProvider.DataProvider as EDIDataProvider).SendData(e.FullPath);
                }
            }
           
        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
        }

    }
}

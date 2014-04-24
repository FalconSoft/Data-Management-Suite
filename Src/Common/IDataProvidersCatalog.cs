using System;
using System.Collections.Generic;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public class DataProvidersContext
    {
        public string Urn { get; set; }
        
        public HeaderInfo ProviderInfo { get; set; }
        
        public IBaseProvider DataProvider { get; set; }

        public IMetaDataProvider MetaDataProvider { get; set; }
    }

    public interface IDataProvidersCatalog
    {
        IEnumerable<DataProvidersContext> GetProviders();

        event EventHandler<DataProvidersContext> DataProviderAdded;

        event EventHandler<string> DataProviderRemoved;

        DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId);

        void RemoveDataSource(string providerString);
    }
}

using System;
using System.Collections.Generic;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public class DataProvidersContext : EventArgs
    {
        public string Urn { get; set; }
        
        public HeaderInfo ProviderInfo { get; set; }
        
        public IBaseProvider DataProvider { get; set; }

        public IMetaDataProvider MetaDataProvider { get; set; }
    }

    public class StringEventArg : EventArgs
    {
        public string Value { get; set; }

        public StringEventArg(string value)
        {
            Value = value;
        }
    }

    public interface IDataProvidersCatalog
    {
        IEnumerable<DataProvidersContext> GetProviders();

        event EventHandler<DataProvidersContext> DataProviderAdded;

        event EventHandler<StringEventArg> DataProviderRemoved;

        DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId);

        void RemoveDataSource(string providerString);
    }
}

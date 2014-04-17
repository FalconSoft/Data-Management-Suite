using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using ReactiveWorksheets.ExternalDataSources.ExternalSources;

namespace ReactiveWorksheets.ExternalDataSources
{
    public class ExternalProviderCatalog : IDataProvidersCatalog
    {
        public event EventHandler<DataProvidersContext> DataProviderAdded;

        public event EventHandler<string> DataProviderRemoved;

        public IEnumerable<DataProvidersContext> GetProviders()  //DEMO TEST VERSION        TODO
        {
              // QUOTESFEED
              var quoteDs = ExternalExtensions.CreateDefaultDataSource(new[] {"SecID"}, typeof (QuotesFeed));
              var quoteContext = new DataProvidersContext
                {
                    Urn = quoteDs.DataSourcePath,
                    DataProvider = new QuotesFeedDataProvider(),
                    ProviderInfo = quoteDs
                };
              // MYTESTDATA
              var testDs = ExternalExtensions.CreateDefaultDataSource(new[] { "FieldId" }, typeof(MyTestData));
              var testContext = new DataProvidersContext
              {
                  Urn = testDs.DataSourcePath,
                  DataProvider = new TestDataProvider(),
                  ProviderInfo = testDs
              };

              return new[] { quoteContext, testContext };

        }

        public DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId)
        {
            return null;
        }

        public void RemoveDataSource(string providerString)
        {
            
        }

        public ServiceSourceInfo CreateServiceSource(ServiceSourceInfo dataSource, string userId)
        {
            return null;
        }

        public void RemoveServiceSource(string providerString)
        {
            
        }
    }
}

using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Server.SampleDataSources.ExternalSources;

namespace FalconSoft.Data.Server.SampleDataSources
{
    public class ExternalProviderCatalog : IDataProvidersCatalog
    {
        public event EventHandler<DataProvidersContext> DataProviderAdded;

        public event EventHandler<StringEventArg> DataProviderRemoved;

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
              // MYTESTDATA
              var calcultor = ExternalExtensions.CreateDefaultDataSource(new[] { "In1", "In2" }, typeof(Calculator));
              var calcContext = new DataProvidersContext
              {
                  Urn = calcultor.DataSourcePath,
                  DataProvider = new CalculatorDataProvider(),
                  ProviderInfo = calcultor
              };
              // YAHOO
              var yahoo = ExternalExtensions.CreateDefaultDataSource(new[] { "Symbol" }, typeof(YahooEquityRefData));
              var yahooContext = new DataProvidersContext
              {
                  Urn = yahoo.DataSourcePath,
                  DataProvider = new YahooEquityRefDataProvider(),
                  ProviderInfo = yahoo
              };


            return new[] {quoteContext, testContext, calcContext ,yahooContext};

        }

        public DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId)
        {
            return null;
        }

        public void RemoveDataSource(string providerString)
        {
            
        }

    }
}

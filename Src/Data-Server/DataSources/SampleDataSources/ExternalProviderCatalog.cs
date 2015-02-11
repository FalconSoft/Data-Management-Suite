using System;
using System.Collections.Generic;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Server.SampleDataSources.ExternalSources;

namespace FalconSoft.Data.Server.SampleDataSources
{
    public class ExternalProviderCatalog : IDataProvidersCatalog
    {
        public Action<DataProvidersContext, string> DataProviderAdded { get; set; }
    
        public Action<string, string> DataProviderRemoved { get; set; }


        public IEnumerable<DataProvidersContext> GetProviders()  //DEMO TEST VERSION        TODO
        {
              // QUOTESFEED
              var quoteDs = ExternalExtensions.CreateDefaultDataSource(new[] {"SecID"}, typeof (QuotesFeed));
              var quoteContext = new DataProvidersContext
                {
                    Urn = quoteDs.Urn,
                    DataProvider = new QuotesFeedDataProvider(quoteDs.Urn),
                    ProviderInfo = quoteDs
                };
              // MYTESTDATA
              var testDs = ExternalExtensions.CreateDefaultDataSource(new[] { "FieldId" }, typeof(MyTestData));
              var testContext = new DataProvidersContext
              {
                  Urn = testDs.Urn,
                  DataProvider = new TestDataProvider(testDs.Urn),
                  ProviderInfo = testDs
              };
              // MYTESTDATA
              var calcultor = ExternalExtensions.CreateDefaultDataSource(new[] { "In1", "In2" }, typeof(Calculator));
              var calcContext = new DataProvidersContext
              {
                  Urn = calcultor.Urn,
                  DataProvider = new CalculatorDataProvider(),
                  ProviderInfo = calcultor
              };
              //// YAHOO
              //var yahoo = ExternalExtensions.CreateDefaultDataSource(new[] { "Symbol" }, typeof(YahooEquityRefData));
              //var yahooContext = new DataProvidersContext
              //{
              //    Urn = yahoo.Urn,
              //    DataProvider = new YahooEquityRefDataProvider(yahoo.Urn),
              //    ProviderInfo = yahoo
              //};
              //// BigData
              //var bigData = ExternalExtensions.CreateDefaultDataSource(new[] { "ID" }, typeof(BigData));
              //var bigDataContext = new DataProvidersContext
              //{
              //    Urn = bigData.Urn,
              //    DataProvider = new BigDataSource(bigData.Urn),
              //    ProviderInfo = bigData
              //};

              return new[] { quoteContext, testContext, calcContext/*, yahooContext, bigDataContext */};
        }

        public DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId)
        {
            return null;
        }

        public void RemoveDataSource(DataSourceInfo dataSource, string userId)
        {
            
        }

    }
}

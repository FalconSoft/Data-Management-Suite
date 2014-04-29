﻿using System;
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


              return new[] { quoteContext, testContext,calcContext };

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

using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.DefaultMongoDbSource
{
    public class DataProvidersCatalog : IDataProvidersCatalog
    {
        private readonly MongoDbCollections _mongoCollections;

        public DataProvidersCatalog(string connectionString)
        {
            _mongoCollections = new MongoDbCollections(connectionString);
        }

        public IEnumerable<DataProvidersContext> GetProviders()
        {
            var collectionDs = _mongoCollections.TableInfos.FindAll().ToArray();
            var listDataProviders = new List<DataProvidersContext>();

            foreach (var dataSource in collectionDs)
            {
                var dataProvider = new DataProvider(_mongoCollections, dataSource);

                var dataProviderContext = new DataProvidersContext
                    {
                        Urn = dataSource.Urn,
                        DataProvider = dataProvider,
                        ProviderInfo = dataSource,
                        MetaDataProvider = new MetaDataProvider(_mongoCollections)
                    };

                var metaDataProvider = dataProviderContext.MetaDataProvider as MetaDataProvider;
                if (metaDataProvider != null & (dataProvider is DataProvider)) metaDataProvider.OnDataSourceInfoChanged = (dataProvider as DataProvider).UpdateSourceInfo;
                listDataProviders.Add(dataProviderContext);
            }
            return listDataProviders;
        }

        public Action<DataProvidersContext, string> DataProviderAdded { get; set; }

        public Action<string, string> DataProviderRemoved { get; set; }

        public DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId)
        {
            var collection = _mongoCollections.TableInfos;
            dataSource.Id = Convert.ToString(ObjectId.GenerateNewId());
            collection.Insert(dataSource);

            IDataProvider dataProvider = new DataProvider(_mongoCollections, dataSource);

            var dataProviderContext = new DataProvidersContext
                {
                    Urn = dataSource.Urn,
                    DataProvider = dataProvider,
                    ProviderInfo = dataSource,
                    MetaDataProvider = new MetaDataProvider(_mongoCollections)
                };

            var metaDataProvider = dataProviderContext.MetaDataProvider as MetaDataProvider;
            if (metaDataProvider != null & (dataProvider is DataProvider))
                metaDataProvider.OnDataSourceInfoChanged = ((DataProvider)dataProvider).UpdateSourceInfo;

            DataProviderAdded(dataProviderContext, userId);
            return collection.FindOneAs<DataSourceInfo>(Query.EQ("Urn", dataSource.Urn));
        }

        public void RemoveDataSource(DataSourceInfo dataSource, string userId)
        {

            var dataSourceProviderString = dataSource.Urn;
            _mongoCollections.GetDataCollection(dataSource.CompanyId, dataSourceProviderString)
                           .Drop();
            _mongoCollections.GetHistoryDataCollection(dataSource.CompanyId, dataSourceProviderString)
                          .Drop();
            _mongoCollections.TableInfos
                          .Remove(Query.EQ("Urn", dataSource.Urn));
            DataProviderRemoved(dataSourceProviderString, userId);
        }

    }
}



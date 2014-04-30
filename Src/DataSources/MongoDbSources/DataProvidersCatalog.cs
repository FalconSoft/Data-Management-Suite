using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Server.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources
{
    public class DataProvidersCatalog : IDataProvidersCatalog
    {
        private const string DataSourceCollectionName = "DataSourceInfo";
        private readonly string _connectionString;
        private MongoDatabase _mongoDatabase;

        public DataProvidersCatalog(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<DataProvidersContext> GetProviders()
        {
            ConnectToDb();
            var collectionDs = _mongoDatabase.GetCollection<DataSourceInfo>(DataSourceCollectionName).FindAll();
            var listDataProviders = new List<DataProvidersContext>();

            foreach (var dataSource in collectionDs)
            {
                
                var dataProvider = new DataProvider(_connectionString, dataSource.ResolveDataSourceParents(collectionDs.ToArray()));
                var dataProviderContext = new DataProvidersContext
                    {
                        Urn = dataSource.DataSourcePath,
                        DataProvider = dataProvider,
                        ProviderInfo = dataSource.ResolveDataSourceParents(collectionDs.ToArray()),
                        MetaDataProvider = new MetaDataProvider(_connectionString, dataProvider.UpdateSourceInfo)
                        MetaDataProvider = new MetaDataProvider(_connectionString)
                    };

                var metaDataProvider = dataProviderContext.MetaDataProvider as MetaDataProvider;
                if (metaDataProvider != null)
                    metaDataProvider.OnDataSourceInfoChanged = dataProvider.UpdateSourceInfo;
                listDataProviders.Add(dataProviderContext);
            }

            return listDataProviders;
        }

        public event EventHandler<DataProvidersContext> DataProviderAdded;
        
        public event EventHandler<StringEventArg> DataProviderRemoved;

        public DataSourceInfo CreateDataSource(DataSourceInfo dataSource, string userId)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<DataSourceInfo>(DataSourceCollectionName);
            dataSource.Id = ObjectId.GenerateNewId().ToString();
            collection.Insert(dataSource);

            var dataCollectionName = dataSource.DataSourcePath.ToValidDbString() + "_Data";
            if (!_mongoDatabase.CollectionExists(dataCollectionName))
                _mongoDatabase.CreateCollection(dataCollectionName);

            var historyCollectionName = dataSource.DataSourcePath.ToValidDbString() + "_History";
            if (!_mongoDatabase.CollectionExists(historyCollectionName))
                _mongoDatabase.CreateCollection(historyCollectionName);

            var dataProvider = new DataProvider(_connectionString, dataSource);
            
            var dataProviderContext = new DataProvidersContext
                {
                    Urn = dataSource.DataSourcePath,
                    DataProvider = dataProvider,
                    ProviderInfo = dataSource,
                    MetaDataProvider = new MetaDataProvider(_connectionString, dataProvider.UpdateSourceInfo)
                    ProviderInfo = dataSource.ResolveDataSourceParents(collection.FindAll().ToArray()),
                    MetaDataProvider = new MetaDataProvider(_connectionString)
                };

            var metaDataProvider = dataProviderContext.MetaDataProvider as MetaDataProvider;
            if (metaDataProvider != null)
                metaDataProvider.OnDataSourceInfoChanged = dataProvider.UpdateSourceInfo;

            DataProviderAdded(this, dataProviderContext);
             return collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", dataSource.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", dataSource.DataSourcePath.GetCategory())));
        }

        public void RemoveDataSource(string providerString)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(providerString.ToValidDbString() + "_Data")
              .Drop();
            _mongoDatabase.GetCollection(providerString.ToValidDbString() + "_History")
              .Drop();
            _mongoDatabase.GetCollection(DataSourceCollectionName)
              .Remove(Query.And(Query.EQ("Name", providerString.GetName()),
                                Query.EQ("Category", providerString.GetCategory())));
            DataProviderRemoved(this, new StringEventArg(providerString));
        }

        private void ConnectToDb()
        {
            if (_mongoDatabase == null || _mongoDatabase.Server.State != MongoServerState.Connected)
            {
                _mongoDatabase = MongoDatabase.Create(_connectionString);
            }
        }
    }
}
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
        private const string ServiceSourceCollectionName = "ServiceSourceInfo";
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
            var collectionSs = _mongoDatabase.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName).FindAll();
            var listDataProviders = new List<DataProvidersContext>();

            foreach (var dataSource in collectionDs)
            {
                var dataProviderContext = new DataProvidersContext
                    {
                        Urn = dataSource.DataSourcePath,
                        DataProvider = new DataProvider(_connectionString) { DataSourceInfo = dataSource.ResolveDataSourceParents(collectionDs.ToArray()) },
                        ProviderInfo = dataSource.ResolveDataSourceParents(collectionDs.ToArray()),
                        MetaDataProvider = new MetaDataProvider(_connectionString)
                    };
                listDataProviders.Add(dataProviderContext);
            }
            foreach (var serviceSourceInfo in collectionSs)
            {
                var dataProviderContext = new DataProvidersContext
                    {
                        Urn = serviceSourceInfo.DataSourcePath,
                        DataProvider = new ServiceSourceDataProvider {ServiceSourceInfo = serviceSourceInfo},
                        ProviderInfo = serviceSourceInfo
                    };
                listDataProviders.Add(dataProviderContext);
            }
            return listDataProviders;
        }

        public event EventHandler<DataProvidersContext> DataProviderAdded;
        
        public event EventHandler<string> DataProviderRemoved;

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

            var dataProviderContext = new DataProvidersContext
                {
                    Urn = dataSource.DataSourcePath,
                    DataProvider = new DataProvider(_connectionString) {DataSourceInfo = dataSource.ResolveDataSourceParents(collection.FindAll().ToArray())},
                    ProviderInfo = dataSource.ResolveDataSourceParents(collection.FindAll().ToArray()),
                    MetaDataProvider = new MetaDataProvider(_connectionString)
                };
            DataProviderAdded(this, dataProviderContext);
             var ds = collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", dataSource.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", dataSource.DataSourcePath.GetCategory())));
            return ds.ResolveDataSourceParents(collection.FindAll().ToArray());
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
            DataProviderRemoved(this, providerString);
        }

        public ServiceSourceInfo CreateServiceSource(ServiceSourceInfo servicesource, string userId)
        {
            ConnectToDb();
            var collection =
                _mongoDatabase.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName);
            servicesource.Id = ObjectId.GenerateNewId()
                                       .ToString();
            collection.Save(servicesource);
            var dataProviderContext = new DataProvidersContext
                {
                    Urn = servicesource.DataSourcePath,
                    DataProvider = new DataProvider(_connectionString),
                    ProviderInfo = servicesource
                };
            DataProviderAdded(this, dataProviderContext);

            new MetaDataProvider(_connectionString).CreateServiceSourceInfo(servicesource, userId);
            return
                collection.FindOneAs<ServiceSourceInfo>(
                    Query.And(Query.EQ("Name", servicesource.DataSourcePath.GetName()),
                              Query.EQ("Category", servicesource.DataSourcePath.GetCategory())));
        }

        public void RemoveServiceSource(string providerString)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(ServiceSourceCollectionName)
              .Remove(Query.And(Query.EQ("Name", providerString.GetName()),
                                Query.EQ("Category", providerString.GetCategory())));
            DataProviderRemoved(this, providerString);
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
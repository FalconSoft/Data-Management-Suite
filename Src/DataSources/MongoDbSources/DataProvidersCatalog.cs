using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
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
        private readonly string _dbName;

        public DataProvidersCatalog(string connectionString, string dbName)
        {
            _connectionString = connectionString;
            _dbName = dbName;
        }

        public IEnumerable<DataProvidersContext> GetProviders()
        {
            var client = new MongoClient(_connectionString);
            MongoServer mongoServer = client.GetServer();
            MongoDatabase db = mongoServer.GetDatabase(_dbName);
            MongoCursor<DataSourceInfo> collectionDs = db.GetCollection<DataSourceInfo>(DataSourceCollectionName)
                                                         .FindAll();
            MongoCursor<ServiceSourceInfo> collectionSs =
                db.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName).FindAll();
            var listDataProviders = new List<DataProvidersContext>();

            foreach (DataSourceInfo dataSource in collectionDs)
            {
                var dataProviderContext = new DataProvidersContext
                    {
                        Urn = dataSource.DataSourcePath,
                        DataProvider = new DataProvider(_connectionString, _dbName) { DataSourceInfo = dataSource.ResolveDataSourceParents(collectionDs.ToArray()) },
                        ProviderInfo = dataSource.ResolveDataSourceParents(collectionDs.ToArray()),
                        MetaDataProvider = new MetaDataProvider(_connectionString, _dbName)
                    };
                listDataProviders.Add(dataProviderContext);
            }
            foreach (ServiceSourceInfo serviceSourceInfo in collectionSs)
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
            var client = new MongoClient(_connectionString);
            MongoServer mongoServer = client.GetServer();
            MongoDatabase db = mongoServer.GetDatabase(_dbName);
            MongoCollection<DataSourceInfo> collection = db.GetCollection<DataSourceInfo>(DataSourceCollectionName);
            dataSource.Id = ObjectId.GenerateNewId().ToString();
            collection.Insert(dataSource);

            string dataCollectionName = dataSource.DataSourcePath.ToValidDbString() + "_Data";
            if (!db.CollectionExists(dataCollectionName))
                db.CreateCollection(dataCollectionName);

            string historyCollectionName = dataSource.DataSourcePath.ToValidDbString() + "_History";
            if (!db.CollectionExists(historyCollectionName))
                db.CreateCollection(historyCollectionName);

            var dataProviderContext = new DataProvidersContext
                {
                    Urn = dataSource.DataSourcePath,
                    DataProvider = new DataProvider(_connectionString, _dbName) {DataSourceInfo = dataSource.ResolveDataSourceParents(collection.FindAll().ToArray())},
                    ProviderInfo = dataSource.ResolveDataSourceParents(collection.FindAll().ToArray())
                };
            DataProviderAdded(this, dataProviderContext);
             var ds = collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", dataSource.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", dataSource.DataSourcePath.GetCategory())));
            return ds.ResolveDataSourceParents(collection.FindAll().ToArray());
        }

        public void RemoveDataSource(string providerString)
        {
            var client = new MongoClient(_connectionString);
            MongoServer mongoServer = client.GetServer();
            MongoDatabase db = mongoServer.GetDatabase(_dbName);
            db.GetCollection(providerString.ToValidDbString() + "_Data")
              .Drop();
            db.GetCollection(providerString.ToValidDbString() + "_History")
              .Drop();
            db.GetCollection(DataSourceCollectionName)
              .Remove(Query.And(Query.EQ("Name", providerString.GetName()),
                                Query.EQ("Category", providerString.GetCategory())));
            DataProviderRemoved(this, providerString);
        }

        public ServiceSourceInfo CreateServiceSource(ServiceSourceInfo servicesource, string userId)
        {
            var client = new MongoClient(_connectionString);
            MongoServer mongoServer = client.GetServer();
            MongoDatabase db = mongoServer.GetDatabase(_dbName);
            MongoCollection<ServiceSourceInfo> collection =
                db.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName);
            servicesource.Id = ObjectId.GenerateNewId()
                                       .ToString();
            collection.Save(servicesource);
            var dataProviderContext = new DataProvidersContext
                {
                    Urn = servicesource.DataSourcePath,
                    DataProvider = new DataProvider(_connectionString, _dbName),
                    ProviderInfo = servicesource
                };
            DataProviderAdded(this, dataProviderContext);

            new MetaDataProvider(_connectionString, _dbName).CreateServiceSourceInfo(servicesource, userId);
            return
                collection.FindOneAs<ServiceSourceInfo>(
                    Query.And(Query.EQ("Name", servicesource.DataSourcePath.GetName()),
                              Query.EQ("Category", servicesource.DataSourcePath.GetCategory())));
        }

        public void RemoveServiceSource(string providerString)
        {
            var client = new MongoClient(_connectionString);
            MongoServer mongoServer = client.GetServer();
            MongoDatabase db = mongoServer.GetDatabase(_dbName);
            db.GetCollection(ServiceSourceCollectionName)
              .Remove(Query.And(Query.EQ("Name", providerString.GetName()),
                                Query.EQ("Category", providerString.GetCategory())));
            DataProviderRemoved(this, providerString);
        }
    }
}
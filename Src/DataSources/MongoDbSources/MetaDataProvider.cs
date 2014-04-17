﻿using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources
{
    public class MetaDataProvider : IMetaDataProvider
    {
        private const string DataSourceCollectionName = "DataSourceInfo";

        private const string ServiceSourceCollectionName = "ServiceSourceInfo";
        private readonly string _connectionString;

        private readonly string _dbName;

        private MongoDatabase _mongoDatabase;

        public MetaDataProvider(string connectionString, string dbname)
        {
            _connectionString = connectionString;
            _dbName = dbname;
        }

        public DataSourceInfo[] GetAvailableDataSources(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<DataSourceInfo>(DataSourceCollectionName)
                                 .FindAll()
                                 .ToArray();
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceProviderString)
        {
            ConnectToDb();
            var allds = _mongoDatabase.GetCollection<DataSourceInfo>(DataSourceCollectionName);
            var ds = allds.FindOne(Query.And(Query.EQ("Name", dataSourceProviderString.GetName()),
                                       Query.EQ("Category", dataSourceProviderString.GetCategory())));
            return string.IsNullOrEmpty(ds.ParentProviderString) ? ds : ds.ResolveDataSourceParents(allds.FindAll().ToArray());
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceProviderString, string userId)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<DataSourceInfo>(DataSourceCollectionName);
            var oldDs =
                collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", oldDataSourceProviderString.GetName()),
                                                               Query.EQ("Category", oldDataSourceProviderString.GetCategory())));
            if (dataSource.DataSourcePath != oldDs.DataSourcePath)
            {
                string oldCollName_Data = oldDs.DataSourcePath.ToValidDbString() + "_Data";
                string oldCollName_History = oldDs.DataSourcePath.ToValidDbString() + "_History";
                _mongoDatabase.RenameCollection(oldCollName_Data, dataSource.DataSourcePath.ToValidDbString() + "_Data");
                _mongoDatabase.RenameCollection(oldCollName_History,
                                                dataSource.DataSourcePath.ToValidDbString() + "_History");
            }
            MongoCollection<BsonDocument> dataCollection =
                _mongoDatabase.GetCollection(dataSource.DataSourcePath.ToValidDbString() + "_Data");
            MongoCollection<BsonDocument> historyCollection =
                _mongoDatabase.GetCollection(dataSource.DataSourcePath.ToValidDbString() + "_History");
            //IF NEW FIELDS ADDED  (ONLY)  
            List<string> addedfields = dataSource.Fields.Keys.Except(oldDs.Fields.Keys)
                                                 .ToList();
            foreach (string addedfield in addedfields)
            {
                dataCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
                historyCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
            }
            //IF FIELDS REMOVED  (ONLY)
            List<string> removedfields = oldDs.Fields.Keys.Except(dataSource.Fields.Keys)
                                              .ToList();
            foreach (string removedfield in removedfields)
            {
                dataCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
                historyCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
            }
            dataSource.Id = oldDs.Id;
            oldDs.Update(dataSource);
            collection.Save(oldDs);
        }

        public DataSourceInfo CreateDataSourceInfo(DataSourceInfo dataSource, string userId)
        {
            ConnectToDb();
            MongoCollection<DataSourceInfo> collection =
                _mongoDatabase.GetCollection<DataSourceInfo>(DataSourceCollectionName);
            dataSource.Id = ObjectId.GenerateNewId()
                                    .ToString();
            collection.Insert(dataSource);
            string dataCollectionName = dataSource.DataSourcePath.ToValidDbString() + "_Data";
            if (!_mongoDatabase.CollectionExists(dataCollectionName))
                _mongoDatabase.CreateCollection(dataCollectionName);

            string historyCollectionName = dataSource.DataSourcePath.ToValidDbString() + "_History";
            if (!_mongoDatabase.CollectionExists(historyCollectionName))
                _mongoDatabase.CreateCollection(historyCollectionName);

            var ds = collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", dataSource.DataSourcePath.GetName()),
                                                                  Query.EQ("Category",
                                                                           dataSource.DataSourcePath.GetCategory())));
            return ds.ResolveDataSourceParents(collection.FindAll().ToArray());
        }

        public void DeleteDataSourceInfo(string dataSourceProviderString, string userId)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(dataSourceProviderString.ToValidDbString() + "_Data")
                          .Drop();
            _mongoDatabase.GetCollection(dataSourceProviderString.ToValidDbString() + "_History")
                          .Drop();
            _mongoDatabase.GetCollection(DataSourceCollectionName)
                          .Remove(Query.And(Query.EQ("Name", dataSourceProviderString.GetName()),
                                            Query.EQ("Category", dataSourceProviderString.GetCategory())));
        }

        public ServiceSourceInfo[] GetAllServiceSourceInfos(string userId)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName)
                                 .FindAll()
                                 .ToArray();
        }

        public ServiceSourceInfo GetServiceSourceInfo(string providerstring)
        {
            ConnectToDb();
            return
                _mongoDatabase.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName)
                              .FindOne(Query.And(Query.EQ("Name", providerstring.GetName()),
                                                 Query.EQ("Category", providerstring.GetCategory())));
        }

        public ServiceSourceInfo CreateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string userId)
        {
            ConnectToDb();
            MongoCollection<ServiceSourceInfo> collection =
                _mongoDatabase.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName);
            serviceSourceInfo.Id = ObjectId.GenerateNewId()
                                           .ToString();
            collection.Insert(serviceSourceInfo);
            return
                collection.FindOneAs<ServiceSourceInfo>(
                    Query.And(Query.EQ("Name", serviceSourceInfo.DataSourcePath.GetName()),
                              Query.EQ("Category", serviceSourceInfo.DataSourcePath.GetCategory())));
        }

        public void UpdateServiceSourceInfo(ServiceSourceInfo serviceSourceInfo, string userId)
        {
            ConnectToDb();
            MongoCollection<ServiceSourceInfo> collection =
                _mongoDatabase.GetCollection<ServiceSourceInfo>(ServiceSourceCollectionName);
            collection.Save(serviceSourceInfo);
        }

        public void DeleteServiceSourceInfo(string serviceSourceProviderstring, string userId)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(DataSourceCollectionName)
                          .Remove(Query.And(Query.EQ("Name", serviceSourceProviderstring.GetName()),
                                            Query.EQ("Category", serviceSourceProviderstring.GetCategory())));
        }

        private void ConnectToDb()
        {
            if (_mongoDatabase == null || _mongoDatabase.Server.State != MongoServerState.Connected)
            {
                var client = new MongoClient(_connectionString);
                MongoServer mongoServer = client.GetServer();
                _mongoDatabase = mongoServer.GetDatabase(_dbName);
            }
        }
    }
}
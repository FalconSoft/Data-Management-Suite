using System;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.DefaultMongoDbSource
{
    public class MetaDataProvider : IMetaDataProvider
    {
        private readonly MongoDbCollections _dbCollections;

        public MetaDataProvider(MongoDbCollections dbCollections)
        {
            _dbCollections = dbCollections;
        }

        public DataSourceInfo[] GetAvailableDataSources(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            return _dbCollections.TableInfos
                                 .FindAll()
                                 .ToArray();
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceProviderString, string userId)
        {
            var collection = _dbCollections.TableInfos;
         
            var oldDs =
                collection.FindOneAs<DataSourceInfo>(Query.EQ("Urn", oldDataSourceProviderString));

            if (dataSource.Urn != oldDs.Urn)
            {
                _dbCollections.RenameDataCollection(oldDs.Urn, dataSource.Urn);
                _dbCollections.RenameHistoryDataCollection(oldDs.Urn, dataSource.Urn);
            }
            var dataCollection = _dbCollections.GetDataCollection(dataSource.CompanyId, dataSource.Urn);
            var historyCollection = _dbCollections.GetHistoryDataCollection(dataSource.CompanyId, dataSource.Urn);

            //IF NEW FIELDS ADDED  (ONLY)  
            var addedfields = dataSource.Fields.Keys.Except(oldDs.Fields.Keys)
                                                 .ToList();
            foreach (string addedfield in addedfields)
            {
                dataCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
                //historyCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
                ChooseHistoryStorageType(historyCollection,dataSource.HistoryStorageType,addedfield);
            }
            //IF FIELDS REMOVED  (ONLY)
            var removedfields = oldDs.Fields.Keys.Except(dataSource.Fields.Keys)
                                              .ToList();
            foreach (string removedfield in removedfields)
            {
                dataCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
                historyCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
            }
            dataSource.Id = oldDs.Id;
            oldDs.Update(dataSource);
            collection.Save(oldDs);

            if (OnDataSourceInfoChanged != null)
            {
                OnDataSourceInfoChanged(dataSource);    
            }            
        }

        public DataSourceInfo CreateDataSourceInfo(DataSourceInfo dataSource, string userId)
        {
            var collection = _dbCollections.TableInfos;
            dataSource.Id = ObjectId.GenerateNewId().ToString();
            
            collection.Insert(dataSource);


            var ds = collection.FindOneAs<DataSourceInfo>(Query.EQ("Urn", dataSource.Urn));

            return ds.ResolveDataSourceParents(collection.FindAll().ToArray());
        }

        public void DeleteDataSourceInfo(DataSourceInfo dsInfo, string userId)
        {
            string dataSourceProviderString = dsInfo.Urn;
            _dbCollections.GetDataCollection(dsInfo.CompanyId, dataSourceProviderString)
                          .Drop();
            _dbCollections.GetHistoryDataCollection(dsInfo.CompanyId, dataSourceProviderString)
                          .Drop();
            _dbCollections.TableInfos
                          .Remove(Query.EQ("Urn", dataSourceProviderString));
        }

        public Action<DataSourceInfo> OnDataSourceInfoChanged { get; set; }

        private void ChooseHistoryStorageType(MongoCollection<BsonDocument> collection,HistoryStorageType storageType,string field)
        {
            switch (storageType)
            {
                case HistoryStorageType.Buffer: //collection.Update(Query.Null, Update.PushEach("Data.$."+field, string.Empty), UpdateFlags.Multi);
                    break;
               case HistoryStorageType.Event: collection.Update(Query.Null, Update.Set(field, string.Empty), UpdateFlags.Multi);
                   break;
            }
            
        }

    }
}
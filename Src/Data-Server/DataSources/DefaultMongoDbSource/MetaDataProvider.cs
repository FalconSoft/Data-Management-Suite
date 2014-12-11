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
                collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", Utils.GetNamePart(oldDataSourceProviderString)),
                                                               Query.EQ("Category", Utils.GetCategoryPart(oldDataSourceProviderString))));
            if (dataSource.DataSourcePath != oldDs.DataSourcePath)
            {
                _dbCollections.RenameDataCollection(oldDs.DataSourcePath, dataSource.DataSourcePath);
                _dbCollections.RenameHistoryDataCollection(oldDs.DataSourcePath, dataSource.DataSourcePath);
            }
            var dataCollection = _dbCollections.GetDataCollection(dataSource.DataSourcePath);
            var historyCollection = _dbCollections.GetHistoryDataCollection(dataSource.DataSourcePath);

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

            var ds = collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", Utils.GetNamePart(dataSource.DataSourcePath)),
                                                                  Query.EQ("Category", Utils.GetCategoryPart(dataSource.DataSourcePath))));
            return ds.ResolveDataSourceParents(collection.FindAll().ToArray());
        }

        public void DeleteDataSourceInfo(string dataSourceProviderString, string userId)
        {
            _dbCollections.GetDataCollection(dataSourceProviderString)
                          .Drop();
            _dbCollections.GetHistoryDataCollection(dataSourceProviderString)
                          .Drop();
            _dbCollections.TableInfos
                          .Remove(Query.And(Query.EQ("Name", Utils.GetNamePart(dataSourceProviderString)),
                                            Query.EQ("Category", Utils.GetCategoryPart(dataSourceProviderString))));
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
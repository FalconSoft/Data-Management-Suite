using System;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Server.Persistence.MongoCollections;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.MetaData
{
    public class MetaDataPersistence : IMetaDataPersistence
    {
        private readonly MetaDataMongoCollections _metaMongoCollections;
        private readonly DataMongoCollections _mongoCollections;


        public MetaDataPersistence(MetaDataMongoCollections metaMongoCollections, DataMongoCollections mongoCollections)
        {
            _metaMongoCollections = metaMongoCollections;
            _mongoCollections = mongoCollections;
        }



        public void ClearAllMetaData()
        {
            _metaMongoCollections.DataSources.RemoveAll();
        }

        public DataSourceInfo[] GetAvailableDataSources(string userId)
        {
            return _metaMongoCollections.DataSources.FindAllAs<DataSourceInfo>().ToArray();
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceProviderString, string userId)
        {
            var allds = _metaMongoCollections.DataSources;
            return allds.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", dataSourceProviderString.GetName()),
                                       Query.EQ("Category", dataSourceProviderString.GetCategory())));
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceProviderString, string userId)
        {
            var collection = _metaMongoCollections.DataSources;
            var oldDs = collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", oldDataSourceProviderString.GetName()),
                                                                       Query.EQ("Category", oldDataSourceProviderString.GetCategory())));
            var childDataSources = oldDataSourceProviderString.GetChildDataSources(collection.FindAllAs<DataSourceInfo>().ToArray());
            if (dataSource.DataSourcePath != oldDs.DataSourcePath)
            {
                _mongoCollections.RenameDataCollection(oldDs.DataSourcePath.ToValidDbString(), dataSource.DataSourcePath.ToValidDbString());
                _mongoCollections.RenameHistoryDataCollection(oldDs.DataSourcePath.ToValidDbString(), dataSource.DataSourcePath.ToValidDbString());
            }

            var dataCollection = _mongoCollections.GetDataCollection(dataSource.DataSourcePath.ToValidDbString());
            var historyCollection = _mongoCollections.GetHistoryDataCollection(dataSource.DataSourcePath.ToValidDbString());
            //IF NEW FIELDS ADDED  (ONLY)  
            var addedfields = dataSource.Fields.Keys.Except(oldDs.Fields.Keys).ToList();
            foreach (var addedfield in addedfields)
            {
                dataCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
                historyCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
            }
            //IF FIELDS REMOVED  (ONLY)
            var removedfields = oldDs.Fields.Keys.Except(dataSource.Fields.Keys).ToList();
            foreach (var removedfield in removedfields)
            {
                dataCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
                historyCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
            }
            dataSource.Id = oldDs.Id;
            oldDs.Update(dataSource);
            collection.Save(oldDs);

            //WE NEED TO MODIFY ALL CHILD DATASOURCES
            foreach (var childDataSource in childDataSources)
            {
                var childDataCollection = _mongoCollections.GetDataCollection(childDataSource.DataSourcePath.ToValidDbString());
                var childHistoryCollection = _mongoCollections.GetHistoryDataCollection(childDataSource.DataSourcePath.ToValidDbString());

                foreach (var addedfield in addedfields)
                {
                    childDataCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
                    childHistoryCollection.Update(Query.Null, Update.Set(addedfield, string.Empty), UpdateFlags.Multi);
                }
                foreach (var removedfield in removedfields)
                {
                    childDataCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
                    childHistoryCollection.Update(Query.Null, Update.Unset(removedfield), UpdateFlags.Multi);
                }
                var childDs = (DataSourceInfo)childDataSource.Clone();
                foreach (var fieldKey in childDataSource.Fields.Where(x => x.Value.DataSourceProviderString == oldDataSourceProviderString))
                {
                    childDs.Fields.Remove(fieldKey.Key);
                }
                foreach (var field in oldDs.Fields.Values)
                {
                    var f = (FieldInfo)field.Clone();
                    f.IsParentField = true;
                    //childDs.Fields.Add(f.Name, f);
                    childDs.Fields[f.Name] = f;
                }
                collection.Save(childDs);
            }
        }

        public DataSourceInfo CreateDataSourceInfo(DataSourceInfo dataSource, string userId)
        {
            var collection = _metaMongoCollections.DataSources;
            dataSource.Id = Convert.ToString(ObjectId.GenerateNewId());
            collection.Insert(dataSource);
            return collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", dataSource.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", dataSource.DataSourcePath.GetCategory())));
        }

        public void DeleteDataSourceInfo(string dataSourceProviderString, string userId)
        {
            _mongoCollections.GetDataCollection(dataSourceProviderString.ToValidDbString()).Drop();
            _mongoCollections.GetHistoryDataCollection(dataSourceProviderString.ToValidDbString()).Drop();
            _metaMongoCollections.DataSources.Remove(Query.And(Query.EQ("Name", dataSourceProviderString.GetName()),
                                                                                    Query.EQ("Category", dataSourceProviderString.GetCategory())));
        }
    }
}

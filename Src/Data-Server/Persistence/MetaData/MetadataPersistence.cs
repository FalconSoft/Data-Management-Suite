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
        private readonly LiveDataMongoCollections _mongoCollections;

        private string GetCategoryPart(string dataSourceUrn)
        {
            return dataSourceUrn.Split('\\').First();
        }

        private string GetNamePart(string dataSourceUrn)
        {
            return dataSourceUrn.Split('\\').Last();
        }

        public MetaDataPersistence(MetaDataMongoCollections metaMongoCollections, LiveDataMongoCollections mongoCollections)
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
            var companyId = _metaMongoCollections.GetCompanyId(userId);
            return _metaMongoCollections.DataSources.FindAs<DataSourceInfo>(Query<DataSourceInfo>.EQ(d=>d.CompanyId,companyId)).ToArray();
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceProviderString, string userId)
        {
            var allds = _metaMongoCollections.DataSources;
            return allds.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", GetNamePart(dataSourceProviderString)),
                                       Query.EQ("Category", GetCategoryPart(dataSourceProviderString))));
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldDataSourceProviderString, string userId)
        {
            var collection = _metaMongoCollections.DataSources;
            var oldDs = collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", GetNamePart(oldDataSourceProviderString)),
                                                                       Query.EQ("Category", GetCategoryPart(oldDataSourceProviderString))));
            var childDataSources = oldDataSourceProviderString.GetChildDataSources(collection.FindAllAs<DataSourceInfo>().ToArray());
            if (dataSource.DataSourcePath != oldDs.DataSourcePath)
            {
                _mongoCollections.RenameDataCollection(oldDs.DataSourcePath, dataSource.DataSourcePath);
                _mongoCollections.RenameHistoryDataCollection(oldDs.DataSourcePath, dataSource.DataSourcePath);
            }

            var dataCollection = _mongoCollections.GetDataCollection(dataSource.DataSourcePath);
            var historyCollection = _mongoCollections.GetHistoryDataCollection(dataSource.DataSourcePath);
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
            if (string.IsNullOrWhiteSpace(dataSource.CompanyId))
            {
                dataSource.CompanyId = _metaMongoCollections.GetCompanyId(userId);
            }

            oldDs.Update(dataSource);
            collection.Save(oldDs);

            //WE NEED TO MODIFY ALL CHILD DATASOURCES
            foreach (var childDataSource in childDataSources)
            {
                var childDataCollection = _mongoCollections.GetDataCollection(childDataSource.DataSourcePath);
                var childHistoryCollection = _mongoCollections.GetHistoryDataCollection(childDataSource.DataSourcePath);

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
            if (string.IsNullOrWhiteSpace(dataSource.CompanyId))
            {
                dataSource.CompanyId = _metaMongoCollections.GetCompanyId(userId);
            }
            collection.Insert(dataSource);
            return collection.FindOneAs<DataSourceInfo>(Query.And(Query.EQ("Name", GetNamePart(dataSource.DataSourcePath)),
                                                                  Query.EQ("Category", GetCategoryPart(dataSource.DataSourcePath))));
        }

        public void DeleteDataSourceInfo(string dataSourceProviderString, string userId)
        {
            _mongoCollections.GetDataCollection(dataSourceProviderString).Drop();
            _mongoCollections.GetHistoryDataCollection(dataSourceProviderString).Drop();
            _metaMongoCollections.DataSources.Remove(Query.And(Query.EQ("Name", GetNamePart(dataSourceProviderString)),
                                                                                    Query.EQ("Category", GetCategoryPart(dataSourceProviderString))));
        }
    }
}

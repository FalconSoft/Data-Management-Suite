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

        public MetaDataPersistence(MetaDataMongoCollections metaMongoCollections, LiveDataMongoCollections mongoCollections)
        {
            _metaMongoCollections = metaMongoCollections;
            _mongoCollections = mongoCollections;
        }

        public void ClearAllMetaData()
        {
            _metaMongoCollections.DataSources.RemoveAll();
        }

        public string GetCompanyIdForUser(string userId)
        {
            return _metaMongoCollections.GetCompanyId(userId);
        }

        public DataSourceInfo[] GetAvailableDataSources(string userId)
        {
            if (userId != "serverAgent")
            {
                var companyId = _metaMongoCollections.GetCompanyId(userId);
                return
                    _metaMongoCollections.DataSources.FindAs<DataSourceInfo>(Query<DataSourceInfo>.EQ(d => d.CompanyId,
                                                                                                      companyId))
                                         .ToArray();
            }
            else // serverAgent is a special user for initializing server components
            {
                return _metaMongoCollections.DataSources.FindAllAs<DataSourceInfo>().ToArray();
            }
        }

        public DataSourceInfo GetDataSourceInfo(string dataSourceProviderString, string userId)
        {
            return _metaMongoCollections.DataSources.FindOneAs<DataSourceInfo>(Query.EQ("Urn", dataSourceProviderString));
        }

        private void ValidateDataSource(DataSourceInfo dataSource, string userId)
        {
            if (dataSource == null)
                throw new ArgumentException("DataSource can't be null", "dataSource");

            if (string.IsNullOrWhiteSpace(dataSource.Urn))
                throw new ArgumentException("DataSource Urn can't be null or whitespace", "dataSource.Urn");

            if (string.IsNullOrWhiteSpace(dataSource.CompanyId))
                dataSource.CompanyId = _metaMongoCollections.GetCompanyId(userId);

            if (dataSource.Urn.CompareTo(dataSource.CreateUrn()) != 0)
                dataSource.Urn = dataSource.CreateUrn();
        }

        public void UpdateDataSourceInfo(DataSourceInfo dataSource, string oldUrn, string userId)
        {
            ValidateDataSource(dataSource, userId);

            var collection = _metaMongoCollections.DataSources;
            var oldDs = collection.FindOneAs<DataSourceInfo>(Query.EQ("Urn", oldUrn));
            var childDataSources = oldUrn.GetChildDataSources(collection.FindAllAs<DataSourceInfo>().ToArray());
            if (dataSource.Urn != oldDs.Urn)
            {
                _mongoCollections.RenameDataCollection(oldDs.Urn, dataSource.Urn);
                _mongoCollections.RenameHistoryDataCollection(oldDs.Urn, dataSource.Urn);
            }

            var dataCollection = _mongoCollections.GetDataCollection(dataSource.Urn);
            var historyCollection = _mongoCollections.GetHistoryDataCollection(dataSource.Urn);
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
                var childDataCollection = _mongoCollections.GetDataCollection(childDataSource.Urn);
                var childHistoryCollection = _mongoCollections.GetHistoryDataCollection(childDataSource.Urn);

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
                foreach (var fieldKey in childDataSource.Fields.Where(x => x.Value.DataSourceProviderString == oldUrn))
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
            ValidateDataSource(dataSource, userId);

            var collection = _metaMongoCollections.DataSources;

            if (collection.Exists("Urn", dataSource.Urn))
            {
                throw new ArgumentException(string.Format("Data Source with Urn '{0}' already exists",dataSource.Urn));
            }

            dataSource.Id = Convert.ToString(ObjectId.GenerateNewId());
            collection.Insert(dataSource);
            return collection.FindOneAs<DataSourceInfo>(Query.EQ("Urn", dataSource.Urn));
        }

        public void DeleteDataSourceInfo(string urn, string userId)
        {
            _mongoCollections.GetDataCollection(urn).Drop();
            _mongoCollections.GetHistoryDataCollection(urn).Drop();
            _metaMongoCollections.DataSources.Remove(Query.EQ("Urn", urn));
        }
    }
}

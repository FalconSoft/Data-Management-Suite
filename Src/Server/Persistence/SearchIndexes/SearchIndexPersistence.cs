using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.Persistence.SearchIndexes
{
    public class SearchIndexPersistence : ISearchIndexPersistence
    {
        private readonly string _connectionString;
        private readonly string _dbName;
        private const string SearchCollectionName = "SearchIndexCollection";
        private const string DataSourceCollectionName = "MetaData_DataSourceInfo";
        private const string MetadbName = "rw_metadata";
        private const string WorksheetInfoCollectionName = "WorksheetInfo";

        public SearchIndexPersistence(string connectionString, string dbName)
        {
            _connectionString = connectionString;
            _dbName = dbName;
        }

        public SearchData[] Search(string searchString)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            return db.GetCollection<SearchData>(SearchCollectionName).Find(Query.And(Query.Matches("Key", searchString),
                Query.EQ("IsSearchable", true))).SetFields(Fields.Exclude("_id")).ToArray();
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            var metadb = mongoServer.GetDatabase(MetadbName);

            var dataSources = searchData.DataSourceUrn.GetChildDataSources(metadb.GetCollection<DataSourceInfo>(DataSourceCollectionName).FindAll().ToArray());
            var worksheets = metadb.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName)
                .Find(Query.And(Query.EQ("DataSourceInfo.Name", searchData.DataSourceUrn.GetName()),
                    Query.EQ("DataSourceInfo.Category", searchData.DataSourceUrn.GetCategory()))).ToList();

            foreach (var dataSourceInfo in dataSources)
            {
                worksheets.AddRange(metadb.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName)
                .Find(Query.And(Query.EQ("DataSourceInfo.Name", dataSourceInfo.Name),
                    Query.EQ("DataSourceInfo.Category", dataSourceInfo.Category))));
            }
            return worksheets.Distinct().Cast<HeaderInfo>().ToArray();
        }

        public void AddSearchData(SearchData searchData)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            db.GetCollection(SearchCollectionName).Remove(Query.And
                (Query.EQ("RecordKey", searchData.RecordKey),
                 Query.EQ("DataSourceUrn", searchData.DataSourceUrn),
                 Query.EQ("FieldName", searchData.FieldName)));
            db.GetCollection<SearchData>(SearchCollectionName).Insert(searchData);
        }

        public void RemoveSearchData(string recordKey, string dataSourceUrn)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            db.GetCollection(SearchCollectionName).Remove(Query.And
                (Query.EQ("RecordKey", recordKey), Query.EQ("DataSourceUrn", dataSourceUrn)));
        }

        public void UpdateUrn(string oldUrn, string newUrn)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            db.GetCollection(SearchCollectionName).Update(Query.EQ("DataSourceUrn", oldUrn), Update.Set("DataSourceUrn", newUrn), UpdateFlags.Multi);
        }

        public void UpdateIsSearchableProperty(string dataSourceUrn, string fieldName, bool value)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            var query = Query.And(Query.EQ("DataSourceUrn", dataSourceUrn), Query.EQ("FieldName", fieldName));
            var update = Update.Set("IsSearchable", value);
            db.GetCollection(SearchCollectionName).Update(query, update, UpdateFlags.Multi);
        }

        public void RemoveFields(string dataSourceUrn, string fieldName)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            db.GetCollection(SearchCollectionName).Remove(Query.And
                (Query.EQ("FieldName", fieldName), Query.EQ("DataSourceUrn", dataSourceUrn)));
        }
    }
}

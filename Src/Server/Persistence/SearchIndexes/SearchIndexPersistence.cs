using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.Persistence.SearchIndexes
{
    public class SearchIndexPersistence : ISearchIndexPersistence
    {
        private readonly string _dataConnectionString;
        private readonly string _metadataConnectionString;
        private const string SearchCollectionName = "SearchIndexCollection";
        private const string DataSourceCollectionName = "MetaData_DataSourceInfo";
        private const string WorksheetInfoCollectionName = "WorksheetInfo";
        private MongoDatabase _mongoDatabase;

        public SearchIndexPersistence(string dataConnectionString, string metadataConnectionString)
        {
            _dataConnectionString = dataConnectionString;
            _metadataConnectionString = metadataConnectionString;
        }

        private void ConnectToDb()
        {
            if (_mongoDatabase == null || _mongoDatabase.Server.State != MongoServerState.Connected)
            {
                _mongoDatabase = MongoDatabase.Create(_dataConnectionString);
            }
        }

        public SearchData[] Search(string searchString)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<SearchData>(SearchCollectionName).Find(Query.And(Query.Matches("Key", searchString),
                Query.EQ("IsSearchable", true))).SetFields(Fields.Exclude("_id")).ToArray();
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            ConnectToDb();
            var metadb = MongoDatabase.Create(_metadataConnectionString);

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
            ConnectToDb();
            _mongoDatabase.GetCollection(SearchCollectionName).Remove(Query.And
                (Query.EQ("RecordKey", searchData.RecordKey),
                 Query.EQ("DataSourceUrn", searchData.DataSourceUrn),
                 Query.EQ("FieldName", searchData.FieldName)));
            _mongoDatabase.GetCollection<SearchData>(SearchCollectionName).Insert(searchData);
        }

        public void RemoveSearchData(string recordKey, string dataSourceUrn)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(SearchCollectionName).Remove(Query.And
                (Query.EQ("RecordKey", recordKey), Query.EQ("DataSourceUrn", dataSourceUrn)));
        }

        public void UpdateUrn(string oldUrn, string newUrn)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(SearchCollectionName).Update(Query.EQ("DataSourceUrn", oldUrn), Update.Set("DataSourceUrn", newUrn), UpdateFlags.Multi);
        }

        public void UpdateIsSearchableProperty(string dataSourceUrn, string fieldName, bool value)
        {
            ConnectToDb();
            var query = Query.And(Query.EQ("DataSourceUrn", dataSourceUrn), Query.EQ("FieldName", fieldName));
            var update = Update.Set("IsSearchable", value);
            _mongoDatabase.GetCollection(SearchCollectionName).Update(query, update, UpdateFlags.Multi);
        }

        public void RemoveFields(string dataSourceUrn, string fieldName)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(SearchCollectionName).Remove(Query.And
                (Query.EQ("FieldName", fieldName), Query.EQ("DataSourceUrn", dataSourceUrn)));
        }
    }
}

using System.Linq;
using System.Text.RegularExpressions;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.SearchIndexes
{
    public class SearchIndexPersistence : ISearchIndexPersistence
    {
        private readonly string _metadataConnectionString;
        private const string SearchCollectionName = "SearchIndexCollection";
        private const string WorksheetInfoCollectionName = "WorksheetInfo";
        private readonly MongoCollection<BsonDocument> _collection;

        public SearchIndexPersistence(string dataConnectionString, string metadataConnectionString)
        {
            _metadataConnectionString = metadataConnectionString;
            var db = MongoDatabase.Create(dataConnectionString);
            if (db.CollectionExists(SearchCollectionName))
            {
                _collection = db.GetCollection(SearchCollectionName);
            }
            else
            {
                db.CreateCollection(SearchCollectionName);
                _collection = db.GetCollection(SearchCollectionName);
                var keys = new IndexKeysBuilder();
                keys.Ascending("RecordKey", "DataSourceUrn", "FieldName");
                var options = new IndexOptionsBuilder();
                options.SetSparse(true);
                options.SetUnique(false);
                _collection.EnsureIndex(keys, options);
            }
        }

        public SearchData[] Search(string searchString)
        {
            return _collection.FindAs<SearchData>(Query.Matches("Key", new BsonRegularExpression(new Regex(searchString, RegexOptions.IgnoreCase)))).SetFields(Fields.Exclude("_id")).ToArray();
        }

        public HeaderInfo[] GetSearchableWorksheets(SearchData searchData)
        {
            var metadb = MongoDatabase.Create(_metadataConnectionString);
            return metadb.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName)
                         .Find(Query.EQ("DataSourceInfoPath", searchData.DataSourceUrn))
                         .Cast<HeaderInfo>()
                         .ToArray();
        }

        public void AddSearchData(SearchData searchData)
        {
            var query = Query.And(Query<SearchData>.EQ(x => x.RecordKey, searchData.RecordKey),
                                    Query<SearchData>.EQ(x => x.DataSourceUrn, searchData.DataSourceUrn),
                                    Query<SearchData>.EQ(x => x.FieldName, searchData.FieldName));
            _collection.FindAndModify(query, SortBy<SearchData>.Ascending(x => x.DataSourceUrn), GenerateUpdate(searchData), true, true);
        }

        public void RemoveSearchData(string recordKey, string dataSourceUrn)
        {
            _collection.Remove(Query.And(Query.EQ("RecordKey", recordKey), Query.EQ("DataSourceUrn", dataSourceUrn)));
        }

        public void RemoveDataSource(string dataSourceUrn)
        {
            _collection.Remove(Query.EQ("DataSourceUrn", dataSourceUrn));
        }

        public void UpdateUrn(string oldUrn, string newUrn)
        {
            _collection.Update(Query.EQ("DataSourceUrn", oldUrn), Update.Set("DataSourceUrn", newUrn), UpdateFlags.Multi);
        }

        public void RemoveFields(string dataSourceUrn, string fieldName)
        {
            _collection.Remove(Query.And(Query.EQ("FieldName", fieldName), Query.EQ("DataSourceUrn", dataSourceUrn)));
        }

        private static IMongoUpdate GenerateUpdate(SearchData searchData)
        {
            return Update<SearchData>.Combine(searchData.GetType().GetProperties().Select(x => Update.Set(x.Name, x.GetValue(searchData, null).ToString())));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FalconSoft.Data.Server.Persistence.MongoCollections
{
    public class DataMongoCollections : BaseMongoCollections
    {
        private const string DataSuffixFormat = "{0}_Data";
        private const string HistoryDataSuffixFormat = "{0}_History";
        private const string RevisionsCollectionName = "Revisions";
        private const string TagInfosCollectionName = "TagInfos";

        public DataMongoCollections(string connectionString) : base(connectionString)
        {

        }

        public MongoCollection<BsonDocument> Revisions
        {
            get { return GetMongoDatabase().GetCollection(RevisionsCollectionName); }
        }

        public MongoCollection<TagInfo> TagInfos
        {
            get { return GetMongoDatabase().GetCollection<TagInfo>(TagInfosCollectionName); }
        }

        public MongoCollection<BsonDocument> GetDataCollection(string dataSourceName)
        {
            return GetMongoDatabase().GetCollection(string.Format(DataSuffixFormat, dataSourceName));
        }

        public MongoCollection<BsonDocument> GetHistoryDataCollection(string dataSourceName)
        {
            return GetMongoDatabase().GetCollection(string.Format(HistoryDataSuffixFormat, dataSourceName));
        }

        public void RenameHistoryDataCollection(string oldDataSourceName, string newDataSourceName)
        {
            GetMongoDatabase().RenameCollection(string.Format(HistoryDataSuffixFormat, oldDataSourceName), string.Format(HistoryDataSuffixFormat, newDataSourceName));
        }

        public void RenameDataCollection(string oldDataSourceName, string newDataSourceName)
        {
            GetMongoDatabase().RenameCollection(string.Format(DataSuffixFormat, oldDataSourceName), string.Format(DataSuffixFormat, newDataSourceName));
        }

    }
}

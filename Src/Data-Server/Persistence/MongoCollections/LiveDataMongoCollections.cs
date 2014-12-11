using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FalconSoft.Data.Server.Persistence.MongoCollections
{
    public class LiveDataMongoCollections : BaseMongoCollections
    {
        private const string DataSuffixFormat = "{0}_LiveData";
        private const string ErrorDataSuffixFormat = "{0}_Errors";
        private const string HistoryDataSuffixFormat = "{0}_LiveHistory";
        private const string RevisionsCollectionName = "Revisions";
        private const string TagInfosCollectionName = "TagInfos";

        public LiveDataMongoCollections(string connectionString) : base(connectionString)
        {

        }

        private string EscapeDataPath(string dataSourceFullPath)
        {
            return dataSourceFullPath.Replace("\\", "_");
        }

        public MongoCollection<BsonDocument> Revisions
        {
            get { return GetMongoDatabase().GetCollection(RevisionsCollectionName); }
        }

        public MongoCollection<TagInfo> TagInfos
        {
            get { return GetMongoDatabase().GetCollection<TagInfo>(TagInfosCollectionName); }
        }

        public MongoCollection<ErrorDataObject> GetErrorDataCollection(string dataSourceFullPath)
        {
            return GetMongoDatabase().GetCollection<ErrorDataObject>(string.Format(ErrorDataSuffixFormat, EscapeDataPath(dataSourceFullPath)));
        }

        public MongoCollection<BsonDocument> GetDataCollection(string dataSourceFullPath)
        {
            return GetMongoDatabase().GetCollection(string.Format(DataSuffixFormat, EscapeDataPath(dataSourceFullPath)));
        }

        public MongoCollection<BsonDocument> GetHistoryDataCollection(string dataSourceFullPath)
        {
            return GetMongoDatabase().GetCollection(string.Format(HistoryDataSuffixFormat, EscapeDataPath(dataSourceFullPath)));
        }

        public void RenameHistoryDataCollection(string olddataSourceFullPath, string newdataSourceFullPath)
        {
            GetMongoDatabase().RenameCollection(string.Format(HistoryDataSuffixFormat, EscapeDataPath(olddataSourceFullPath)), string.Format(HistoryDataSuffixFormat, EscapeDataPath(newdataSourceFullPath)));
        }

        public void RenameDataCollection(string olddataSourceFullPath, string newdataSourceFullPath)
        {
            GetMongoDatabase().RenameCollection(string.Format(DataSuffixFormat, EscapeDataPath(olddataSourceFullPath)), string.Format(DataSuffixFormat, EscapeDataPath(newdataSourceFullPath)));
        }

    }
}

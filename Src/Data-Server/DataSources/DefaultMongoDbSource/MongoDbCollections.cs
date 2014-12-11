using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FalconSoft.Data.Server.DefaultMongoDbSource
{
    public class MongoDbCollections
    {
        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly Lazy<MongoClient> _mongoClient;
        private readonly Lazy<MongoDatabase> _database;
        private const string DataSuffixFormat = "{0}_Data";
        private const string HistoryDataSuffixFormat = "{0}_History";
        private const string TableInfo = "TableInfos";

        public MongoDbCollections(string connectionString)
        {
            connectionString = connectionString.TrimEnd('/');
            int lastIndex = connectionString.LastIndexOf("/", StringComparison.Ordinal);
            _connectionString = connectionString.Substring(0, lastIndex);
            _databaseName = connectionString.Substring(lastIndex + 1);
            _mongoClient = new Lazy<MongoClient>(() => new MongoClient(_connectionString));
            _database = new Lazy<MongoDatabase>(() => _mongoClient.Value.GetServer().GetDatabase(_databaseName));
        }

        protected MongoDatabase GetMongoDatabase()
        {
            if (_database.Value.Server.State != MongoServerState.Connected)
            {
                _database.Value.Server.Reconnect();
            }
            return _database.Value;
        }

        private string EscapeDataPath(string dataSourceFullPath)
        {
            return dataSourceFullPath.Replace("\\", "_");
        }

        public MongoCollection<DataSourceInfo> TableInfos
        {
            get { return GetMongoDatabase().GetCollection<DataSourceInfo>(TableInfo); }
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

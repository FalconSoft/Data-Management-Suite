using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Security;
using MongoDB.Driver;

namespace FalconSoft.Data.Server.Persistence.MongoCollections
{
    public class BaseMongoCollections
    {
        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly Lazy<MongoClient> _mongoClient;
        private readonly Lazy<MongoDatabase> _database;

        public BaseMongoCollections(string connectionString)
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

        public bool EnsureCollectionExists(string collectionName)
        {
            if (!GetMongoDatabase().CollectionExists(collectionName))
            {
                GetMongoDatabase().CreateCollection(collectionName);
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}

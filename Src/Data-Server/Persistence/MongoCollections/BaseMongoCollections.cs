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
        public BaseMongoCollections(string connectionString)
        {
            _connectionString = connectionString;
            _databaseName = connectionString.Substring(connectionString.LastIndexOf("/"));
        }

        private MongoClient _mongoClient;
        private MongoDatabase _database;

        protected MongoDatabase GetMongoDatabase()
        {
            if (_mongoClient != null)
                _mongoClient = new MongoClient(_connectionString);

            // reconnect if needed

            if (_database == null)
            {
                _database = _mongoClient.GetServer().GetDatabase(_databaseName);
            }

            return _database;
        }
    }
}

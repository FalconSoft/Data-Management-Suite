using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common.Security;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.Persistence.Security
{
    public class SecurityPersistence : ISecurityPersistence
    {

        private readonly string _connectionString;
        private readonly string _dbName;
        private const string UsersCollectionName = "Users";

        public SecurityPersistence(string connectionString, string dbName)
        {
            _connectionString = connectionString;
            _dbName = dbName;
        }

        public List<User> GetUsers()
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            return db.GetCollection<User>(UsersCollectionName).FindAll().ToList();
        }

        public void SaveNewUser(User user)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            user.Id = ObjectId.GenerateNewId().ToString();
            db.GetCollection<User>(UsersCollectionName).Insert(user);
        }

        public void UpdateUser(User user)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            db.GetCollection<User>(UsersCollectionName).Update(Query<User>.EQ(u=>u.Id,user.Id),Update<User>.Set(u=>u.LoginName,user.LoginName)
                                                                                                           .Set(u => u.FirstName, user.FirstName)
                                                                                                           .Set(u => u.LastName, user.LastName));
        }

        public void RemoveUser(User user)
        {
            var client = new MongoClient(_connectionString);
            var mongoServer = client.GetServer();
            var db = mongoServer.GetDatabase(_dbName);
            db.GetCollection<User>(UsersCollectionName).Remove(Query.EQ("Id", new BsonString(user.Id)));
        }
    }
}

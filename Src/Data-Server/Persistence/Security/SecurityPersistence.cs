using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Security;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.Security
{
    public class SecurityPersistence : ISecurityPersistence
    {
        private readonly string _connectionString;
        private const string UsersCollectionName = "Users";
        private MongoDatabase _mongoDatabase;

        public SecurityPersistence(string connectionString)
        {
            _connectionString = connectionString;
        }

        private void ConnectToDb()
        {
            if (_mongoDatabase == null || _mongoDatabase.Server.State != MongoServerState.Connected)
            {
                _mongoDatabase = MongoDatabase.Create(_connectionString);
            }
        }

        public KeyValuePair<bool,string> Authenticate(string login, string password)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<User>(UsersCollectionName);
            var user = collection.FindOneAs<User>(Query<User>.EQ(u => u.LoginName, login));
            if (user == null) return new KeyValuePair<bool, string>(false, "User Login is Incorrect");
            return user.Password.Equals(password) 
                ? new KeyValuePair<bool, string>(true,user.Id) 
                : new KeyValuePair<bool, string>(false, "Password is incorrect");
        }

        public User GetUser(string login)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<User>(UsersCollectionName);
            return collection.FindOneAs<User>(Query<User>.EQ(u => u.LoginName, login));
        }

        public User GetUserByToken(string token)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<User>(UsersCollectionName);
            return collection.FindOneAs<User>(Query<User>.EQ(u => u.Id, token));
        }

        public List<User> GetUsers(string userToken)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<User>(UsersCollectionName).FindAll().ToList();
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            ConnectToDb();
            user.Id = ObjectId.GenerateNewId().ToString();
           _mongoDatabase.GetCollection<User>(UsersCollectionName).Insert(user);
            return user.Id;
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            ConnectToDb();
            var query = Update<User>.Set(u => u.LoginName, user.LoginName)
                .Set(u => u.FirstName, user.FirstName)
                .Set(u => u.LastName, user.LastName)
                .Set(u => u.Password, user.Password)
                .Set(u => u.EMail, user.EMail)
                .Set(u => u.Department, user.Department)
                .Set(u => u.CorporateTitle, user.CorporateTitle)
                .Set(u => u.UserGroupId, user.UserGroupId);

            _mongoDatabase.GetCollection<User>(UsersCollectionName)
                .Update(Query<User>.EQ(u => u.Id, user.Id), query);
        }

        public void RemoveUser(User user, string userToken)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection<User>(UsersCollectionName).Remove(Query.EQ("Id", new BsonString(user.Id)));
        }
    }
}

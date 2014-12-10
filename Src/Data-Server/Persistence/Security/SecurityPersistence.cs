using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Security;
using FalconSoft.Data.Server.Persistence.MongoCollections;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.Security
{
    public class SecurityPersistence : ISecurityPersistence
    {
        private readonly MetaDataMongoCollections _metaDbMongoCollections;

        public SecurityPersistence(MetaDataMongoCollections metaDbMongoCollections)
        {
            _metaDbMongoCollections = metaDbMongoCollections;
        }


        public KeyValuePair<bool,string> Authenticate(string login, string password)
        {
            var user = _metaDbMongoCollections.Users.FindOneAs<User>(Query<User>.EQ(u => u.LoginName, login));
            if (user == null) return new KeyValuePair<bool, string>(false, "User Login is Incorrect");
            return user.Password.Equals(password) 
                ? new KeyValuePair<bool, string>(true,user.Id) 
                : new KeyValuePair<bool, string>(false, "Password is incorrect");
        }

        public User GetUser(string login)
        {
            return _metaDbMongoCollections.Users.FindOneAs<User>(Query<User>.EQ(u => u.LoginName, login));
        }

        public User GetUserByToken(string token)
        {
            return _metaDbMongoCollections.Users.FindOneAs<User>(Query<User>.EQ(u => u.Id, token));
        }

        public List<User> GetUsers(string userToken)
        {
            return _metaDbMongoCollections.Users.FindAllAs<User>().ToList();
        }

        public string SaveNewUser(User user, UserRole userRole, string userToken)
        {
            _metaDbMongoCollections.Users.Insert(user);
            return user.Id;
        }

        public void UpdateUser(User user, UserRole userRole, string userToken)
        {
            var query = Update<User>.Set(u => u.LoginName, user.LoginName)
                .Set(u => u.FirstName, user.FirstName)
                .Set(u => u.LastName, user.LastName)
                .Set(u => u.Password, user.Password)
                .Set(u => u.EMail, user.EMail)
                .Set(u => u.Department, user.Department)
                .Set(u => u.CorporateTitle, user.CorporateTitle)
                .Set(u => u.UserGroupId, user.UserGroupId);

            _metaDbMongoCollections.Users
                .Update(Query<User>.EQ(u => u.Id, user.Id), query);
        }

        public void RemoveUser(User user, string userToken)
        {
            _metaDbMongoCollections.Users.Remove(Query.EQ("Id", new BsonString(user.Id)));
        }
    }
}

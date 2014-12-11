using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using FalconSoft.Data.Server.Persistence.MongoCollections;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.Data.Server.Persistence.Security
{
    public class SecurityPersistence : ISecurityPersistence
    {
        private readonly ILogger _logger;
        private readonly MetaDataMongoCollections _metaDbMongoCollections;

        public SecurityPersistence(MetaDataMongoCollections metaDbMongoCollections, ILogger logger)
        {
            _logger = logger;
            _metaDbMongoCollections = metaDbMongoCollections;
        }

        private void CreatePowerAdmin(string companyId, string userName, string password)
        {
            var id = ObjectId.GenerateNewId().ToString();
            _metaDbMongoCollections.Users.Insert(new User
            {
                Id = id,
                LoginName = userName,
                Password = password,
                CompanyId = companyId
            });

            _metaDbMongoCollections.Permissions.Insert(new Permission
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = id,
                UserRole = UserRole.Administrator,
                DataSourceAccessPermissions = new Dictionary<string, DataSourceAccessPermission>()
            });
        }

        public AuthenticationResult Authenticate(string companyId, string login, string password)
        {
            var result = new AuthenticationResult{ Status = AuthenticationStatus.Success };
            var company = _metaDbMongoCollections.Companies.FindOneAs<CompanyInfo>(Query<CompanyInfo>.EQ(c => c.CompanyId, companyId));
            if (company == null) // creates new company and adds default power user
            {
                _metaDbMongoCollections.Companies.Insert(new CompanyInfo
                    {
                        Id = ObjectId.GenerateNewId().ToString(), 
                        CompanyId = companyId, 
                        FullCompanyName = companyId,
                        Subscribtion = SubscribtionTypes.FreeTrial,
                        CreatedAt = DateTime.Now
                    });                
                CreatePowerAdmin(companyId, login, password);
                _logger.InfoFormat("=> New company '{0}' was created and new user '{1}' was added", companyId, login);
                result.Status = AuthenticationStatus.SuccessFirstTimeUser;
            }

            var user = _metaDbMongoCollections.Users.FindOneAs<User>(
                Query.And(
                    Query<User>.EQ(u => u.CompanyId, companyId),
                    Query<User>.EQ(u => u.LoginName, login)));

            if (user == null)
            {
                result.Status = AuthenticationStatus.IncorrectLogin;
                _logger.InfoFormat("Incorrect Login {0}\\{1}", companyId, login);
            }
            else if(user.Password != password) // do encryption here
            {
                _logger.InfoFormat("Wrong password: {0}\\{1}", companyId, login);
                result.Status = AuthenticationStatus.WrongPassword;
            }
            else
            {
                result.User = user;
            }

            return result;
        }

        public User GetUser(string login)
        {
            return _metaDbMongoCollections.Users.FindOneAs<User>(Query<User>.EQ(u => u.LoginName, login));
        }

        public User GetUserByToken(string token)
        {
            return _metaDbMongoCollections.Users.FindOneAs<User>(Query<User>.EQ(u => u.Id, token));
        }

        public User[] GetUsers(string userId)
        {
            var companyId = _metaDbMongoCollections.GetCompanyId(userId);
            return _metaDbMongoCollections.Users.FindAs<User>(Query<User>.EQ(d => d.CompanyId, companyId)).ToArray();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Security;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace FalconSoft.Data.Server.Persistence.Security
{
    public class PermissionSecurityPersistance : IPermissionSecurityPersistance
    {
        private readonly string _connectionString;
        private const string PermissionsCollectionName = "Permissions";
        private MongoDatabase _mongoDatabase;

        public PermissionSecurityPersistance(string connectionString)
        {
            _connectionString = connectionString;
            ConnectToDb();
            if (!_mongoDatabase.CollectionExists(PermissionsCollectionName))
            {
                _mongoDatabase.CreateCollection(PermissionsCollectionName);
            }
        }

        private void ConnectToDb()
        {
            if (_mongoDatabase == null || _mongoDatabase.Server.State != MongoServerState.Connected)
            {
                _mongoDatabase = MongoDatabase.Create(_connectionString);
            }
        }

        public IEnumerable<Permission> GetUserPermissions(string userToken)
        {
            ConnectToDb();

            var collection = _mongoDatabase.GetCollection(typeof (PermissionPersistanceBsonDocument),
                PermissionsCollectionName);
        
            var permission =  collection.FindAllAs<PermissionPersistanceBsonDocument>().FirstOrDefault(p => p.UserId == userToken);
            if (permission != null)
            {
                return permission.UserPermissions.Select(p => new Permission
                {
                    PermissionName = "Permission for " + p.Key,
                    AccessLevel = p.Value,
                    TargetDataSourcePath = p.Key
                });
            }
            return null;
        }

        public void SaveUserPermissions(IEnumerable<Permission> permissions, string targetUserToken, string grantedByUserToken, Action<string> messageAction = null)
        {
            ConnectToDb();

            var collection = _mongoDatabase.GetCollection<PermissionPersistanceBsonDocument>(
                PermissionsCollectionName);
            var permission = collection.FindAllAs<PermissionPersistanceBsonDocument>().FirstOrDefault(p => p.UserId == targetUserToken);
            if (permission != null)
            {
                var permissionsCollection = permission.UserPermissions;
                foreach (var accessLevel in permissions)
                {
                    if (permissionsCollection.ContainsKey(accessLevel.TargetDataSourcePath))
                        permissionsCollection[accessLevel.TargetDataSourcePath] = accessLevel.AccessLevel;
                    else
                    {
                        permissionsCollection.Add(accessLevel.TargetDataSourcePath,accessLevel.AccessLevel);
                    }
                }
                collection.Update(Query<PermissionPersistanceBsonDocument>.EQ(p => p.UserId, targetUserToken),
                    Update<PermissionPersistanceBsonDocument>.Set(p => p.UserPermissions, permissionsCollection));
            }
            else
            {
                collection.Insert(new PermissionPersistanceBsonDocument
                {
                    GrantedByUserId = grantedByUserToken,
                    Id = ObjectId.GenerateNewId().ToString(),
                    UserId = targetUserToken,
                    UserPermissions = permissions.ToDictionary(p => p.TargetDataSourcePath, p => p.AccessLevel)
                });
            }
        }

       
    }

    class PermissionPersistanceBsonDocument
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string GrantedByUserId { get; set; }

        public Dictionary<string,AccessLevel> UserPermissions { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Security;
using MongoDB.Driver;

namespace FalconSoft.Data.Server.Persistence.MongoCollections
{
    public class MetaDataMongoCollections : BaseMongoCollections
    {
        private const string UsersCollectionName = "Users";
        private const string CompaniesCollectionName = "Companies";
        private const string PermissionsCollectionName = "Permissions";
        private const string DataSourceCollectionName = "MetaData_DataSourceInfo";
        private const string WorksheetInfoCollectionName = "MetaData_WorksheetInfo";
        private const string AggregatedWorksheetInfoCollectionName = "MetaData_AggregatedWorksheetInfo";

        public MetaDataMongoCollections(string connectionString) : base(connectionString)
        {
        }

        public MongoCollection Users
        {
            get { return GetMongoDatabase().GetCollection<User>(UsersCollectionName); }
        }
    }

}

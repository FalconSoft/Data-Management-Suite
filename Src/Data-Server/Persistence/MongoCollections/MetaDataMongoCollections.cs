using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

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

        public string GetCompanyId(string userId)
        {
            return Users.FindOneAs<User>(Query<User>.EQ((u) => u.Id, userId))
                        .CompanyId;
        }

        public MongoCollection<User> Users
        {
            get { return GetMongoDatabase().GetCollection<User>(UsersCollectionName); }
        }

        public MongoCollection<Permission> Permissions
        {
            get { return GetMongoDatabase().GetCollection<Permission>(PermissionsCollectionName); }
        }

        public MongoCollection<CompanyInfo> Companies
        {
            get { return GetMongoDatabase().GetCollection<CompanyInfo>(CompaniesCollectionName); }
        }

        public MongoCollection<DataSourceInfo> DataSources
        {
            get { return GetMongoDatabase().GetCollection<DataSourceInfo>(DataSourceCollectionName); }
        }

        public MongoCollection<WorksheetInfo> Worksheets
        {
            get { return GetMongoDatabase().GetCollection<WorksheetInfo>(WorksheetInfoCollectionName); }
        }

        public MongoCollection<AggregatedWorksheetInfo> AggregatedWorksheets
        {
            get { return GetMongoDatabase().GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName); }
        }

    }

}

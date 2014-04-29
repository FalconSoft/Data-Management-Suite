using System;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.Persistence
{
    public class WorksheetPersistence : IWorksheetPersistence
    {
        private readonly string _connectionString;
        private const string WorksheetInfoCollectionName = "WorksheetInfo";
        private const string AggregatedWorksheetInfoCollectionName = "AggregatedWorksheetInfo";
        private MongoDatabase _mongoDatabase;

        public WorksheetPersistence(string connectionString)
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

        public WorksheetInfo GetWorksheetInfo(string urn)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName)
                    .FindOne(Query.And(Query.EQ("Name", urn.GetName()),
                                                Query.EQ("Category", urn.GetCategory())));
        }

        public WorksheetInfo[] GetAvailableWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName).FindAll().ToArray();
        }

        public void UpdateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName);
            collection.Save(wsInfo);
        }

        public WorksheetInfo CreateWorksheetInfo(WorksheetInfo wsInfo, string userId)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<WorksheetInfo>(WorksheetInfoCollectionName);
            wsInfo.Id = Convert.ToString(ObjectId.GenerateNewId());
            collection.Insert(wsInfo);
            return collection.FindOneAs<WorksheetInfo>(Query.And(Query.EQ("Name", wsInfo.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", wsInfo.DataSourcePath.GetCategory())));
        }

        public void DeleteWorksheetInfo(string worksheetUrn, string userId)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(WorksheetInfoCollectionName).Remove(Query.And(Query.EQ("Name", worksheetUrn.GetName()),
                                                                  Query.EQ("Category", worksheetUrn.GetCategory())));
        }

        public AggregatedWorksheetInfo GetAggregatedWorksheetInfo(string worksheetUrn)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName)
                    .FindOne(Query.And(Query.EQ("Name", worksheetUrn.GetName()),
                                                Query.EQ("Category", worksheetUrn.GetCategory())));
        }

        public AggregatedWorksheetInfo[] GetAvailableAggregatedWorksheets(string userId, AccessLevel minAccessLevel = AccessLevel.Read)
        {
            ConnectToDb();
            return _mongoDatabase.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName).FindAll().ToArray();
        }

        public void UpdateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName);
            collection.Save(wsInfo);
        }

        public AggregatedWorksheetInfo CreateAggregatedWorksheetInfo(AggregatedWorksheetInfo wsInfo, string userId)
        {
            ConnectToDb();
            var collection = _mongoDatabase.GetCollection<AggregatedWorksheetInfo>(AggregatedWorksheetInfoCollectionName);
            wsInfo.Id = Convert.ToString(ObjectId.GenerateNewId());
            collection.Insert(wsInfo);
            return collection.FindOneAs<AggregatedWorksheetInfo>(Query.And(Query.EQ("Name", wsInfo.DataSourcePath.GetName()),
                                                                  Query.EQ("Category", wsInfo.DataSourcePath.GetCategory())));
        }

        public void DeleteAggregatedWorksheetInfo(string worksheetUrn, string userId)
        {
            ConnectToDb();
            _mongoDatabase.GetCollection(AggregatedWorksheetInfoCollectionName).Remove(Query.And(Query.EQ("Name", worksheetUrn.GetName()),
                                                                  Query.EQ("Category", worksheetUrn.GetCategory())));
        }
    }
}

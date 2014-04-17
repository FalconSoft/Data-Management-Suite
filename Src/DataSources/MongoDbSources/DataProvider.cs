using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources
{
    public class DataProvider : IDataProvider
    {
        private readonly string _dbName;
        private readonly string _connectionString;

        public DataProvider(string dataConnectionString, string dbName)
        {
            _connectionString = dataConnectionString;
            _dbName = dbName;
        }

        public DataSourceInfo DataSourceInfo { get; set; }

        public List<Dictionary<string, object>> GetData(string[] fields = null, IList<FilterRule> whereCondition = null)
        {
            MongoCollection<BsonDocument> collection = GetCollection(DataSourceInfo.DataSourcePath.ToValidDbString() + "_Data");
            MongoCursor<BsonDocument> cursor;
            string query = CreateFilterRuleQuery(whereCondition);
            if (string.IsNullOrEmpty(query))
            {
                cursor = collection.FindAll();
            }
            else
            {
                var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(query));
                cursor = collection.FindAs<BsonDocument>(qwraper);
            }
            var data = cursor.SetFields(Fields.Exclude("_id"));
            List<Dictionary<string, object>> listOfRecords = data.Select( bsdocumnet =>
                    bsdocumnet.ToDictionary(doc => doc.Name, doc => ToStrongTypedObject(doc.Value, doc.Name)))
                    .ToList();
            return listOfRecords;
        }

        public RevisionInfo SubmitChanges(List<Dictionary<string, object>> recordsToChange, List<string> recordsToDelete, string comment = null)
        {
            return SubmitChangesHelper(recordsToChange, recordsToDelete, DataSourceInfo.DataSourcePath,
                 comment);
        }

        public event EventHandler<ValueChangedEventArgs> RecordChangedEvent;


        public void UpdateSourceInfo(object sourceInfo)
        {
            DataSourceInfo = sourceInfo as DataSourceInfo;
        }

        /// <summary>
        ///     Submit changes into original data solection by provider string
        /// </summary>
        /// <param name="recordsToInsert">Data to insert</param>
        /// <param name="recordsToUpdate">Data to update</param>
        /// <param name="recordsToDelete">Data to delete</param>
        /// <param name="providerString">DataSource provider string</param>
        /// <param name="comment">Some Nice Comment :-)</param>
        /// <returns></returns>
        private RevisionInfo SubmitChangesHelper(IEnumerable<Dictionary<string, object>> recordsToChange,
            List<string> recordsToDelete,string providerString, string comment = null)
        {
            MongoCollection<BsonDocument> collection = GetCollection(providerString.ToValidDbString() + "_Data");
            UpdateRecords(recordsToChange, collection);
            DeleteRecords(recordsToDelete, collection);
            return  new RevisionInfo();
        }
        private string ConvertToMongoOperations(Operations operation, string value)
        {
            switch (operation)
            {
                case Operations.Equal:
                    return value;
                case Operations.NotEqual:
                    return "{ $ne :" + value + " }";
                case Operations.GreaterThan:
                    return "{ $gt :" + value + " }";
                case Operations.LessThan:
                    return "{ $lt :" + value + " }";
                case Operations.In:
                    return "{ $in :" + value.Replace('(', '[')
                                            .Replace(')', ']')
                                            .Replace(Convert.ToChar("'"), Convert.ToChar("\"")) + " }";
                case Operations.Like:
                    return "/" + value.Replace("'", "") + "/";
            }
            return "";
        }

        private string CreateFilterRuleQuery(IList<FilterRule> whereCondition)
        {
            if (whereCondition == null || !whereCondition.Any()) return string.Empty;
            string query = "{";
            foreach (FilterRule condition in whereCondition)
            {
                switch (condition.Combine)
                {
                    case CombineState.And:
                        query += " $and : [{ " + condition.FieldName + " : " +
                                 ConvertToMongoOperations(condition.Operation, condition.Value) + " } ],";
                        break;
                    case CombineState.Or:
                        query += " $or : [{ " + condition.FieldName + " : " +
                                 ConvertToMongoOperations(condition.Operation, condition.Value) + " } ],";
                        break;
                    default:
                        query += condition.Combine + " " + condition.FieldName + " : " +
                                 ConvertToMongoOperations(condition.Operation, condition.Value) + ",";
                        break;
                }
            }
            query.Remove(query.Count() - 2);
            query += @"}";
            return query;
        }


        private object ToStrongTypedObject(BsonValue bsonValue, string fieldName)
        {
            var dataType = DataSourceInfo.Fields.First(f => f.Key == fieldName).Value.DataType;
            if (bsonValue.ToString() == string.Empty)
                return null;
            switch (dataType)
            {
                case DataTypes.Int:
                    return bsonValue.ToInt32();
                case DataTypes.Double:
                    return bsonValue.ToDouble();
                case DataTypes.String:
                    return bsonValue.ToString();
                case DataTypes.Bool:
                    return bsonValue.ToBoolean();
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return bsonValue.ToLocalTime();
                default:
                    throw new NotSupportedException("DataType is not supported");
            }
        }

        private BsonValue ToBsonValue(string value, string fieldName)
        {
            var dataType = DataSourceInfo.Fields.First(f => f.Key == fieldName).Value.DataType;
            if (string.IsNullOrEmpty(value))
                return BsonString.Empty;
            switch (dataType)
            {
                case DataTypes.Int:
                    return new BsonInt32(Convert.ToInt32(value));
                case DataTypes.Double:
                    return new BsonDouble(Convert.ToDouble(value));
                case DataTypes.String:
                    return new BsonString(value);
                case DataTypes.Bool:
                    return new BsonBoolean(Convert.ToBoolean(value));
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return new BsonDateTime(Convert.ToDateTime(value));
                default:
                    throw new NotSupportedException("DataType is not supported");
            }
        }

        private void DeleteRecords(IEnumerable<string> recordsToDelete, MongoCollection<BsonDocument> collection)
        {
            foreach (var record in recordsToDelete)
            {
                var queryDoc = new QueryDocument();
                foreach (var keyFieldName in DataSourceInfo.GetKeyFieldsName())
                {
                    queryDoc.Add(keyFieldName, ToBsonValue(record.Replace("|",""),keyFieldName)); //TODO TEST this
                }
                collection.Remove(queryDoc);
            }
        }

        private void UpdateRecords(IEnumerable<IDictionary<string, object>> recordsToUpdate, MongoCollection<BsonDocument> collection)
        {
            foreach (var records in recordsToUpdate)
            {
                var queryDoc = new QueryDocument();
                foreach (var rec in records)
                {
                    queryDoc.Add(rec.Key, ToBsonValue(rec.Value != null ? rec.Value.ToString() : string.Empty, rec.Key));
                }
                collection.Save(queryDoc); // TODO need to TEST this, if some problems then remove save and add Update & Insert 
                
            }
        }


        private MongoCollection<BsonDocument> GetCollection(string name)
        {
            MongoServer server = MongoServer.Create(_connectionString);
            MongoDatabase db = server.GetDatabase(_dbName);
            if (!db.CollectionExists(name))
                throw new InvalidDataException("No collection with such name exists!!!");
            MongoCollection<BsonDocument> collection = db.GetCollection(name);
            return collection;
        }
    }
}
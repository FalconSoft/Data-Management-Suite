using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;


namespace FalconSoft.Data.Server.DefaultMongoDbSource
{
    public class DataProvider : IDataProvider
    {
        private readonly MongoDbCollections _mongoCollections;

        public DataProvider(MongoDbCollections mongoCollections, DataSourceInfo datasourceInfo)
        {
            _mongoCollections = mongoCollections;
            DataSourceInfo = datasourceInfo;
        }

        public DataSourceInfo DataSourceInfo { get; private set; }

        public IEnumerable<Dictionary<string, object>> GetData(string[] fields = null, FilterRule[] filterRules = null, Action<string, string> onError = null)
        {
            var collection = _mongoCollections.GetDataCollection(DataSourceInfo.CompanyId, DataSourceInfo.Urn);
            MongoCursor<BsonDocument> cursor;
            var query = CreateFilterRuleQuery(filterRules);
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
            var listOfRecords = data.Select(bsdocumnet =>
                bsdocumnet.ToDictionary(doc => doc.Name, doc => BsonTypeMapper.MapToDotNetValue(doc.Value)));
            return listOfRecords;
        }

        public RevisionInfo SubmitChanges(IEnumerable<Dictionary<string, object>> recordsToChange, IEnumerable<string> recordsToDelete, string comment = null)
        {
            return SubmitChangesHelper(recordsToChange, recordsToDelete, DataSourceInfo.Urn,
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
        /// <param name="recordsToChange">Data to change</param>
        /// <param name="recordsToDelete">Data to delete</param>
        /// <param name="providerString">DataSource provider string</param>
        /// <param name="comment">Some Nice Comment :-)</param>
        /// <returns></returns>
        private RevisionInfo SubmitChangesHelper(IEnumerable<Dictionary<string, object>> recordsToChange,
            IEnumerable<string> recordsToDelete, string providerString, string comment = null)
        {
            var collection = GetDataCollection(providerString);
            var isDeleted = DeleteRecords(recordsToDelete, collection);
            var isUpdated = UpdateRecords(recordsToChange, collection);
            return new RevisionInfo
            {
                IsSuccessfull = isUpdated || isDeleted
            };
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

        private string CreateFilterRuleQuery(IEnumerable<FilterRule> whereCondition)
        {
            if (whereCondition == null || !whereCondition.Any()) return string.Empty;
            var query = "{";
            foreach (var condition in whereCondition)
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

        private bool DeleteRecords(IEnumerable<string> recordsToDelete, MongoCollection<BsonDocument> collection)
        {
            var isSuccessful = false;
            foreach (var record in recordsToDelete)
            {
                var queryDoc = new QueryDocument();
                foreach (var keyFieldName in DataSourceInfo.GetKeyFieldsName())
                {
                    queryDoc.Add(keyFieldName, ToBsonValue(record.Replace("|", ""), keyFieldName)); 
                }
                var result = collection.Find(queryDoc);
                if (!result.Any()) continue;
                isSuccessful = true;
                collection.Remove(queryDoc);
            }
            return isSuccessful;
        }

        private bool UpdateRecords(IEnumerable<IDictionary<string, object>> recordsToUpdate, MongoCollection<BsonDocument> collection)
        {
            var isSuccessful = false;
            var itemsToInsert = new List<QueryDocument>();
            foreach (var records in recordsToUpdate)
            {
                var queryDoc = new QueryDocument();
                foreach (var rec in records)
                {
                    queryDoc.Add(rec.Key, ToBsonValue(rec.Value != null ? rec.Value.ToString() : string.Empty, rec.Key));
                }
                var result = collection.Find(CreateFindKeyQuery(records));
                if (!result.Any())
                {
                    itemsToInsert.Add(queryDoc);
                    isSuccessful = true;
                }
                else if (!Equal(result.First(), records))
                {
                    foreach (var q in queryDoc)
                    {
                        collection.Update(CreateFindKeyQuery(records), Update.Set(q.Name, q.Value));
                    }
                    isSuccessful = true;
                }
            }

            if (itemsToInsert.Any())
            {
                collection.InsertBatch(itemsToInsert);
                isSuccessful = true;
            }
            return isSuccessful;
        }

        private MongoCollection<BsonDocument> GetDataCollection(string urn)
        {
            var collection = _mongoCollections.GetDataCollection(DataSourceInfo.CompanyId, urn);
            foreach (var keyField in DataSourceInfo.GetAllIndexFieldsNames())
            {
                collection.CreateIndex(keyField);    
            }
            
            return collection;
        }

        private IMongoQuery CreateFindKeyQuery(IDictionary<string, object> record)
        {
            return Query.And(DataSourceInfo.GetKeyFieldsName().Select(key => Query.EQ(key, ToBsonValue(record[key] != null ? record[key].ToString() : string.Empty, key))));
        }
        
        private bool Equal(IEnumerable<BsonElement> doc, IDictionary<string, object> record)
        {
            return !doc.Select(x => x.Name).Any(x => !record.ContainsKey(x))
                && doc.Where(x => x.Name != "_id")
                .All(element => element.Value == ToBsonValue(record[element.Name] != null ?
                    record[element.Name].ToString() : string.Empty, element.Name));
        }
    }
}
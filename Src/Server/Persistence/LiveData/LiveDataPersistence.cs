using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace FalconSoft.ReactiveWorksheets.Persistence.LiveData
{
    public class LiveDataPersistence : ILiveDataPersistence
    {
        private readonly MongoCollection<BsonDocument> _collection;

        public LiveDataPersistence(string connectionString, string collectionName)
        {
            var database = MongoDatabase.Create(connectionString);
            if (database.CollectionExists(collectionName))
            {
               _collection = database.GetCollection(collectionName);
            }
            else
            {
                database.CreateCollection(collectionName);
                _collection = database.GetCollection(collectionName);
                _collection.EnsureIndex("RecordKey");
            }
        }

        /// <summary>
        ///   Get data from collection
        /// </summary>
        /// <param name="fields">Ignored and not implementer. Set Null as input param</param>
        /// <param name="filterRules">Fiter rule to get data by custom condition. Set Null as input param to get all availible data</param>
        /// <returns>Return data for records</returns>
        public IEnumerable<LiveDataObject> GetData(string[] fields = null, FilterRule[] filterRules = null)
        {
            var query = CreateFilterRuleQuery(filterRules);
            MongoCursor<LiveDataObject> cursor;

            if (!string.IsNullOrEmpty(query))
            {
                var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(query));
                cursor = _collection.FindAs<LiveDataObject>(qwraper);
                return cursor.SetFields(Fields.Exclude("_id")).ToList();
            }

            cursor = _collection.FindAllAs<LiveDataObject>();
            return cursor.SetFields(Fields.Exclude("_id")).ToList();
        }

        public IEnumerable<T> GetData<T>(string dataSourcePath, FilterRule[] filterRules = null)
        {
            var query = CreateFilterRuleQuery(filterRules);
            MongoCursor<T> cursor;

            if (!string.IsNullOrEmpty(query))
            {
                var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(query));
                cursor = _collection.FindAs<T>(qwraper);
                return cursor;
            }

            cursor = _collection.FindAllAs<T>();
            return cursor;
        }

        /// <summary>
        /// Find all data by record key
        /// </summary>
        /// <param name="rekordKey">Array of data record keys what we are looking for</param>
        /// <returns>All matched data</returns>
        public IEnumerable<LiveDataObject> GetDataByKey(string[] rekordKey)
        {
            return _collection.AsQueryable().Cast<LiveDataObject>()
                    .Where(e => rekordKey.Contains(e.RecordKey)).ToList();
        }

        public IEnumerable<LiveDataObject> GetAggregatedData(AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null)
        {
            var keyCols = aggregatedWorksheet.GroupByColumns.Select(x => x.Header).ToArray();
            var result = new List<LiveDataObject>();
            var collection = _collection.Aggregate(aggregatedWorksheet.GetPipeline()).ResultDocuments;

            foreach (var row in collection)
            {
                var dic = keyCols.ToDictionary<string, string, object>(key => key, key => row["_id"][key]);
                foreach (var el in row.ToDictionary(x => x.Name, x => (object) x.Value.ToString()).Where(x => x.Key != "_id"))
                {
                    dic.Add(el.Key,el.Value);
                }
                result.Add(new LiveDataObject
                {
                    RecordKey = DataHelper.WorkOutRecordKey(dic, keyCols),
                    RecordValues = dic
                });
            }
            return result;
        }

        public IEnumerable<LiveDataObject> GetDataByForeignKey(Dictionary<string, object> record)
        {
            if (record.Keys.Any(a => a == null))
                return null;

            var queryList = record.Select(rec => Query.EQ(string.Format("RecordValues.{0}", rec.Key), BsonValue.Create(rec.Value)));
            return _collection.FindAs<LiveDataObject>(Query.And(queryList)).SetFields(Fields.Exclude("_id"));

            //return _collection.AsQueryable().Cast<LiveDataObject>()
            //        .Where(e => e.RecordValues[foreignFieldName] == recordValue).ToList();
        }

        public void UpdateForeignIndexes(string[] fields)
        {
            if ((fields == null) || !fields.Any())
                return;
            
            foreach (var field in fields)
            {
                _collection.EnsureIndex(string.Format("RecordValues.{0}", field));
            }
        }

        /// <summary>
        /// Insert, update or remove record data due to ChangedAction and OriginalRecordKey
        /// </summary>
        /// <param name="record">RecorChanedParam with live data</param>
        /// <returns>Return recordChangedParam fith full record data</returns>
        public RecordChangedParam SaveData(RecordChangedParam record)
        {
            // make query that will find document by original record key
            var query = Query<LiveDataObject>.EQ(e => e.RecordKey, record.RecordKey);

            var entity = new LiveDataObject
            {
                RecordKey = record.RecordKey,
                UserToken = record.UserToken,
                RecordValues = record.RecordValues
            };

            if (record.ChangedAction == RecordChangedAction.AddedOrUpdated)

                if (_collection.FindAs<LiveDataObject>(query).SetFields(Fields.Exclude("_id")).FirstOrDefault() != null)
                {
                    // list of updates that will be done in document
                    var updateValues = new List<UpdateBuilder>();
                    updateValues.Add(Update.Set("RecordKey", record.RecordKey));
                    updateValues.Add(Update.Set("UserToken", record.UserToken ?? BsonNull.Value.ToString()));

                    // if i have to update just several fields in dictionary
                    if (record.ChangedPropertyNames != null)
                        // updateValues.AddRange(record.ChangedPropertyNames.Select(name => Update<LiveDataObject>.Set(e => e.RecordValues[name], record.RecordValues[name])).ToArray());
                        updateValues.AddRange(record.ChangedPropertyNames.Select(name => Update.Set(string.Format("RecordValues.{0}", name), record.RecordValues[name] == null? BsonNull.Value : BsonValue.Create(record.RecordValues[name]))).ToArray());
                    else
                        // if i have to update all properies in dicationary
                        // updateValues.Add(Update<LiveDataObject>.Set(e => e.RecordValues, record.RecordValues));
                        updateValues.Add(Update.Set("RecordValues", record.RecordValues.ToBsonDocument()));

                    var update = Update<LiveDataObject>.Combine(updateValues);

                    // thread save get last update of document
                    var documents = _collection.FindAndModify(query, SortBy<LiveDataObject>.Ascending(e => e.RecordKey),
                        update, true);
                    documents.ModifiedDocument.Remove("_id");
                    var document = BsonSerializer.Deserialize<LiveDataObject>(documents.ModifiedDocument);
                    record.RecordValues = document.RecordValues;
                    return record;
                }
                else
                {
                    _collection.Insert(entity);
                    return record;
                }
            if (record.ChangedAction == RecordChangedAction.Removed)
            {
                record.RecordValues =
                    _collection.FindAs<LiveDataObject>(Query.EQ("RecordKey", record.RecordKey)).SetFields(Fields.Exclude("_id")).First().RecordValues;
                _collection.Remove(query);
                return record;
            }
            return null;
        }

        private string ConvertToMongoOperations(Operations operation, string value)
        {
            if (!string.IsNullOrEmpty(value) && value.ToCharArray()[0] != Convert.ToChar("'"))
            {
                var t = 0;
                double d = 0;
                if (int.TryParse(value, out t) == false || (double.TryParse(value, out d) == false))
                    value = string.Format("'{0}'", value);
            }

            switch (operation)
            {
                case Operations.Equal: return value;
                case Operations.NotEqual: return "{ $ne :" + value + " }";
                case Operations.GreaterThan: return "{ $gt :" + value + " }";
                case Operations.LessThan: return "{ $lt :" + value + " }";
                case Operations.In: return "{ $in :" + value.Replace('(', '[').Replace(')', ']').Replace(Convert.ToChar("'"), Convert.ToChar("\"")) + " }";
                case Operations.Like: return "/" + value.Replace("'", "") + "/";
            }
            return "";
        }

        private string CreateFilterRuleQuery(IList<FilterRule> whereCondition)
        {
            if (whereCondition == null || whereCondition.Count == 0) return string.Empty;
            var query = "{";
            foreach (var condition in whereCondition)
            {
                switch (condition.Combine)
                {
                    case CombineState.And:
                        query += " $and : [{ \"RecordValues."+ condition.FieldName + "\" : " +
                                 ConvertToMongoOperations(condition.Operation, condition.Value) + " } ],";
                        break;
                    case CombineState.Or:
                        query += " $or : [{ \"RecordValues." + condition.FieldName + "\" : " +
                                 ConvertToMongoOperations(condition.Operation, condition.Value) + " } ],";
                        break;
                    default:
                        query += condition.Combine + " \"RecordValues." + condition.FieldName + "\" : " + ConvertToMongoOperations(condition.Operation, condition.Value) + ",";
                        break;
                }
            }
            query = query.Remove(query.Count() - 1);
            query += @"}";
            return query;
        }
    }

}
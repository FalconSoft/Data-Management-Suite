using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace FalconSoft.Data.Server.Persistence.LiveData
{
    public class LiveDataPersistence : ILiveDataPersistence
    {
        private readonly MongoCollection<BsonDocument> _collection;
        private readonly ILogger _logger;

        public LiveDataPersistence(string connectionString, string collectionName, ILogger logger)
        {
            _logger = logger;
            var database = MongoDatabase.Create(connectionString);
            if (database.CollectionExists(collectionName))
            {
                _collection = database.GetCollection(collectionName);
            }
            else
            {
                database.CreateCollection(collectionName);
                _collection = database.GetCollection(collectionName);
                _collection.CreateIndex("RecordKey");
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
            try
            {
                var query = CreateFilterRuleQuery(filterRules);
                MongoCursor<LiveDataObject> cursor;
                if (!string.IsNullOrEmpty(query))
                {
                    if (fields != null)
                        query += ", +" + CreateSelectedFieldsQuery(fields);
                    var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(query));
                    cursor = _collection.FindAs<LiveDataObject>(qwraper);
                    return cursor;
                }
                if (fields != null)
                {
                    var mongoQuery = "{ }," + CreateSelectedFieldsQuery(fields);
                    var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(mongoQuery));
                    cursor = _collection.FindAs<LiveDataObject>(qwraper);
                    return cursor;
                }
                cursor = _collection.FindAllAs<LiveDataObject>();
                return cursor;
            }
            catch (Exception ex)
            {
                _logger.Debug("GetData() Error: " + ex.Message);
                return new List<LiveDataObject>();
            }


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
            try
            {
                return _collection.AsQueryable<LiveDataObject>().Where(w => rekordKey.Contains(w.RecordKey));
            }
            catch (Exception ex)
            {
                var primaryKeys = rekordKey.Aggregate(string.Empty, (current, record) => current + (record + ", "));
                _logger.Debug("GetDataByKey() Error:" + "Keys : " + primaryKeys + " Error:" + ex.Message);
                return new List<LiveDataObject>();
            }
        }

        public IEnumerable<LiveDataObject> GetAggregatedData(AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null)
        {
            try
            {
                var keyCols = aggregatedWorksheet.GroupByColumns.Select(x => x.Header).ToArray();
                var result = new List<LiveDataObject>();
                var collection = _collection.Aggregate(aggregatedWorksheet.GetPipeline()).ResultDocuments;

                foreach (var row in collection)
                {
                    var dic = keyCols.ToDictionary<string, string, object>(key => key, key => row["_id"][key]);
                    foreach (
                        var el in
                            row.ToDictionary(x => x.Name, x => (object)x.Value.ToString()).Where(x => x.Key != "_id"))
                    {
                        dic.Add(el.Key, el.Value);
                    }
                    result.Add(new LiveDataObject
                    {
                        _id = Guid.NewGuid(),
                        RecordKey = dic.WorkOutRecordKey(keyCols),
                        RecordValues = dic
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.Debug("GetAggregatedData() Error:" + ex.Message);
                return new List<LiveDataObject>();
            }

        }

        public IEnumerable<LiveDataObject> GetDataByForeignKey(Dictionary<string, object> record)
        {
            try
            {
                if (record.Keys.Any(a => a == null))
                    return null;

                var queryList = record.Select(rec => Query.EQ(string.Format("RecordValues.{0}", rec.Key), BsonValue.Create(rec.Value)));
                if (!queryList.Any()) return new LiveDataObject[0];
                return _collection.FindAs<LiveDataObject>(Query.And(queryList));
            }
            catch (Exception ex)
            {
                _logger.Debug("GetDataByForeignKey() Error:" + ex.Message);
                return new List<LiveDataObject>();
            }
        }

        public void UpdateForeignIndexes(string[] fields)
        {
            try
            {
                if (fields == null) return;
                foreach (var field in fields)
                {
                    _collection.CreateIndex(string.Format("RecordValues.{0}", field));
                }
            }
            catch (Exception ex)
            {
                var ufields = fields.Aggregate(string.Empty, (current, record) => current + (record + ", "));
                _logger.Debug("UpdateForeignIndexes() Error:" + "Fields : " + ufields + " Error:" + ex.Message);
            }
        }

        public void BulkUpsertData(IEnumerable<RecordChangedParam> recordParams)
        {
            try
            {
                var groupedRecords = new Dictionary<string, RecordChangedParam>();
                foreach (var recordChangedParam in recordParams)
                {
                    groupedRecords[recordChangedParam.RecordKey] = recordChangedParam;
                }

                var existedRecords = GetDataByKey(groupedRecords.Keys.ToArray()).Select(s => s.RecordKey).ToArray();
                var recordsToUpdate = groupedRecords.Keys.Intersect(existedRecords);
                var recordsToInsert = groupedRecords.Keys.Except(existedRecords)
                    .ToDictionary(k => k, k => new LiveDataObject
                    {
                        _id = Guid.NewGuid(),
                        RecordKey = groupedRecords[k].RecordKey,
                        UserToken = groupedRecords[k].UserToken,
                        RecordValues = groupedRecords[k].RecordValues
                    });

                foreach (var recordtoUpdate in recordsToUpdate)
                {
                    UpdateRecord(groupedRecords[recordtoUpdate]);
                }

                if (recordsToInsert.Any())
                {
                    _collection.InsertBatch(recordsToInsert.Values);
                }
            }
            catch (Exception ex)
            {
                var keys = recordParams.Select(s => s.RecordKey).Aggregate(string.Empty, (current, record) => current + (record + ", "));
                _logger.Debug("BulkUpsertData() Error:" + "RecordKeys : " + keys + " Error:" + ex.Message);
            }
        }

        public bool CheckExistence(string fieldName, object value)
        {
            var queryList = Query.EQ(string.Format("RecordValues.{0}", fieldName), BsonValue.Create(value));
            var colecton = _collection.Find(queryList);
            var count = colecton.Count();
            if (count > 0)
            {
                return true;
            }
            return false;
        }

        public void ClearCollection()
        {
            _collection.RemoveAll();
        }

        /// <summary>
        /// Insert, update or remove record data due to ChangedAction and OriginalRecordKey
        /// </summary>
        /// <param name="record">RecorChanedParam with live data</param>
        /// <returns>Return recordChangedParam fith full record data</returns>
        public RecordChangedParam SaveData(RecordChangedParam record)
        {
            try
            {
                // make query that will find document by original record key
                var query = Query<LiveDataObject>.EQ(e => e.RecordKey, record.RecordKey);

                var entity = new LiveDataObject
                {
                    _id = Guid.NewGuid(),
                    RecordKey = record.RecordKey,
                    UserToken = record.UserToken,
                    RecordValues = record.RecordValues
                };

                if (record.ChangedAction == RecordChangedAction.AddedOrUpdated)

                    if (_collection.FindAs<LiveDataObject>(query).FirstOrDefault() != null)
                    {
                        UpdateRecord(record);
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
                        _collection.FindAs<LiveDataObject>(Query.EQ("RecordKey", record.RecordKey)).First().RecordValues;
                    _collection.Remove(query);
                    return record;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.Debug("SaveData() Error:" + "RecordKey : " + record.RecordKey + " Error:" + ex.Message);
                return null;
            }
        }

        private void UpdateRecord(RecordChangedParam record)
        {
            var query = Query<LiveDataObject>.EQ(e => e.RecordKey, record.RecordKey);
            var updateValues = new List<UpdateBuilder>();
            if (record.ChangedAction == RecordChangedAction.Removed)
            {
                _collection.Remove(query);
                return;
            }
            updateValues.Add(Update.Set("RecordKey", record.RecordKey));
            updateValues.Add(Update.Set("UserToken", record.UserToken ?? BsonNull.Value.ToString()));

            if (record.ChangedPropertyNames != null)
                updateValues.AddRange(
                    record.ChangedPropertyNames.Select(name => Update.Set(string.Format("RecordValues.{0}", name), record.RecordValues[name] == null ? BsonNull.Value : BsonValue.Create(record.RecordValues[name]))).ToArray());
            else
                updateValues.Add(Update.Set("RecordValues", record.RecordValues.ToBsonDocument()));
            var update = Update<LiveDataObject>.Combine(updateValues);
            //var update = Update.Set("RecordKey", record.RecordKey).Set("UserToken", record.UserToken ?? BsonNull.Value.ToString());
            //if (record.ChangedPropertyNames != null)
            //        record.ChangedPropertyNames.Select(name => update.Set(string.Format("RecordValues.{0}", name), record.RecordValues[name] == null ? BsonNull.Value : BsonValue.Create(record.RecordValues[name]))).Count();
            //else
            //    update.Set("RecordValues", record.RecordValues.ToBson());
            _collection.Update(query, update);
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
                case Operations.In:
                    {
                        var query = "{ $in :" +
                                    value.Replace("'(", "[")
                                        .Replace(")'", "]")
                                        .Replace(Convert.ToChar("'"), Convert.ToChar("\"")) + " }";
                        return query;
                    }
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
                        query += " $and : [{ \"RecordValues." + condition.FieldName + "\" : " +
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

        private string CreateSelectedFieldsQuery(string[] fields)
        {
            var constStr = "{_id : 1, RecordKey : 1, UserToken : 1,";
            var query = constStr;
            foreach (var field in fields)
            {
                if (fields.Last() == field)
                {
                    query += string.Format(" RecordValues.{0} : 1",field) + "}";
                    break;
                }
                query += string.Format(" RecordValues.{0} : 1,",field);
            }
            return query;
        }
    }

    
}
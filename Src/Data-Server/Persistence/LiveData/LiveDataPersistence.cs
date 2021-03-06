﻿using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Server.Persistence.MongoCollections;
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

        public LiveDataPersistence(LiveDataMongoCollections mongoCollections, string dataSourceUrn, ILogger logger)
        {
            _logger = logger;
            _collection = mongoCollections.GetDataCollection(dataSourceUrn);
            _collection.CreateIndex("RecordKey");
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
                var query =  CreateFilterRuleQuery(filterRules != null? filterRules.ToList() : null);
                MongoCursor<LiveDataObject> cursor;
                if (!string.IsNullOrEmpty(query))
                {
                    if (fields != null)
                        query += ", " + CreateSelectedFieldsQuery(fields);
                    var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(query));
                    cursor = _collection.FindAs<LiveDataObject>(qwraper);
                    return cursor;
                }
                if (fields != null)
                {
                    var mongoQuery = "{ }, " + CreateSelectedFieldsQuery(fields);
                    var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(mongoQuery));
                    cursor = _collection.FindAs<LiveDataObject>(qwraper)
                        .SetFields(Fields.Include(fields.Select(f => string.Format("RecordValues.{0}", f)).ToArray()));
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

        public IEnumerable<string> GetFieldValues(string fieldName, string match, int elementsToReturn = 10)
        {
            try
            {
                var qwraper = new QueryDocument(BsonSerializer.Deserialize<BsonDocument>(CreateSelectedFieldQuery(fieldName, match)));
                //var cursor = _collection.FindAs<LiveDataObject>(qwraper).SetLimit(elementsToReturn).Where(x => x.RecordValues.ContainsKey(fieldName)).DistinctBy(y => y.RecordValues[fieldName]).Select(x => Convert.ToString(x.RecordValues[fieldName]));
                var cursor = _collection.FindAs<LiveDataObject>(qwraper).Where(x => x.RecordValues.ContainsKey(fieldName)).DistinctBy(x => x.RecordValues[fieldName]).Take(elementsToReturn).Select(x => Convert.ToString(x.RecordValues[fieldName])).OrderBy(x => x);
                //var cursor = _collection.AsQueryable<LiveDataObject>()
                //    .Where(x => x.RecordValues.ContainsKey(fieldName))
                //    .Where(x => Convert.ToString(x.RecordValues[fieldName]).StartsWith(match))
                //    .DistinctBy(y => y.RecordValues[fieldName])
                //    .Take(elementsToReturn)
                //    .Select(x => Convert.ToString(x.RecordValues[fieldName]));
                return cursor;
            }
            catch (Exception ex)
            {
                _logger.Debug("GetData() Error: " + ex.Message);
                return new string[0];
            }
        }

        /// <summary>
        /// Find all data by record key
        /// </summary>
        /// <param name="rekordKey">Array of data record keys what we are looking for</param>
        /// <returns>All matched data</returns>
        public IEnumerable<LiveDataObject> GetDataByKey(string[] rekordKey, string[] fields = null)
        {
            try
            {
                //if (fields == null)
                    return _collection.AsQueryable<LiveDataObject>().Where(w => rekordKey.Contains(w.RecordKey));
               // var result = _collection.AsQueryable<LiveDataObject>().Where(w => rekordKey.Contains(w.RecordKey)).Select(s=>MergeFields(s,fields));
               // return result;
            }
            catch (Exception ex)
            {
                var primaryKeys = rekordKey.Aggregate(string.Empty, (current, record) => current + (record + ", "));
                _logger.Debug("GetDataByKey() Error:" + "Keys : " + primaryKeys + " Error:" + ex.Message);
                return new List<LiveDataObject>();
            }
        }

        private LiveDataObject MergeFields(LiveDataObject liveDataObject, string[] fields)
        {
             liveDataObject.RecordValues = liveDataObject.RecordValues.Join(fields, j1 => j1.Key, j2 => j2, (j1, j2) => j1)
                                        .ToDictionary(k => k.Key, v => v.Value);
            return liveDataObject;
        }

        public IEnumerable<LiveDataObject> GetAggregatedData(AggregatedWorksheetInfo aggregatedWorksheet, FilterRule[] filterRules = null)
        {
            try
            {
                var keyCols = aggregatedWorksheet.GroupByColumns.Select(x => x.Header).ToArray();
                var result = new List<LiveDataObject>();
                var collection = _collection.Aggregate(DbUtils.GetPipeline(aggregatedWorksheet)).ResultDocuments;

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
            return count > 0;
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

        private static string ConvertToMongoOperations(Operations operation, string value)
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
                                        .Replace("'", "\"") + " }";
                        return query;
                    }
                case Operations.Like: return "/" + value.Replace("'", "") + "/";
            }
            return "";
        }

        private static string CreateFilterRuleQuery(IList<FilterRule> whereCondition)
        {
            if (whereCondition == null || !whereCondition.Any()) return string.Empty;

            var index = whereCondition.Count - 1;
            var filetrRule = whereCondition[index];
            whereCondition.Remove(filetrRule);

            var query = "{";

            if (index == 0)
            {
                query += " \"RecordValues." + filetrRule.FieldName + "\" : " + ConvertToMongoOperations(filetrRule.Operation, filetrRule.Value) + "}";
                return query;
            }

            switch (filetrRule.Combine)
            {
                case CombineState.And:
                    query += " $and :" + " [{\"RecordValues." + filetrRule.FieldName + "\" : " +
                             ConvertToMongoOperations(filetrRule.Operation, filetrRule.Value) + "}, " +
                             CreateFilterRuleQuery(whereCondition) + "]}";
                    break;
                case CombineState.Or:
                    query += " $or :" + " [{\"RecordValues." + filetrRule.FieldName + "\" : " +
                             ConvertToMongoOperations(filetrRule.Operation, filetrRule.Value) + "}, " +
                             CreateFilterRuleQuery(whereCondition) + "]}";
                    break;
            } 
            return query;
        }

        private static string CreateSelectedFieldsQuery(string[] fields)
        {
            const string constStr = "{_id : 1, RecordKey : 1, UserToken : 1,";
            var query = constStr;
            foreach (var field in fields)
            {
                if (fields.Last() == field)
                {
                    query += string.Format(" \"RecordValues.{0}\" : 1", field) + "}";
                    break;
                }
                query += string.Format(" \"RecordValues.{0}\" : 0,", field);
            }
            return query;
        }

        private static string CreateSelectedFieldQuery(string field, string match)
        {
            return string.Format("{{'RecordValues.{0}' : /^{1}/}}", field, match);
        }
    }
}
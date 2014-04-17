using System;
using System.Collections.Generic;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using MongoDB.Bson;

namespace FalconSoft.ReactiveWorksheets.Persistence
{
    public static class MongoDBExtention
    {
        private static BsonElement ConvertAggregateFunc(string header, string fieldName, AggregatedFunction function)
        {
            switch (function)
            {
                case AggregatedFunction.Count:
                    return new BsonElement(header, new BsonDocument
                    {
                        { "$sum" ,1 }
                    });
                case AggregatedFunction.Sum:
                    return new BsonElement(header, new BsonDocument
                    {
                        { "$sum" , string.Format("$RecordValues.{0}",fieldName) }
                    });
                case AggregatedFunction.Avg:
                    return new BsonElement(header, new BsonDocument
                    {
                        { "$avg" , string.Format("$RecordValues.{0}",fieldName) }
                    });
                case AggregatedFunction.Min:
                    return new BsonElement(header, new BsonDocument
                    {
                        { "$min" , string.Format("$RecordValues.{0}",fieldName) }
                    });
                case AggregatedFunction.Max:
                    return new BsonElement(header, new BsonDocument
                    {
                        { "$max" , string.Format("$RecordValues.{0}",fieldName) }
                    });
                default:
                    throw new ArgumentOutOfRangeException("function");
            }
        }
        private static BsonElement ConvertGroupId(IEnumerable<string> fieldNames)
        {
            return new BsonElement("_id", new BsonDocument(fieldNames.Select(x => new BsonElement(x, string.Format("$RecordValues.{0}", x)))));
        }

        public static BsonDocument[] GetPipeline(this AggregatedWorksheetInfo aggregatedWorksheet)
        {
            var agregations = new BsonDocument
            {
                ConvertGroupId(aggregatedWorksheet.GroupByColumns.Select(x => x.Header))
            };
            agregations.AddRange(aggregatedWorksheet.Columns.Select(x => ConvertAggregateFunc(x.Value.Header, x.Value.FieldName, x.Key)));
            var group = new BsonDocument
            {
                {
                    "$group", agregations
                }
            };
            var pipelane = new[] {group};
            return pipelane;
        }
    }
}

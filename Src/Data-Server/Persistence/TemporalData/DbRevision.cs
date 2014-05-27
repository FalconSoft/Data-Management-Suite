using System;
using MongoDB.Bson;

namespace FalconSoft.Data.Server.Persistence.TemporalData
{
    public class DbRevision
    {
        public ObjectId Id { get; set; }
        public string ProviderString { get; set; }
        public DateTime TimeStamp { get; set; }
        public string UserId { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public class DataSourceInfo : HeaderInfo, ICloneable
    {
        public DataSourceInfo()
        {
            Fields = new Dictionary<string, FieldInfo>();
            Relationships = new Dictionary<string, RelationshipInfo>();
        }
        public DataSourceInfo(IEnumerable<FieldInfo> fields, bool accessToOriginalData = false)
        {
            Relationships = new Dictionary<string, RelationshipInfo>();
            Fields = fields.ToDictionary(f => f.Name, f => f);
            AccessToOriginalData = accessToOriginalData;
        }

        /// <summary>
        /// DataSource's fields
        /// </summary>
        public IDictionary<string, FieldInfo> Fields { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        public IDictionary<string, RelationshipInfo> Relationships { get; set; }

        /// <summary>
        /// Parent Source DataSourcePath
        /// </summary>
        public string ParentDataSourcePath { get; set; }

        /// <summary>
        /// Parent Source FilterString(analogy to whereclause prop)
        /// </summary>
        public string ParentFilterString { get; set; }

        /// <summary>
        /// History Storage Type: event,buffer,time (default use buffer)
        /// </summary>
        public HistoryStorageType HistoryStorageType { get; set; }

        /// <summary>
        /// History Storage Type Param (by default = 100)
        /// </summary>
        public string HistoryStorageTypeParam { get; set; }


        /// <summary>
        /// Access to original data 
        /// </summary>
        public bool AccessToOriginalData { get;  set; }

        public void Update(DataSourceInfo dataSourceInfo)
        {
            Id = dataSourceInfo.Id;
            Name = dataSourceInfo.Name;
            Category = dataSourceInfo.Category;
            ImageSource = dataSourceInfo.ImageSource;
            Description = dataSourceInfo.Description;
            Relationships = dataSourceInfo.Relationships != null ? new Dictionary<string, RelationshipInfo>(dataSourceInfo.Relationships) : new Dictionary<string, RelationshipInfo>();
            Fields = new Dictionary<string, FieldInfo>(dataSourceInfo.Fields);
            ParentFilterString = dataSourceInfo.ParentFilterString;
            ParentDataSourcePath = dataSourceInfo.ParentDataSourcePath;
        }

        public object Clone()
        {
            return new DataSourceInfo
            {
                Id = Id,
                Name = Name,
                Category = Category,
                ImageSource = ImageSource,
                Description = Description,
                Relationships = Relationships,
                Fields = new Dictionary<string, FieldInfo>(Fields),
                ParentFilterString = ParentFilterString,
                ParentDataSourcePath = ParentDataSourcePath
            };
        }
    }
}

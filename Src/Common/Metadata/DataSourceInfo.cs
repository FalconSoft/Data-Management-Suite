using System;
using System.Collections.Generic;
using System.Linq;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public class DataSourceInfo:BaseSourceInfo ,ICloneable
    {
        public DataSourceInfo()
        {
            Fields = new Dictionary<string, FieldInfo>();
            Relationships = new Dictionary<string, RelationshipInfo>();
            ServiceRelations = new Dictionary<string, ServiceSourceRelationship>();
        }
        public DataSourceInfo(IEnumerable<FieldInfo> fields)
        {
            Relationships = new Dictionary<string, RelationshipInfo>();
            ServiceRelations = new Dictionary<string, ServiceSourceRelationship>();
            Fields = fields.ToDictionary(f => f.Name, f => f);
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
        /// List of dependend Relations with ServiceSources
        /// </summary>
        public IDictionary<string, ServiceSourceRelationship> ServiceRelations { get; set; }

        /// <summary>
        /// Parent Source DataSourcePath
        /// </summary>
        public string ParentProviderString { get; set; }

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


        public void Update(DataSourceInfo dataSourceInfo)
        {
            Id = dataSourceInfo.Id;
            Name = dataSourceInfo.Name;
            Category = dataSourceInfo.Category;
            ImageSource = dataSourceInfo.ImageSource;
            Description = dataSourceInfo.Description;
            Relationships = dataSourceInfo.Relationships != null ? new Dictionary<string, RelationshipInfo>(dataSourceInfo.Relationships) : new Dictionary<string, RelationshipInfo>();
            ServiceRelations = dataSourceInfo.ServiceRelations != null ? new Dictionary<string, ServiceSourceRelationship>(dataSourceInfo.ServiceRelations) : new Dictionary<string, ServiceSourceRelationship>();
            Fields = new Dictionary<string, FieldInfo>(dataSourceInfo.Fields);
            ParentFilterString = dataSourceInfo.ParentFilterString;
            ParentProviderString = dataSourceInfo.ParentProviderString;
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
                ServiceRelations = ServiceRelations,
                Fields = new Dictionary<string, FieldInfo>(Fields),
                ParentFilterString = ParentFilterString,
                ParentProviderString = ParentProviderString
            };
        }
    }
}

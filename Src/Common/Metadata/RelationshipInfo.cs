using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    /// <summary>
    /// Represents a DataSource relationship between two data sources
    /// </summary>
    public class RelationshipInfo
    { 
        /// <summary>
        /// Relation Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Main SourceInfo DataSourcePath
        /// </summary>
        public string SourceInfoProviderString { get; set; }
        /// <summary>
        /// Related SourceInfo provider string to call data provider
        /// </summary>
        public string RelatedSourceInfoProviderString { get; set; }
        /// <summary>
        /// A mapping table for the fields from two data sources
        /// </summary>
        public Dictionary<string, string> MappedFields { get; set; }
    }

    public enum RelationType
    {
        Single, Multi
    }
}

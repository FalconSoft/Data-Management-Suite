using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    /// <summary>
    /// Represents a DataSource relationship between two data sources
    /// </summary>
    public class BaseRelationshipInfo 
    {
        public string Name { get; set; }
        /// <summary>
        /// Main SourceInfo DataSourcePath
        /// </summary>
        public string SourceInfoProviderString { get; set; }

        /// <summary>
        /// Related SourceInfo provider string to call data provider
        /// </summary>
        public string RelatedSourceInfoProviderString { get; set; }
    }

    /// <summary>
    /// Represents a DataSource relationship between two data sources
    /// </summary>
    public class RelationshipInfo : BaseRelationshipInfo
    {
        /// <summary>
        /// Foreign Field Id
        /// a field from main datasource what suppose to reference another datasource
        /// </summary>
        public string ForeignFieldName { get; set; }

        /// <summary>
        /// Field from related datasource
        /// Foreign Field from main data source will look up for primary field from related data source   
        /// </summary>
        public string PrimaryFieldName { get; set; }
    }

    /// <summary>
    /// Represent relationships between two data sources based on number of input parameters
    /// </summary>
    public class MultiColumnRelations : BaseRelationshipInfo
    {
        /// <summary>
        /// A mapping table for the fields from two data sources
        /// </summary>
        public Dictionary<string, string> InputFieldsMapping { get; set; }
    }
}

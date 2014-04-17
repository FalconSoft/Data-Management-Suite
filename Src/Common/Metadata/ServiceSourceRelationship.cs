using System.Collections.Generic;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    /// <summary>
    /// Class that describes Service Relationship
    /// </summary>
    public class ServiceSourceRelationship
    {
        /// <summary>
        /// Id of the Service Relationship
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Service Relationship Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Related DataSourceProviderString
        /// </summary>
        public string DataSourceProviderString { get; set; }

        /// <summary>
        /// Related ServiceSource DataSourcePath
        /// </summary>
        public string ServiceSourceProviderString { get; set; }

        /// <summary>
        /// List of Relation Elements 
        /// </summary>
        public IList<ServiceRelationElement> Relations { get; set; }
    }
}

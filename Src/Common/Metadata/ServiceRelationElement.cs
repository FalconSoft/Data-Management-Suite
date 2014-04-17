namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    /// <summary>
    /// Class That describes Relation element between Field and ServiceSource Field
    /// </summary>
    public class ServiceRelationElement
    {
        /// <summary>
        /// Id of ServiceSource Relation Element
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id of the Service Relationship
        /// </summary>
        public int RelationId { get; set; }

        /// <summary>
        /// Name of the Service Relationship
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Name of the Service Relationship Parameter
        /// </summary>
        public string ServiceParamName { get; set; }

        /// <summary>
        /// DataType of the Service Relationship Parameter
        /// </summary>
        public DataTypes KeyDataType { get; set; }

        /// <summary>
        /// Related Field
        /// </summary>
        public FieldInfo Field { get; set; }

        /// <summary>
        /// Converter to Convert Fielt Type to ServiceParam Type
        /// </summary>
        public string Converter { get; set; }
    }
}

using System;

namespace FalconSoft.ReactiveWorksheets.Common.Metadata
{
    public enum DataTypes
    {
        Null,
        String,
        Int,
        Double,
        Date,
        DateTime,
        Bool
    }

    /// <summary>
    /// class that describes field
    /// </summary>
    public class FieldInfo : ICloneable
    {
        public string Name { get; set; }
        /// <summary>
        /// Type of data
        /// </summary>
        public DataTypes DataType { get; set; }

        /// <summary>
        /// Size of field
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// Value will be set to DefaultValue, if not provided
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Indicates either is nullable
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Either is unique, within datasource
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Is ReadOnly
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Is Key or part of composite key
        /// </summary>
        public bool IsKey { get; set; }
        
        /// <summary>
        /// Either is Searchable field
        /// </summary>>
        public bool IsSearchable { get; set; }

        /// <summary>
        /// Urn of the relation assigned to the field
        /// </summary>
        public string RelationUrn { get; set; }

        /// <summary>
        /// Name of the related field
        /// </summary>
        public string RelatedFieldName { get; set; }

        /// <summary>
        /// Name of the owned dataSource
        /// </summary>
        public string DataSourceProviderString { get; set; }

        /// <summary>
        /// Used for Parent Fields
        /// </summary>
        public bool IsParentField { get; set; }


        public object Clone()
        {
            return new FieldInfo
            {
                Name = Name,
                DataType = DataType,
                Size = Size,
                DefaultValue = DefaultValue,
                IsNullable = IsNullable,
                IsUnique = IsUnique,
                IsReadOnly = IsReadOnly,
                IsKey = IsKey,
                IsSearchable = IsSearchable,
                RelationUrn = RelationUrn,
                RelatedFieldName = RelatedFieldName,
                DataSourceProviderString = DataSourceProviderString,
                IsParentField = IsParentField,
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

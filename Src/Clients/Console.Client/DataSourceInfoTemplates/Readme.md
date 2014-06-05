In the [DataSourceInfoTemplate.txt](../DataSourceInfoTemplate.txt) is a JSON - formated template for using in `<SchemaPath>` in FalconSoft.Data.Client command `create <SchemaPath> <UserName> <Password>`

Every DataSourceInfo must contain:

1. `Id` - DataSource id;
2. `Name` - Name of DataSource
3. `Category` - Category of DataSource
4. `DataSourcePath` - string that contains category and name in format `Category\\Name`
5. `Description` - Description of DataSourceInfo 
6. `ImageSource` - Path to Image
7. `AccessToOriginalData` - Allow using original data storage
8. `HistoryStorageType` - Method of recording information in history - `Buffer`, `Time`, `Event`
9. `HistoryStorageTypeParam` - History Storage Type Param (by default = 100)
10. `ParentDataSourcePath` - DataSourcePath to Parent DataSoruceInfo
11. `ParentFilterString` - Filter for Parent DataSource
12. `Relationships` - Dictionary of Relationships with other DataSources
13. Dictionary of `FieldInfos` where key is `Name` of `FieldInfo`

 
Structure of `FieldInfo`

1. `Name` - FieldInfo Name
2. `DataType` - Data type of FieldInfo
3. `Size` - Size of FieldInfo
4. `DefaultValue` - Value will be set to DefaultValue, if not provided
5. `IsNullable` - Indicates that FieldInfo is Nullable
6. `IsUnique` - Indicates that FieldInfo is Unique within the DataSourceInfo
7. `IsReadOnly` - Indicates that FieldInfo is ReadOnly
8. `IsKey` - Indicates that FieldInfo is A Key
9. `IsSearchable` - Indicates that FieldInfo is Searchable
10. `RelationUrn` - Urn to RelationInfo in `RelationShips`
11. `RelatedFieldName` - Name of FiledInfo from foreign DataSource to be showed
12. `DataSourceProviderString` - DataSourcePath
13. `IsParentField` - Indicates that FieldInfo is from parent DataSourceInfo

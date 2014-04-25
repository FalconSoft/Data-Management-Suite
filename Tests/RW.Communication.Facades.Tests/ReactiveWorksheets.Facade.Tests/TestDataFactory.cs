using System.Collections.Generic;
using System.IO;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace ReactiveWorksheets.Facade.Tests
{
    internal static class TestDataFactory
    {
        private static FieldInfo[] _fieldInfos =
        {
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = true,
                IsNullable = false,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "CustomerID",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = false,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "CompanyName",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "ContactName",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "ContactTitle",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Address",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "City",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Region",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "PostalCode",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Country",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Phone",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            }
            ,
            new FieldInfo
            {
                DataSourceProviderString = "Customers\\Northwind",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = true,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Fax",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            }
        };

        private static DataSourceInfo _testDataSourceInfo = new DataSourceInfo(_fieldInfos)
        {
            Category = "Customers",
            Description = "Test datasource",
            Name = "Northwind",
        };

        private static WorksheetInfo _tetsWorksheetInfo = new WorksheetInfo
            {
                Name = "TestDatasource Worksheet",
                Category = "Test",
                DataSourceInfo = _testDataSourceInfo,
                Columns = new List<ColumnInfo>(new []
                {
                    new ColumnInfo(_fieldInfos[0])
                    {
                        ColumnIndex = 0
                    }, 
                    new ColumnInfo(_fieldInfos[1])
                    {
                        ColumnIndex = 1
                    },
                    new ColumnInfo(_fieldInfos[2])
                    {
                        ColumnIndex = 2
                    } 
                })
            };

        private static List<Dictionary<string, object>> _dataList;

        private static User _testUser;

        internal static DataSourceInfo CreateTestDataSourceInfo()
        {
            return _testDataSourceInfo;
        }

        internal static WorksheetInfo CreateTestWorksheetInfo()
        {
            return _tetsWorksheetInfo;
        }

        internal static IEnumerable<Dictionary<string, object>> CreateTestData()
        {
            var keys = _fieldInfos.Select(f => f.Name).ToArray();
            var count = keys.Count();
            using (var sr = new StreamReader("Customers.txt"))
            {
                sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    var dictionary = new Dictionary<string, object>();

                    var str = sr.ReadLine().Split('\t');
                    for (int i = 0; i < count; i++)
                    {
                        dictionary.Add(keys[i],str[i]);
                    }
                    yield return dictionary;
                }
            }
            yield break;
        }

        internal static User CreateTestUser()
        {
            return _testUser ?? (_testUser = new User
            {
                LoginName = "TestUser",
            });
        }
    }
}

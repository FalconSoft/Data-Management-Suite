using System.Collections.Generic;
using System.IO;
using System.Linq;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;

namespace ReactiveWorksheets.Facade.Tests
{
    internal static class TestDataFactory
    {
        private static readonly FieldInfo[] FieldInfos =
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

        private static readonly DataSourceInfo TestDataSourceInfo = new DataSourceInfo(FieldInfos)
        {
            Category = "Customers",
            Description = "Test datasource",
            Name = "Northwind",
        };

        private static readonly WorksheetInfo TetsWorksheetInfo = new WorksheetInfo
            {
                Name = "TestDatasource Worksheet",
                Category = "Test",
                Columns = new List<ColumnInfo>(new []
                {
                    new ColumnInfo
                    {
                        ColumnIndex = 0,
                        FieldName = FieldInfos[0].Name
                    }, 
                    new ColumnInfo
                    {
                        ColumnIndex = 1,
                        FieldName = FieldInfos[1].Name
                    },
                    new ColumnInfo
                    {
                        ColumnIndex = 2,
                        FieldName = FieldInfos[2].Name
                    } 
                })
            };

        private static User _testUser;

        internal static DataSourceInfo CreateTestDataSourceInfo()
        {
            return TestDataSourceInfo;
        }

        internal static WorksheetInfo CreateTestWorksheetInfo()
        {
            return TetsWorksheetInfo;
        }

        internal static IEnumerable<Dictionary<string, object>> CreateTestData()
        {
            var keys = FieldInfos.Select(f => f.Name).ToArray();
            var count = keys.Count();
            using (var sr = new StreamReader("Customers.txt"))
            {
                sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    var dictionary = new Dictionary<string, object>();

                    var readLine = sr.ReadLine();
                    if (readLine != null)
                    {
                        var str = readLine.Split('\t');
                        for (var i = 0; i < count; i++)
                        {
                            dictionary.Add(keys[i],str[i]);
                        }
                    }
                    yield return dictionary;
                }
            }
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

using System.Collections.Generic;
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
                DataSourceProviderString = "Test\\TestDatasource",
                DataType = DataTypes.Int,
                DefaultValue = null,
                IsKey = true,
                IsNullable = false,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Id",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Test\\TestDatasource",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = false,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Name",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            },
            new FieldInfo
            {
                DataSourceProviderString = "Test\\TestDatasource",
                DataType = DataTypes.String,
                DefaultValue = null,
                IsKey = false,
                IsNullable = false,
                IsParentField = false,
                IsReadOnly = false,
                IsSearchable = true,
                IsUnique = false,
                Name = "Description",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            }
        };

        private static DataSourceInfo _testDataSourceInfo = new DataSourceInfo(_fieldInfos)
        {
            Category = "Test",
            Description = null,
            Name = "testDatasource",
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
            if (_dataList == null)
            {
                var list = new List<Dictionary<string, object>>();
                for (int i = 0; i < 20000; i++)
                {
                    var dictionary = new Dictionary<string, object>();
                    dictionary.Add("Id", i);
                    dictionary.Add("Name", "String " + i);
                    dictionary.Add("Description", "Description string " + i);
                    list.Add(dictionary);
                }
                _dataList = list;
            }
            return _dataList;
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

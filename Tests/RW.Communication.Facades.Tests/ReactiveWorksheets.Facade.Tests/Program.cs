using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using FalconSoft.Data.Management.Common.Security;
using FalconSoft.Data.Management.InProcessServer.Client;

namespace ReactiveWorksheets.Facade.Tests
{
    internal class Program
    {
        private static IFacadesFactory _facadesFactory;
        private const string ConnectionString = @"http://localhost:8081";
        private static ISecurityFacade _securityFacade;

        private static void Main()
        {
            _facadesFactory = GetFacadesFactory("RabbitMQ");

            _securityFacade = _facadesFactory.CreateSecurityFacade();


            var datasource = TestDataFactory.CreateTestDataSourceInfo();
            var user = TestDataFactory.CreateTestUser();

            Console.WriteLine("Testing starts...");

            user = TestSecurityfacade(user);

            Console.WriteLine("\n1. Create DataSource (Customers)");
            var dataSourceTest = new SimpleDataSourceTest(datasource, _facadesFactory, user);

            Console.WriteLine("\n2. Create 3 different worksheets referencing to the same data source with three different columns set and filter rules");
            var firstWorksheetColumns = new List<ColumnInfo>
            {
                new ColumnInfo{FieldName = "CustomerID"},
                new ColumnInfo{FieldName = "CompanyName"},
                new ColumnInfo{FieldName = "ContactName"}
            };
            var firstWorksheet = dataSourceTest.CreateWorksheetInfo("First Worksheet", "Test", firstWorksheetColumns, user);

            var secondWorksheetColumns = new List<ColumnInfo>
            {
                new ColumnInfo{FieldName = "CustomerID"},
                new ColumnInfo{FieldName = "ContactTitle"},
                new ColumnInfo{FieldName = "Address"}
            };
            var secondWorksheet = dataSourceTest.CreateWorksheetInfo("Second Worksheet", "Test", secondWorksheetColumns, user);
            var thirdWorksheetColumns = new List<ColumnInfo>
            {
                new ColumnInfo{FieldName = "CustomerID"},
                new ColumnInfo{FieldName = "City"},
                new ColumnInfo{FieldName = "Region"}
            };
            var thirdWorksheet = dataSourceTest.CreateWorksheetInfo("Third Worksheet", "Test", thirdWorksheetColumns, user);

            Console.WriteLine("\n3. Submit data (from Tsv)");
            var data = TestDataFactory.CreateTestData("..\\..\\Customers.txt");

            var changedData = data as Dictionary<string, object>[] ?? data.ToArray();
            dataSourceTest.SubmitData("test data save", changedData);

            Console.WriteLine("\n4. GetData");
            var getData = dataSourceTest.GetData();
            Console.WriteLine("Saved data count : {0}", getData.Count());

            Console.WriteLine("\n5. Subscribe on changes and submit a few modified rows. Make sure you get proper updates.");
            var disposer = dataSourceTest.GetDataChanges().Subscribe(GetDataChanges);

            changedData[0]["CompanyName"] = "New value";
            changedData[1]["CompanyName"] = "New value";
            changedData[2]["CompanyName"] = "New value";

            dataSourceTest.SubmitData("Make changes", changedData.Take(3));

            Console.WriteLine("\n6. Check history for modified records");
            var firstRecordHistory = dataSourceTest.GetHistory(datasource.GetKeyFieldsName().Aggregate("", (cur, key) => cur + "|" + changedData[0][key]));
            Console.WriteLine("First Record History count : {0}", firstRecordHistory.Count());
            var secondRecordHistory = dataSourceTest.GetHistory(datasource.GetKeyFieldsName().Aggregate("", (cur, key) => cur + "|" + changedData[0][key]));
            Console.WriteLine("Second Record History count : {0}", secondRecordHistory.Count());
            var thirdtRecordHistory = dataSourceTest.GetHistory(datasource.GetKeyFieldsName().Aggregate("", (cur, key) => cur + "|" + changedData[0][key]));
            Console.WriteLine("Third Record History count : {0}", thirdtRecordHistory.Count());

            Console.WriteLine("\n7. Make changes to DataSourcenfo add fields");
            var addField = new FieldInfo
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
                Name = "NewField",
                RelatedFieldName = null,
                RelationUrn = null,
                Size = null
            };

            datasource.Fields.Add(addField.Name, addField);
            datasource = dataSourceTest.UpdateDataSourceInfo(datasource, user);

            Console.WriteLine("\n8. Get Data and see what is there");
            getData = dataSourceTest.GetData();
            var getDataArray = getData as Dictionary<string, object>[] ?? getData.ToArray();
            Console.WriteLine("Updated dataSource data count : {0}", getDataArray.Count());
            var updatedDatasourceKeys = getDataArray.First().Keys;
            Console.WriteLine("Data keys {0}", updatedDatasourceKeys.Aggregate("", (cur, key) => cur + " : [" + key + "]"));
            Console.WriteLine("Datasource field keys {0}", datasource.Fields.Keys.Aggregate("", (cur, key) => cur + " : [" + key + "]"));

            Console.WriteLine("\n7. Make changes to DataSourcenfo remove fields");
            datasource.Fields.Remove("Region");

            datasource = dataSourceTest.UpdateDataSourceInfo(datasource, user);
            Console.WriteLine("\n8. Get Data and see what is there");
            getData = dataSourceTest.GetData();
            
            getDataArray = getData as Dictionary<string, object>[] ?? getData.ToArray();
            Console.WriteLine("Updated dataSource data count : {0}", getDataArray.Count());
            updatedDatasourceKeys = getDataArray.First().Keys;
            Console.WriteLine("Data keys {0}", updatedDatasourceKeys.Aggregate("", (cur, key) => cur + " : [" + key + "]"));
            Console.WriteLine("Datasource field keys {0}", datasource.Fields.Keys.Aggregate("", (cur, key) => cur + " : [" + key + "]"));

            Console.WriteLine("\n9. Delete records");
            
            dataSourceTest.RemoveWorksheet(firstWorksheet, user);
            dataSourceTest.RemoveWorksheet(secondWorksheet, user);
            dataSourceTest.RemoveWorksheet(thirdWorksheet, user);
            
            var keyFields = datasource.GetKeyFieldsName();
            var datakeys = changedData.Select(record => keyFields.Aggregate("", (cur, key) => cur + "|" + record[key]));

            disposer.Dispose();

            dataSourceTest.SubmitData("Remove test data", null, datakeys);
            dataSourceTest.RemoveDatasourceInfo(user);
            dataSourceTest.Dispose();

            _securityFacade.RemoveUser(user, user.Id);
            Console.WriteLine("\nTest finish. Type <Enter> to exit.");
            Console.ReadLine();
        }

        private static void GetDataChanges(RecordChangedParam[] obj)
        {
            foreach (var recordChangedParam in obj)
            {
                Console.WriteLine("RecordChangedParam resived RecordKey : {0} OriginalRecordKey : {1} dataDourcePath : {2}", recordChangedParam.RecordKey, recordChangedParam.OriginalRecordKey, recordChangedParam.ProviderString);
            }
        }

        private static User TestSecurityfacade(User user)
        {
            Console.WriteLine("Step #1. Create test user.");
            _securityFacade.SaveNewUser(user, UserRole.User, user.Id);

            Console.WriteLine("Cheacking if user is created...");
            var allUsers = _securityFacade.GetUsers(user.Id);
            if (allUsers.ToList().Exists(u => u.LoginName == user.LoginName))
            {
                Console.WriteLine("Insert successfull");
                user = allUsers.FirstOrDefault(u => u.LoginName == user.LoginName);
            }
            return user;
        }

        private static IFacadesFactory GetFacadesFactory(string facadeType)
        {
            if (facadeType.Equals("RabbitMQ"))
            {
                //return new RabbitMQFacadeFactory();
            }
            if (facadeType.Equals("InProcess", StringComparison.OrdinalIgnoreCase))
            {
                // this is hardcode for testing only
                const string metaDataPersistenceConnectionString = "mongodb://localhost/rw_metadata";
                const string persistenceDataConnectionString = "mongodb://localhost/rw_data";
                const string mongoDataConnectionString = "mongodb://localhost/MongoData";
                const string connectionString = @"http://localhost:8081/";
                const string dataSources = @"..\..\..\DataSources\SampleDataSources\bin\Debug\;..\..\..\DataSources\DefaultMongoDbSource\bin\Debug\;..\..\..\DataSources\EDI\bin\Debug\;";
                return new InProcessServerFacadesFactory(metaDataPersistenceConnectionString, persistenceDataConnectionString, mongoDataConnectionString, connectionString, dataSources);
            }
            throw new ConfigurationErrorsException("Unsupported facade type - >" + facadeType);
        }
    }
}


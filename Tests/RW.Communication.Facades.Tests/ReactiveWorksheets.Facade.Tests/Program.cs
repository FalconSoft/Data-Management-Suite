using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Client.SignalR;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;
using FalconSoft.ReactiveWorksheets.Common.Security;
using FalconSoft.ReactiveWorksheets.InProcessServer.Client;

namespace ReactiveWorksheets.Facade.Tests
{
    internal class Program
    {
        private static IFacadesFactory _facadesFactory;
        private const string ConnectionString = @"http://localhost:8081";
        private static ISecurityFacade _securityFacade;
        private static IMetaDataAdminFacade _metaDataAdminFacade;
        private static ICommandFacade _commandFacade;
        private static IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private static void Main()
        {
            _facadesFactory = GetFacadesFactory("InProcess");
            _metaDataAdminFacade = _facadesFactory.CreateMetaDataAdminFacade();
            _securityFacade = _facadesFactory.CreateSecurityFacade();
            _metaDataAdminFacade.ObjectInfoChanged += ObjectInfoChanged;
            _reactiveDataQueryFacade = _facadesFactory.CreateReactiveDataQueryFacade();
            _commandFacade = _facadesFactory.CreateCommandFacade();

            var datasource = TestDataFactory.CreateTestDataSourceInfo();
            var worksheet = TestDataFactory.CreateTestWorksheetInfo();
            var data = TestDataFactory.CreateTestData();
            var user = TestDataFactory.CreateTestUser();

            Console.WriteLine("Testing starts...");
            
            user = TestSecurityfacade(user);

            datasource = TestMetaDataFacadeDataSourceInfo(datasource, user);

            worksheet = TestMetaDataFacadeWorksheetInfo(worksheet, user);

            SaveDataIntoDatasourceTest(data,datasource,"Test data");

            var keyFields = datasource.GetKeyFieldsName();
            var dataKeys = data.Select(record => keyFields.Aggregate("", (cur, key) => cur + "|" + record[key]));
            
            RemoveTestData(worksheet, datasource, user, dataKeys);

            DisposeAllConnections();

            Console.WriteLine("Test finish. Type <Enter> to exit.");
            Console.ReadLine();
        }

        private static void DisposeAllConnections()
        {
            Console.WriteLine("\nStep #6. Dispose all connections");
            _commandFacade.Dispose();
            _metaDataAdminFacade.Dispose();
            _reactiveDataQueryFacade.Dispose();
            _securityFacade.Dispose();
        }

        private static void RemoveTestData(WorksheetInfo worksheetInfo, DataSourceInfo dataSourceInfo, User user,
            IEnumerable<string> dataKeys)
        {
            Console.WriteLine("\nStep #5. Remove test data.");
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            _commandFacade.SubmitChanges(dataSourceInfo.DataSourcePath,"Remove test data",null,dataKeys, r =>
            {
                Console.WriteLine("Test data removed successfull");
                tcs.SetResult(r);
            }, ex =>
            {
                Console.WriteLine("Test data remove failed");
                tcs.SetException(ex);
            });
            task.Wait();

            Console.WriteLine("Try to get test data");
            var removeData = _reactiveDataQueryFacade.GetData(dataSourceInfo.DataSourcePath);
            
            if (!removeData.Any())
            {
                Console.WriteLine("All test data removed");
            }

            _metaDataAdminFacade.DeleteWorksheetInfo(worksheetInfo.DataSourcePath,user.Id);
            _metaDataAdminFacade.DeleteDataSourceInfo(dataSourceInfo.DataSourcePath, user.Id);
            _securityFacade.RemoveUser(user);
        }

        private static void SaveDataIntoDatasourceTest(IEnumerable<Dictionary<string, object>> data,DataSourceInfo dataSourceInfo,string comment)
        {
            Console.WriteLine("\nStep #4. Save data");
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            var changedRecords = data as IList<Dictionary<string, object>> ?? data.ToList();

            _commandFacade.SubmitChanges(dataSourceInfo.DataSourcePath, comment, changedRecords,null,
                r =>
                {
                    Console.WriteLine("Data saved successfull");
                    tcs.SetResult(r);
                }, ex =>
                {
                    Console.WriteLine("Data save failed");
                    tcs.SetException(ex);
                });

            task.Wait();

            Console.WriteLine("Get saved data");
            var savedData = _reactiveDataQueryFacade.GetData(dataSourceInfo.DataSourcePath);

            if (savedData.Count() == changedRecords.Count)
            {
                Console.WriteLine("All data saved");
            }
        }


        private static WorksheetInfo TestMetaDataFacadeWorksheetInfo(WorksheetInfo worksheetInfo, User user)
        {
            Console.WriteLine("\nStep #3. Create worksheetInfo");
            _metaDataAdminFacade.CreateWorksheetInfo(worksheetInfo, user.Id);

            Console.WriteLine("Get created worksheetInfo");
            var worksheet = _metaDataAdminFacade.GetWorksheetInfo(worksheetInfo.DataSourcePath);
            return worksheet;
        }

        private static DataSourceInfo TestMetaDataFacadeDataSourceInfo(DataSourceInfo dataSourceInfo, User user)
        {
            Console.WriteLine("\nStep #2. Create test datasource");
            _metaDataAdminFacade.CreateDataSourceInfo(dataSourceInfo, user.Id);

            Console.WriteLine("Get created datasource");
            var datasource = _metaDataAdminFacade.GetDataSourceInfo(dataSourceInfo.DataSourcePath);
            return datasource;
        }

        private static void ObjectInfoChanged(object sender, SourceObjectChangedEventArgs e)
        {
            Console.WriteLine("ChangedObjectType : {0}\n ChangeObjectUrl : {1}", e.ChangedObjectType,
                e.OldObjectUrn);
        }

        private static User TestSecurityfacade(User user)
        {
            Console.WriteLine("Step #1. Create test user.");
            _securityFacade.SaveNewUser(user);

            Console.WriteLine("Cheacking if user is created...");
            var allUsers = _securityFacade.GetUsers();
            if (allUsers.Exists(u => u.LoginName == user.LoginName))
            {
                Console.WriteLine("Insert successfull");
                user = allUsers.FirstOrDefault(u => u.LoginName == user.LoginName);
            }
            return user;
        }

        private static IFacadesFactory  GetFacadesFactory(string facadeType)
        {
            if (facadeType.Equals("SignalR", StringComparison.OrdinalIgnoreCase))
            {
                return new SignalRFacadesFactory(ConnectionString);
            }
            if (facadeType.Equals("InProcess", StringComparison.OrdinalIgnoreCase))
            {
                // this is hardcode for testing only
                const string metaDataPersistenceConnectionString = "mongodb://localhost/rw_metadata";
                const string persistenceDataConnectionString = "mongodb://localhost/rw_data";
                const string mongoDataConnectionString = "mongodb://localhost/MongoData";

                return new InProcessServerFacadesFactory(metaDataPersistenceConnectionString, persistenceDataConnectionString, mongoDataConnectionString);
            }
            throw new ConfigurationException("Unsupported facade type - >" + facadeType);
        }
    }
}


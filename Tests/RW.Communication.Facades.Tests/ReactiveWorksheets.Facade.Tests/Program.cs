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
            Console.WriteLine("Step #1. Create test user.");

            user = TestSecurityfacade(user);

            datasource = TestMetaDataFacadeDataSourceInfo(datasource, user);

            worksheet = TestMetaDataFacadeWorksheetInfo(worksheet, user);

            SaveDataIntoDatasource(data,datasource,"Test data");

            Console.WriteLine("Test finish. Type <Enter> to exit.");
            Console.ReadLine();
        }

        private static void SaveDataIntoDatasource(IEnumerable<Dictionary<string, object>> data,DataSourceInfo dataSourceInfo,string comment)
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


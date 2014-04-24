using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Client.SignalR;
using FalconSoft.ReactiveWorksheets.Common.Facade;
using FalconSoft.ReactiveWorksheets.Common.Metadata;

namespace ReactiveWorksheets.Facade.Tests
{
    internal class Program
    {
        private static IFacadesFactory _facadesFactory;
        private const string ConnectionString = @"http://localhost:8081";
       

        private static void Main()
        {
            _facadesFactory = new SignalRFacadesFactory(ConnectionString);
            var metadatafacade = _facadesFactory.CreateMetaDataAdminFacade();
            var securityFacade = _facadesFactory.CreateSecurityFacade();
            var reactiveDataProvider = _facadesFactory.CreateReactiveDataQueryFacade();
            var commandFacade = _facadesFactory.CreateCommandFacade();

            var datasource = TestDataFactory.CreateTestDataSourceInfo();
            var worksheet = TestDataFactory.CreateTestWorksheetInfo();
            var data = TestDataFactory.CreateTestData();
            var user = TestDataFactory.CreateTestUser();

            Console.WriteLine("Testing starts...");
            Console.WriteLine("Step #1. Create test user.");
            securityFacade.SaveNewUser(user);
            var userArray = securityFacade.GetUsers();
            Console.WriteLine("Cheacking if user is created...");
            var updateUser = userArray.Find(u => u.LoginName == user.LoginName);

            while (updateUser == null)
            {
                Console.WriteLine("User has not created yet...");
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                Console.WriteLine("Try again");
                userArray = securityFacade.GetUsers();
                updateUser = userArray.Find(u => u.LoginName == user.LoginName);
            }
            
            Console.WriteLine("User saved");
            user = updateUser;
            
            Console.WriteLine("\nStep #2. Create test datasource");
            metadatafacade.CreateDataSourceInfo(datasource, user.Id);
            
            Console.WriteLine("Checking if datasource is created and saved");
            var datasourceArray = metadatafacade.GetAvailableDataSources(user.Id);
            
            var ds = datasourceArray.FirstOrDefault(d => d.DataSourcePath == datasource.DataSourcePath);
            while (ds==null)
            {
                Console.WriteLine("Datasource has not created yet");
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                Console.WriteLine("Try again...");
                datasourceArray = metadatafacade.GetAvailableDataSources(user.Id);
                ds = datasourceArray.FirstOrDefault(d => d.DataSourcePath == datasource.DataSourcePath);
            }
            
            Console.WriteLine("DataSourceInfo saved");
            datasource = ds;

            Console.WriteLine("\nStep #3. Create worksheetInfo");
            metadatafacade.CreateWorksheetInfo(worksheet,user.Id);

            var worksheetArray = metadatafacade.GetAvailableWorksheets(user.Id);

            var ws = worksheetArray.FirstOrDefault(d => d.DataSourcePath == datasource.DataSourcePath);
            while (ws == null)
            {
                Console.WriteLine("Worksheet has not created yet");
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                Console.WriteLine("Try again...");
                worksheetArray = metadatafacade.GetAvailableWorksheets(user.Id);
                ws = worksheetArray.FirstOrDefault(w=> w.Name == worksheet.Name);
            }

            Console.WriteLine("Worksheet saved");
            worksheet = ws;

            Console.WriteLine("\nStep #4. Submit data");
            var changedRecords = data as Dictionary<string, object>[] ?? data.ToArray();

            //start when all test data is submited
            Action action = ()=>
            {
                action = null;
                Console.WriteLine("GetData");
               
                var getData = reactiveDataProvider.GetData(datasource.DataSourcePath).ToArray();

                while (getData.Count() < changedRecords.Count())
                {
                    Console.WriteLine("Not all data submited");
                    Thread.Sleep(TimeSpan.FromMilliseconds(3000));
                    Console.WriteLine("Try again getData");
                    getData = reactiveDataProvider.GetData(datasource.DataSourcePath).ToArray();
                    Console.WriteLine("Data count : {0}", getData.Count());
                }

                if (getData.Count() == changedRecords.Count())
                {
                    Console.WriteLine("Data count : {0}", getData.Count());
                }

                else Console.WriteLine("Not all data sumbited submited count : {0}  #getData count : {1}",
                        changedRecords.Count(), getData.Count());

                Console.WriteLine("\nStep #5. Remove WorksheetInfo");
                metadatafacade.DeleteWorksheetInfo(worksheet.DataSourcePath, user.Id);
                Thread.Sleep(TimeSpan.FromMilliseconds(150));

                // start when all test data is removed
                Action a = () =>
                {
                    a = null;
                    var cheackIfDataExists = reactiveDataProvider.GetData(datasource.DataSourcePath).ToArray();

                    while (cheackIfDataExists.Any())
                    {
                        Console.WriteLine("Not all data removed");
                        Thread.Sleep(TimeSpan.FromMilliseconds(3000));
                        Console.WriteLine("Try again getData");
                        cheackIfDataExists = reactiveDataProvider.GetData(datasource.DataSourcePath).ToArray();
                        Console.WriteLine("Data count : {0}", cheackIfDataExists.Count());
                    }

                    Console.WriteLine("\nStep #7. Remove DatasourceInso");
                    metadatafacade.DeleteDataSourceInfo(datasource.DataSourcePath, user.Id);
                    
                    Console.WriteLine("\nStep #8. Remove user");
                    securityFacade.RemoveUser(user);
                    
                    Console.WriteLine("\nStep #9. Close connections");

                    Thread.Sleep(TimeSpan.FromMilliseconds(300));
                    
                    reactiveDataProvider.Dispose();
                    commandFacade.Dispose();
                    metadatafacade.Dispose();
                    securityFacade.Dispose();

                    Console.WriteLine("Test finish. Type <Enter> to exit.");
                };

                Console.WriteLine("\nStep #6. Remove submited data");
                commandFacade.SubmitChanges(datasource.DataSourcePath, "Some comment", null, changedRecords.Select(record => datasource.GetKeyFieldsName().Aggregate("", (cur, key) => cur + "|" + record[key])),
                    r =>
                    {
                        Console.WriteLine("Test data Removed");
                        a();
                    });
              

                
            };

            Console.WriteLine("Start data submit.");
            commandFacade.SubmitChanges(datasource.DataSourcePath, "some comment", changedRecords, null,
                revision =>
                {
                    Console.WriteLine("Revision returned");
                    action();
                }, 
                ex => Console.WriteLine("Exception returned"));

           
           
           
          
            Console.ReadLine();

            
        }

    }
}


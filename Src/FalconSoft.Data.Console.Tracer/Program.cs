using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using FalconSoft.Data.Management.Client.RabbitMQ;
using FalconSoft.Data.Management.Common;
using FalconSoft.Data.Management.Common.Facades;
using FalconSoft.Data.Management.Common.Metadata;
using SC = System.Console;

namespace FalconSoft.Data.Console.Tracer
{
    class Program
    {
        private static string _userToken;
        private static string _folderLocation;
        private const bool OverrideExistingFiles = true;
        private static IReactiveDataQueryFacade _reactiveDataQueryFacade;
        private static List<IDisposable> _disposables = new List<IDisposable>(); 

        static void Main()
        {
            const string serverUrl = "localhost";
            const string login = "RWClient";
            const string password = "RWClient";

            const string userLogin = "Admin";
            const string userPassword = "Admin";

            var facadeFactory = new RabbitMqFacadesFactory(serverUrl, login, password);

            var securityFacade = facadeFactory.CreateSecurityFacade();

            _userToken = securityFacade.Authenticate(userLogin, userPassword).Value;

            if (string.IsNullOrEmpty(_userToken))
            {
                SC.WriteLine("User is not loged in!"+ Environment.NewLine +" Pres <Any key> to exit console");
                SC.ReadKey(true);
                return;
            }

            SC.WriteLine("Please, specify the folder location, where data will be saved.");
            _folderLocation = SC.ReadLine();

            while (string.IsNullOrEmpty(_folderLocation) || !Directory.Exists(_folderLocation))
            {
                SC.WriteLine("Can't find specified directory. Try again or type <Exit> to close program");
                
                _folderLocation = SC.ReadLine();
                
                if (string.Equals(_folderLocation, "Exit", StringComparison.OrdinalIgnoreCase))
                {
                    SC.WriteLine("Bye...");
                    facadeFactory.Dispose();
                    return;
                }
            }

            var metadataFacade = facadeFactory.CreateMetaDataFacade();
            
            _reactiveDataQueryFacade = facadeFactory.CreateReactiveDataQueryFacade();

            var dataSourceList = metadataFacade.GetAvailableDataSources(_userToken);

            _disposables = new List<IDisposable>(dataSourceList.Length);

            foreach (var dataSourceInfo in dataSourceList)
            {
                var disposer = SubscribeOnDataSourceChanges(dataSourceInfo, _reactiveDataQueryFacade, _userToken, _folderLocation, OverrideExistingFiles);
                _disposables.Add(disposer);
            }

            metadataFacade.ObjectInfoChanged += GetObjectInfoChanges;

            SC.WriteLine("Type <Exit> to close console.");
            
            var command = string.Empty;
            
            while (!string.Equals(command, "Exit", StringComparison.OrdinalIgnoreCase))
            {
                command = SC.ReadLine();
            }

            SC.WriteLine("Bye...");

            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            metadataFacade.ObjectInfoChanged -= GetObjectInfoChanges;

            facadeFactory.Dispose();
        }

        private static void GetObjectInfoChanges(object sender, SourceObjectChangedEventArgs e)
        {
            if (e.ChangedActionType == ChangedActionType.Create)
            {
                if (e.ChangedObjectType == ChangedObjectType.DataSourceInfo)
                {
                    var disposer = SubscribeOnDataSourceChanges(e.SourceObjectInfo as DataSourceInfo, _reactiveDataQueryFacade, _userToken, _folderLocation, OverrideExistingFiles);
                    _disposables.Add(disposer);
                }
            }
        }

        private static IDisposable SubscribeOnDataSourceChanges(DataSourceInfo dataSourceInfo, IReactiveDataQueryFacade reactiveDataQueryFacade, string userToken, string folderLocation, bool overrideExistingFiles)
        {
            var dataSourcePath = dataSourceInfo.DataSourcePath;

            var fileName = string.Format(@"{0}\{1}.txt", folderLocation, dataSourcePath);
            var disrectory = string.Format(@"{0}\{1}", folderLocation, dataSourceInfo.Category);

            if (File.Exists(fileName))
            {
                if (overrideExistingFiles)
                    File.WriteAllText(fileName, string.Empty);
            }
            else
            {
                if (!Directory.Exists(disrectory))
                    Directory.CreateDirectory(disrectory);

                File.Create(fileName);
            }
            
            var fileWriter = new FileWriter(fileName, true);

            var disposer = reactiveDataQueryFacade
                .GetDataChanges(userToken, dataSourcePath)
                .Subscribe(fileWriter.WriteRecords);
            
            SC.WriteLine("Subscribed On {0} datasource changes...", dataSourceInfo.Name);

            return disposer;
        }

        private class FileWriter
        {
            private readonly string _fileName;
            private readonly bool _append;
            private readonly JavaScriptSerializer _scriptSerializer = new JavaScriptSerializer();
            private readonly object _lockThis = new object();

            public FileWriter(string fileName, bool append)
            {
                _fileName = fileName;
                _append = append;
            }

            internal void WriteRecords(RecordChangedParam[] recordChangedParam)
            {
                lock (_lockThis)
                {
                    using (var streamWriter = new StreamWriter(_fileName, _append))
                    {
                        foreach (var changedParam in recordChangedParam)
                        {
                            string recordsSb;
                            if (changedParam.ChangedAction == RecordChangedAction.AddedOrUpdated)
                            {
                                var anonItem = new
                                {
                                    DateTime = DateTime.Now.ToString("G"),
                                    changedParam.RecordKey,
                                    changedParam.RecordValues
                                };

                                recordsSb = _scriptSerializer.Serialize(anonItem) + Environment.NewLine;
                            }
                            else
                            {
                                var anonItem = new
                                {
                                    DateTime = DateTime.Now.ToString("G"),
                                    changedParam.RecordKey,
                                    ChangedAction = changedParam.ChangedAction.ToString()
                                };

                                recordsSb = _scriptSerializer.Serialize(anonItem) + Environment.NewLine;
                            }


                            streamWriter.Write(recordsSb);
                        }
                    }
                }
            }
        }
    }
}

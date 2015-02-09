[Falconsoft Ltd@ 2014](http://falconsoft-ltd.com/)
## How to Install and Run FalconSoft Data Management Suite Server.

Сonfigure the `FalconSoft.Data.Server.exe.config` file:

Find the section <appSettings>

```xml
  <appSettings>
    <add key="ConnectionString" value="http://localhost:8080/" />
    <add key="MetaDataPersistenceConnectionString" value="mongodb://localhost/rw_metadata" />
    <add key="PersistenceDataConnectionString" value="mongodb://localhost/rw_data" />
    <add key="MongoDataConnectionString" value="mongodb://localhost/MongoData" />
    <add key="CatalogDlls" value=".\DataSources\" />
  </appSettings>
```

   `Connection String` path to connect to server;
   
   `MetaDataPersistenceConnectionString`, `PersistenceDataConnectionString`, `MongoDataConnectionString` - path to DataBase tables;
   
   `CatalogDlls` - path to directory with your own dataSources. If you want to have an relative path you have to start path with `.\`. If you need to have more than one path you can separate if with `;` symbol;

   ```
   netsh http add urlacl url=http://+:8080/ user=machine\username
   ```

### After Configuring the config file you can run your server as:

1. A Console Application.

   To run Server as a console application Run the `FalconSoft.Data.Server.exe` file as Administrator.


2. A Windows Service.

   Run the `InstallService.bat` file as Administrator to instal Falconsoft_Data_Server;
   
   Enter your login and path to install service with administrator rights.

   To uninstall service run `UnInstallService.bat` file as Administrator

------------
The log information about server state is in the `log-file.txt`;

The log information about Service installing process is in `FalconSoft.Data.Server.InstallLog` and in `InstallUtil.InstallLog` files

The log information about Service installing state is in `FalconSoft.Data.Server.InstallState`

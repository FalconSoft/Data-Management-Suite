Falconsoft Ltd@ 2014
Reactive Worksheet Console Client application designed to comunicate with Falconsoft Data Sever troght console interface.

To run application please set url to server for ConnectionString in <appSettings> section and than run the executable.

Following commans allow you to comunicate with server:

-help show commands available in application and description how to use them
-exit exit from application


-get <DataSource Name> <Output File Name> [FilterRules] [Separator]
where 
	<DataSource name> - required parameter, format category\datasource name. Datasource name from which data will be downloaded
	<Output File Name> - required parameter, the file name where data will be downloaded
	[FilterRules] - optional parameter, filter base on which client will get data
	[separator] -  optional parameter, delimeter in CSV data file(s). Default to TAB

--submit <update filename> <delete filename> <DataSource name> [comment] [separator]
where 
	 <update filename> - required parameter, file what has to be uploaded
	 <delete filename> - required parameter, file wich contains RecordKeys to be deleted
	 <DataSource name> - required parameter, forma category\datasource name. Datasource where data will be submited
	 [comment] - optional parameter, comment 
	 [separator] -  optional parameter, delimeter in CSV data file(s). Default to TAB

-subscribe <DataSource name> <filename> [FilterRules] [separator]
where:
	<DataSource name> - required parameter, forma category\datasource name. Datasource where data will be submited
	<filename> - required parameter, name of the file where output will be dumped
	[FilterRules] - optional parameter, filter base on which client will get updates
	[separator] -  optional parameter, delimeter in CSV data file(s). Default to TAB


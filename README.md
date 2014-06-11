FalconSoft Data Management Suite
==================

FalconSoft Data Management Suite is a set of reusable data management and analysis components designed and built by FalconSoft to significantly speed up and simplify development of real-time data management solutions within an enterprise.

FalconSoft Data Management Suite' reference architecture and set of reusable components aimed to cover most common enterprise data management tasks, and allowing developers to focus rather on business tasks instead of technology issues.

###Core feadures
 - Real-time data updates
 - Data Virtualization - new way to consolidate data from different sources in real-time.
 - Versioned Data Repository
 - Full Audit trail
 - Security / control
 - Customizable WPF GUI with high frequent and real-time updates
 - Advanced search
for more information look into...

###Project Structure
FalconSoft Data Management Suite is organized into several high level assemblies

####Common
 - **FalconSoft.Data.Management.Common** - common assembly that contains all main interfaces and base classes used in data virtualization server as well as GUI.
 - **FalconSoft.Data.Management.Components** - core components responsible for processing real-time events, consolidate and validate messages.


####Server
 - **FalconSoft.Data.Server** - source code for bootstrapping data virtualization server
 - **FalconSoft.Data.Server.Persistence** - project responsible for persisting objects in data repository

####DataSources
 - **MongoDb.DataSource** - a primary implementation of data source what allow us to store generic (user created) data.
 - **Sample.DataSources** - sample data sources

####Communications
FalconSoft Data Server implementation does not depend on any specific communication technology. Even more, we made it easy to implement communication level with differend kind of communication protocols, what better suits your infrastructure.

- SignalR
 * **Client.SignalR** - Client assembly what implements facades communication with [Microsoft SignalR](http://www.asp.net/signalr). 
 * **Server.SignalR** Server side assembly what implements facades communication with [Microsoft SignalR](http://www.asp.net/signalr) 
- InProcess
A simple library what references server-side logic into clients AppDomain. It doesn't have any communication overhead and acts as a fat client. Mainly for testing or when no server side infrastructure. 

####Clients
 - **FalconSoft.Data.Console** - a simple console application to work with FalconSoft Data Server
 - **Reactive Worksheets** - A generic and customizable Wpf application, what was designed and built to visualize and manage data sources in real-time. It is not open source yet!

##Resources
 - [web site - www.falconsoft-ltd.com/data-management-suite](http://www.falconsoft-ltd.com/data-management-suite)
 - [Licensing](https://github.com/FalconSoft/Data-Management-Suite/wiki/Licensing)
 - [Getting Started](https://github.com/FalconSoft/Data-Management-Suite/wiki/Getting-Started)
 - [Developer's Wiki page](https://github.com/FalconSoft/Data-Management-Suite/wiki)
 - [Architecture Overview](https://github.com/FalconSoft/Data-Management-Suite/wiki/Architecture-Overview)
 - [Release Notes](https://github.com/FalconSoft/Data-Management-Suite/wiki/Release-Notes)
 - [Future plans / Roadmap](https://github.com/FalconSoft/Data-Management-Suite/wiki/Roadmap)


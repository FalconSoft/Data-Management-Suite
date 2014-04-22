ReactiveWorksheets
==================

Reactive Worksheets is a set of reusable data management and analysis components designed and built by FalconSoft to significantly speed up and simplify development of real-time data management solutions within an enterprise.

Reactive Worksheets' reference architecture and set of reusable components aimed to cover most common enterprise data management tasks, and allowing developers to focus rather on business tasks instead of technology issues.

###Core feadures
 - Real-time data updates
 - Data Virtualization
 - Security / control
 - Full Audit trail
 - Customizable WPF GUI with high frequent and real-time updates
 - Advanced search
for more information look into...

###Project Structure
ReactiveWorksheets platform is organized into several high level assemblies

####Common
 - **ReactiveWorksheets.Common** - common assembly that contains all main interfaces and base classes used in data virtualization server as well as GUI.

####Server
 - **ReactiveWorksheets.Server** - data virtualization server source code
 - **ReactiveWorksheets.Server.Bootstrapper** - source code for bootstrapping data virtualization server
 - **ReactiveWorksheets.Server.Persistence** - project responsible to persist objects within

####DataSources
 - **MongoDb.DataSource**
 - **Sample.DataSources**

####Communications
Reactive Worksheets server side implementation does not depend on any specific communication technology. Even more, we made it easy to implement communication level with differend kind of communication protocols, what better suits your infrastructure.

- SignalR
 * **Client.SignalR** - Client assembly what implements facades communication with [Microsoft SignalR](http://www.asp.net/signalr). 
 * **Server.SignalR** Server side assembly what implements facades communication with [Microsoft SignalR](http://www.asp.net/signalr) 
- InProcess
A simple library what references server-side logic into clients AppDomain. It doesn't have any communication overhead and acts as a fat client. Mainly for testing or when no server side infrastructure. 

####Clients
 - **ReactiveWorksheets.Console**
 - **ReactiveWorksheets** - Real-time and customizable Wpf application. Is not open source yet!

###NuGet Packages

###Current Release
 - release is comming

###Future plans / Roadmap

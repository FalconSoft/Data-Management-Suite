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
- SignalR
 * **Client.SignalR**
 * **Server.SignalR**
- InProcess

####Clients
 - **ReactiveWorksheets.Console**
 - **ReactiveWorksheets** - Real-time and customizable Wpf application. Is not open source yet!

###NuGet Packages

###Current Release
 - release is comming

###Future plans / Roadmap

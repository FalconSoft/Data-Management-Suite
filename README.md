Welcome to ReactiveWorksheets platform!
==================

FalconSoft's Reactive Worksheets platform is a set of reusable enterprise data management and analysis components designed and build as an reference platform in data management space.

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

Common
 - **ReactiveWorksheets.Common** - base project what incapsulates all core interfaces and base structures 

Server
 - **ReactiveWorksheets.Server** - data virtualization server source code
 - **ReactiveWorksheets.Server.Bootstrapper** - source code for bootstrapping data virtualization server
 - **ReactiveWorksheets.Server.Persistence** - project responsible to persist objects within

DataSources
 - MongoDb.DataSource
 - Sample.External Data Sources

Communications
- SignalR
 * Client.SignalR
 * Server.SignalR
- InProcess

Clients
 - ReactiveWorksheets.Console
 - Real-time and customizable Wpf application

###NuGet Packages

##Technologies used
 - .Net 4.5 (C#)
 - Reactive Extensions (RX)
 - Tasks / Async
 - MongoDb
 - SignalR
 - MEF
 - log4net
 - Json.Net
 - NUnit
 - IronPython

###Current Release

###Future plans / Roadmat

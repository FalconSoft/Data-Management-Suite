FalconSoft Data Management Suite
==================

FalconSoft Data Management Suite is a set of reusable data management components, designed and built by FalconSoft, to significantly speed up and simplify development of real-time data management solutions within an enterprise.

FalconSoft Data Management Suite' reference architecture and set of reusable components aimed to cover most common enterprise data management tasks, and allowing developers to focus rather on business tasks instead of technology issues.

##Resources
 - [web site - www.falconsoft-ltd.com/data-management-suite](http://www.falconsoft-ltd.com/data-management-suite)
 - [Licensing](https://github.com/FalconSoft/Data-Management-Suite/wiki/Licensing)
 - [Getting Started](https://github.com/FalconSoft/Data-Management-Suite/wiki/Getting-Started)
 - [Developer's Wiki page](https://github.com/FalconSoft/Data-Management-Suite/wiki)
 - [Architecture Overview](https://github.com/FalconSoft/Data-Management-Suite/wiki/Architecture-Overview)
 - [Release Notes](https://github.com/FalconSoft/Data-Management-Suite/wiki/Release-Notes)
 - [Future plans / Roadmap](https://github.com/FalconSoft/Data-Management-Suite/wiki/Roadmap)

###Core features
 - Real-time data synchronization
 - Collaborative User Inputs
 - Data Virtualization - virtual data integration layer that turns multiple data sources into unified and consistent data sets available to applications in real-time.
 - Versioned Data Repository - versioned data control system to keep track on change history
 - Data Federation - joining things together. Effectively you have a new layer for data modelling
 - Full Audit Trail - who changed data, when changed, and why changed
 - Security / control
 - Customizable WPF GUI with high frequent real-time updates and multi user support
 - Advanced search through data

###Project Structure
FalconSoft Data Management Suite is organized into several high level assemblies

####Common
 - **FalconSoft.Data.Management.Common** - common assembly that contains all main interfaces and base classes used in FalconSoft Data Server as well as Reactive Worksheets (GUI).
 - **FalconSoft.Data.Management.Components** - core server components responsible for processing real-time events, consolidate and validate messages.

####Server
 - **FalconSoft.Data.Server** - source code for bootstrapping and running FalconSoft Data Server
 - **FalconSoft.Data.Server.Persistence** - project responsible for persisting objects into data repository. We use [MongoDb](http://www.mongodb.org/) open source document database. 

####DataSources
 - **MongoDb.DataSource** - a primary implementation of data source what allow us to store dynamically created user data.
 - **Sample.DataSources** - sample data sources

####Communications
FalconSoft Data Server implementation does not depend on any specific communication technology. Even more, we made it easy to implement custom communication mechanisms, which better suits your infrastructure.

- RabbitMQ
 * **Client.RabbitMQ** - Client assembly what implements façades communication with [RabbitMQ](http://www.rabbitmq.com/). 
 * **Server.RabbitMQ** - Server side assembly what implements broker communication with [RabbitMQ](http://www.rabbitmq.com/) 
- SignalR
 * **Client.SignalR** - Client assembly what implements façades communication with [Microsoft SignalR](http://www.asp.net/signalr). 
 * **Server.SignalR** - Server side assembly what implements facades communication with [Microsoft SignalR](http://www.asp.net/signalr) 
- InProcess
A simple library what references server-side logic into clients AppDomain. It doesn't have any communication overhead and acts as a fat client. Mainly for testing or when no server side infrastructure. 

####Clients
 - **FalconSoft.Data.Console** - a simple console application to work with FalconSoft Data Server
 - **Reactive Worksheets** - A generic and customizable WPF application, what was designed and built to visualize and manage data sources in real-time. We use [DevExpress Controls for WPF](https://www.devexpress.com/Products/NET/Controls/WPF/) and enabled run-time customization of their [WPF Data Grid](https://www.devexpress.com/Products/NET/Controls/WPF/Grid/) and docking shell. This part is open source yet, but available with commercial license!

##Libraries / technologies used
 - .Net 4.0 - somebody still using Windows XP and we can't go to .Net 4.5 yet.
 - [MongoDb](http://www.mongodb.org/) - is an open-source document database, and the leading NoSQL database
 - [Reactive Extensions (RX)](http://msdn.microsoft.com/en-gb/data/gg577609.aspx) - is a library to compose asynchronous and event-based programs using observable collections and LINQ-style query operators.
 - [Json.Net](http://json.codeplex.com/) -  is a popular high-performance JSON framework for .NET
 - [log4net](http://logging.apache.org/log4net) - is a logging library
 - [RabbitMQ](http://www.rabbitmq.com/) - is a message broker to enable communication between the client and server.
 - [Asp.Net SignalR](http://www.asp.net/signalr) -  is a Microsoft Asp.Net technology to enable bi-directional communication between the client and server. However, communication layer can be elegantly implemented with other technologies e.g. ZeroMQ, Tibco ...
 - [DevExpress for WPF](https://www.devexpress.com/Products/NET/Controls/WPF) - is a commercial WPF controls library
 - [IronPython](http://ironpython.net) is an open-source implementation of the Python programming language which is tightly integrated with the .NET Framework.
 - [CS Script](http://www.csscript.net/) is a CLR based scripting system which uses ECMA-compliant C# as a programming language.
 - [AvalonEdit](http://avalonedit.net) - is a WPF-based text editor component

RemoveQuotes application

Brief
==================================================================================
This sample shows how to remove instrument prices that are kept in the local storage.

Building the application
==================================================================================
In order to build this application you will need MS Visual Studio 2017 or later and
.NET Core 1.0 or later.
You can download .NET Core from https://www.microsoft.com/net/download/core
To build the application run fxbuild.bat on Windows or fxbuild.sh on *nix.
To compile the application into a debug build set argument "Debug" via command line.
Compiled files will be placed in .\bin\netcoreapp1.0 directory.

Running the application
==================================================================================
To run the application you must run fxrun.bat on Windows or fxrun.sh on *nix. 
The output will be displayed on your console.

Arguments
==================================================================================
<login>
Your user name.

<password>
Your password.

<url>
The server URL. For example, http://www.fxcorporate.com/Hosts.jsp. 

<connection>
The connection name.For example, "Demo" or "Real".

<sessionid>
The database name. This argument is optional. Required only for users who have a multiple database login.

<pin>
Your pin code. This argument is optional. Required only for users who have a pin.

<instrument>
The instrument that you want to use in the sample. For example, "EUR/USD".

<year>
The year for which you want to remove historical prices from the local storage.
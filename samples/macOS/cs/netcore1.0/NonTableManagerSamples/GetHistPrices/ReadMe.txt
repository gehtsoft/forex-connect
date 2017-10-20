GetHistPrices application

Brief
==================================================================================
This sample shows how to load instruments' historical prices.
The sample performs the following actions:
1. Login.
2. Request historical prices for the specified timeframe of the specified period or last trading day. 
3. If the request is not filled completely, additional request(s) will be sent until the complete filling.
4. Print gathered information.
5. Logout.

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
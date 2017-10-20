CloseAllPositionsByInstrument application

Brief
==================================================================================
This sample shows how to close all positions for the specified instrument.
After the successful order execution it will print the balance 
and the closed trades table.
Otherwise, it will print information about an error.
The sample performs the following actions:
1. Login. 
2. Close all positions for the specified instrument by using a netting close order.
3. Wait for tables to update.
4. Print information about all closed trades.
   If an order has not been executed, information about an error
   will be printed instead.
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
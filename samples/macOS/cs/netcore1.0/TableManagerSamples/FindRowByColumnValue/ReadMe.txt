FindRowByColumnValue application

Brief
==================================================================================
This sample shows how to find a table row by the column value.
The sample performs the following actions:
1. Login.
2. Create an order with attached limit and stop orders. Store the request ID.
3. Find all rows in the orders table with the request ID from the previous step.
4. Find all rows in the orders table by multiple fields: Type and BuySell.
5. Print information about the found rows.
6. Logout.

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
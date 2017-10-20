LockUpdates application

Brief
==================================================================================
This sample shows how to stop tables updates.
The sample performs the following actions:
1. Login.
2. Create a TrueMarketOpen order.
3. Lock orders table updates after the order has been created.
4. Print orders table information.
5. Unlock orders table updates.
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
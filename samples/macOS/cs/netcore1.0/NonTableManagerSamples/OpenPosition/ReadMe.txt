OpenPosition application

Brief
==================================================================================
The sample shows how to open position on hedging and non-hedging accounts. 
The sample does not work on netting accounts.
The sample performs the following actions:
1. Login.
2. Open position.
3. Wait for order execution.
4. Print result of execution.
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
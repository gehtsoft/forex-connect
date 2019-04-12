RemoveQuotes application

Brief
==================================================================================
This sample shows how to remove instrument prices that are kept in the local storage.

Building the application
==================================================================================
To build this application, you will need MS Visual Studio 2015 or later and
.NET framework 4.5.2
You can download .NET framework from http://msdn.microsoft.com/en-us/netframework/
To build the application, run fxbuild.bat.
To build the x64 application, run fxbuild64.bat.
To compile a debug build of the application, add the argument "debug" using the command line.
The compiled files will be placed in the ..\bin\dotnet45\x86|x64 directory.

Running the application
==================================================================================
You can run this application from the ..\bin\dotnet45\x86|x64 directory.

All arguments must be passed from the command line -
this will run the application and display the output in your console.

You can run the application with no arguments, this will show the
application Help and information about historical prices from the local storage.

Arguments
==================================================================================
<instrument>
The instrument that you want to use in the sample. For example, "EUR/USD".

<year>
The year for which you want to remove historical prices from the local storage.

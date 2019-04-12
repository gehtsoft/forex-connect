GUI sample

Brief
================================================================================================
This sample shows how to use the Price History and Quotes Manager APIs in GUI applications.
The sample is a GUI application written using WinForms.
This sample allows doing the following:
- Print price updates (Offers table);
- Get price history using the Price History API (Get History command);
- Request information about historical prices from the local storage; 
- Remove prices of the specified instrument for the specified year using the QuotesManager API 
  (Remove Quotes From Cache command).

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
You can run this application from the ..\bin\dotnet45\x86|x64 directory. No arguments are required.
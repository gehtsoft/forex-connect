ShowCommissionDetails application

Brief
==================================================================================
This sample shows information about the commissions.
The sample performs the following actions:
1. Login. 
2. Print information about the commissions.
3. Logout.

Building the application
==================================================================================
In order to build this application you will need MS Visual Studio 2015 and
.NET framework 4.5 or later.
You can download .NET framework from http://msdn.microsoft.com/en-us/netframework/
To build the application run fxbuild.bat.
To compile the application into a debug build set argument "debug" via command line.
Compiled files will be placed in .\bin\ directory.

Running the application
==================================================================================
Change the App.config file by putting your information in the "appSettings" section.
For example, if your user name is 'testuser' change the line
<add key="Login" value="{LOGIN}" /> to <add key="Login" value="testuser" />.
To run the application you must run executable file in .\bin\ directory.
The output will be displayed on your console.

Arguments
==================================================================================
{LOGIN} - Your user name. Mandatory argument.
{PASSWORD} - Your password. Mandatory argument.
{URL} - The server URL. Mandatory argument.
        The URL must be full, including the path to the host descriptor.
        For example, http://www.fxcorporate.com/Hosts.jsp. 
{CONNECTION} - The connection name. Mandatory argument.
        For example, "Demo" or "Real".
{INSTRUMENT} - An instrument, for which you want to create an order.
For example, EUR/USD. Mandatory argument.
{BUYSELL} - The order direction. Possible values are: B - buy, S - sell. Mandatory argument.
{LOTS} - Trade amount in lots. Optional argument.
For example, 2.
{ACCOUNT} - Your Account ID. Optional argument.

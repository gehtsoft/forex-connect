PrintTableListener application

Brief
==================================================================================
This sample shows how to get a table and print it.
It differs from the TableManager/PrintTable sample in that the former uses
the TablesManagerStatusListener class to receive information about the TablesManager status.
The sample performs the following actions:
1. Login.
2. Print the accounts table by using the getNextRow.
3. Print the orders table by using the IO2GEachRowListener
4. Logout.

Building the application
==================================================================================
In order to build this application you will need MS Visual Studio 2005 or later and
.NET framework 2.0 or 4.0.
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
{SESSIONID} - The database name. Optional argument.
        Required only for users who have a multiple database login.
        If you do not have one, leave this argument as it is.
{PIN} - Your pin code. Optional argument. Required only for users who have a pin.
        If a pin is not required, leave this argument as it is.
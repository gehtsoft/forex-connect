TwoConnections application

Brief
==================================================================================
This sample shows how to work with multiple sessions.
The sample performs the following actions:
1. Login by using two different sets of credentials.
2. Set one of the sessions for a NO Price Update
3. Open a position for the specified instrument in two sessions.
4. Logout from both sessions.

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
{LOGIN2} - Your user name for second session. Mandatory argument.
{PASSWORD2} - Your password for second session. Mandatory argument.
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
{SESSIONID2} - The database name for second session. Optional argument.
        Required only for users who have a multiple database login.
        If you do not have one, leave this argument as it is.
{PIN2} - Your pin code for second session. Optional argument. Required only for users who have a pin.
        If a pin is not required, leave this argument as it is.
{INSTRUMENT} - An instrument, for which you want to create an order.
        For example, EUR/USD. Mandatory argument.
{BUYSELL} - The order direction. Possible values are: B - buy, S - sell. Mandatory argument.
{LOTS} - Trade amount in lots. Optional argument.
        For example, 2.
{ACCOUNT} - Your Account ID. Optional argument.
{ACCOUNT2} - Your Account ID  for second session. Optional argument.
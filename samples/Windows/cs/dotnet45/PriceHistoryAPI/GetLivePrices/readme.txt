GetLivePrices application

Brief
==================================================================================
This sample shows how to load instrument historical prices and build live quotes.
The sample does the following steps:
1. Log in.
2. Request historical prices with the specified timeframe for the specified period.
3. Print the received bars to the console.
4. Receive table updates from O2GSession.
5. Update the latest bar when a new tick is available.
3. Print the gathered information.
4. Log out.

Building the application
==================================================================================
To build this application, you will need MS Visual Studio 2015 or later and
.NET framework 4.5.2
You can download .NET framework from http://msdn.microsoft.com/en-us/netframework/
To build the application for .NET framework 4.5.2, run fxbuild.bat.
To build the x64 application for .NET framework 4.5.2, run fxbuild64.bat.
To compile a debug build of the application, add the argument "debug" using the command line.
The compiled files will be placed in the ..\bin\dotnet45\x86|x64 directory.

Running the application
==================================================================================
Change the GetLivePrices.config file by putting your information in the "appSettings" section.
For example, if your user name is 'testuser', change the line
<add key="Login" value="{LOGIN}" /> to <add key="Login" value="testuser" />.
To run the application, run the executable file in the ..\bin\dotnet45\x86|x64 directory.
The output will be displayed in your console.

Arguments
==================================================================================
{LOGIN} - Your user name. 
{PASSWORD} - Your password. 
{URL} - The server URL. 
        For example, http://www.fxcorporate.com/Hosts.jsp. 
{CONNECTION} - The connection name.
        For example, "Demo" or "Real".
{SESSIONID} - The database name. This argument is optional.
        Required only for users who have a multiple database login.
{PIN} - Your pin code. This argument is optional. Required only for users who have a pin.
{INSTRUMENT} - The instrument that you want to use in the sample.
        For example, "EUR/USD".
{TIMEFRAME} - The time period that forms a single candle. 
        For example, m1 - for 1 minute, H1 - for 1 hour. 
        Custom timeframes (e.g. m2) are also supported.
{DATEFROM} - The date and time starting from which you want to receive historical prices.
        This argument is optional. If you leave this argument as is,
        a default value will be used. The format is MM.dd.yyyy HH:mm:ss
        The time is in UTC timezone.
{COUNT} - The number of historical prices you want to receive. This argument is optional. 
        If you leave this argument as is, a default value will be used or the argument will be ignored if {DATEFROM} 
        is specified.

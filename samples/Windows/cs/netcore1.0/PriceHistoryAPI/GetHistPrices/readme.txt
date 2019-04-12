GetHistPrices application

Brief
==================================================================================
This sample shows how to load instrument historical prices.
The sample does the following steps:
1. Log in.
2. Request historical prices with the specified timeframe for the specified period
   using PriceHistory API. 
3. Print the gathered information.
4. Log out.

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

Arguments
==================================================================================
<login>
Your user name.

<password>
Your password.

<url>
The server URL. For example, http://www.fxcorporate.com/Hosts.jsp. 

<connection>
The connection name.For example, "Demo" or "Real".

<sessionid>
The database name. This argument is optional. Required only for users who have a multiple database login.

<pin>
Your pin code. This argument is optional. Required only for users who have a pin.

<instrument>
The instrument that you want to use in the sample. For example, "EUR/USD".

<timeframe>
The time period that forms a single candle. For example, m1 - for 1 minute, H1 - for 1 hour. 
Custom timeframes (e.g. m2) are also supported.

<datefrom>
The date and time starting from which you want to receive historical prices.
This argument is optional. If you leave this argument as is, a default value will be used. The format is MM.dd.yyyy HH:mm:ss
The time is in UTC timezone.

<dateto>
The date and time until which you want to receive historical prices.
This argument is optional. If you leave this argument as is, you will get the data by now.
The format is MM.dd.yyyy HH:mm:ss
The time is in UTC timezone.

<count>
The number of historical prices you want to receive. This argument is optional. 
If you leave this argument as is, a default value will be used or the argument will be ignored 
if <datefrom is specified.

<candlesmode>
If it's "firsttick" then the opening price of a period equals the first price update inside the period.
If it's "prevclose" then the opening price of a period equals the prior period's close price.
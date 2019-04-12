GetLivePrices application

Brief
===============================================================================
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
===============================================================================
To build this application, you will need Java SDK 1.7 and Apache Ant.
You can download Java SDK from http://www.oracle.com/technetwork/java/javase/downloads
You can download Apache Ant from http://ant.apache.org/bindownload.cgi

In your command prompt, go to the current directory and type: 
./fxbuild.sh -  on Linux and
fxbuild.bat - on Windows.

This will generate the directory structure .\build\classes\ and place the compiled files there.

Running the application
===============================================================================
In your command prompt, go to the current directory and type: 
./fxrun.sh - on Linux
fxrun.bat - on Windows.

All arguments must be passed from the command line -
this will run the application and display the output in your console.

For example,
./fxrun.sh -login my_login --password my_password -u http://fxcorporate.com/Hosts.jsp -c Demo
 --timeframe m1 --datefrom "04.17.2013 00:00:00" --dateto "04.17.2013 00:01:00" --instrument EUR/USD

You can run the application with no arguments, this will show the
application Help.

Arguments
===============================================================================
/login | --login | /l | -l
Your user name.

/password | --password | /p | -p
Your password.

/url | --url | /u | -u
The server URL. For example, http://www.fxcorporate.com/Hosts.jsp.

/connection | --connection | /c | -c
The connection name. For example, "Demo" or "Real".

/sessionid | --sessionid 
The database name. This argument is optional. 
Required only for users who have accounts in more than one database.

/pin | --pin 
Your pin code. This argument is optional. Required only for users who have a pin. 

/instrument | --instrument | /i | -i
The instrument that you want to use in the sample. For example, "EUR/USD".

/timeframe | --timeframe 
The time period which forms a single candle. For example, m1 - for 1 minute, H1 - for 1 hour.
Custom timeframes (e.g. m2) are also supported.

/datefrom | --datefrom 
The date and time starting from which you want to receive historical prices. This argument is optional.
If you leave this argument as is, a default value will be used. The format is "m.d.Y H:M:S". 
The time is in UTC timezone.

/count | --count
The number of historical prices you want to receive. This argument is optional. 
If you leave this argument as is, a default value will be used or the argument will be ignored if 'datefrom' 
is specified.

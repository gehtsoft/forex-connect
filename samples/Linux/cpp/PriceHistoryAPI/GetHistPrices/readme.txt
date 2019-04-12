GetHistPrices application

Brief
===============================================================================
This sample shows how to load instrument historical prices.
The sample does the following steps:
1. Log in.
2. Request historical prices with the specified timeframe for the specified period
   using the PriceHistory API.
3. Write the gathered information to a CSV file.
4. Log out.

Building the application
===============================================================================
Windows:
    To build this application, you will need MS Visual Studio 2015 or later.

    To build the application, in MS Visual Studio, either run fxbuild.bat or select "build".

Linux/MacOS:
    To build this application, you will need:
        gcc-4.3 or later
        g++-4.3 or later
        CMake 2.6 or later
        
To build the application, run fxbuild.sh.

Running the application
===============================================================================
You can run the application from the bin directory.
Also you can run this application by executing fxrun.bat or fxrun.sh on Windows
and Linux respectively.
All arguments must be passed from the command line -
this will run the application and display the output in your console.

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
The time period that forms a single candle. For example, m1 - for 1 minute, H1 - for 1 hour.
Custom timeframes (e.g. m2) are also supported.

/datefrom | --datefrom 
The date and time starting from which you want to receive historical prices. This argument is optional.
If you leave this argument as is, a default value will be used. The format is "m.d.Y H:M:S". 
The time is in UTC timezone.

/dateto | --dateto 
The date and time until which you want to receive historical prices. This argument is optional.
If you leave this argument as is, the data will be received up to now. The format is "m.d.Y H:M:S". 
The time is in UTC timezone.

/count | --count
The number of historical prices you want to receive. This argument is optional. 
If you leave this argument as is, a default value will be used or the argument will be ignored if 'datefrom' 
is specified.

/output | --output
The output file name.

Examples
===============================================================================

GetHistPricesCSV /login {LOGIN} /password {PASSWORD} /connection Demo /instrument EUR/USD /timeframe m1 /url http://www.fxcorporate.com/Hosts.jsp /datefrom "06.27.2014 20:50:00" /dateto "06.29.2014 22:05:00" /output result.csv
GetHistPricesCSV /login {LOGIN} /password {PASSWORD} /connection Demo /instrument EUR/USD /timeframe m2 /url http://www.fxcorporate.com/Hosts.jsp /count 10 /dateto "06.29.2014 22:05:00" /output result.csv


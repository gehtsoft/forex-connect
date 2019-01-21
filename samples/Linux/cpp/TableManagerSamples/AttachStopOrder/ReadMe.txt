AttachStopOrder application


Brief
===============================================================================
This sample application shows how to attach a stop order to an existing entry order.

The sample script performs the following:
1. Logs in.
2. Creates and sends a request to create a stop order for the existing entry order specified by the --orderid argument.
3. Waits when the order appears in the Orders table and prints "Done!".
4. Logs out.

See ForexConnect API SDK C++ User Guide for more details:
http://fxcodebase.com/bin/forexconnect/1.6.0/help/CPlusPlus/web-content.html#TradingCommandsCreateOrder_S.html

Building the application

===============================================================================

Windows:

    To build this application, you will need MS Visual Studio 2015.



    You can run fxbuild.bat (fxbuild64.bat for 64-bit version) or select

    "build" in MS Visual Studio.



Linux/MacOS:

    To build this application, you will need:

        gcc-4.3 or later

        g++-4.3 or later

        CMake 2.6 or later



Run fxbuild.sh to build application.



Running the application
===============================================================================
You can run this application from the bin directory.

In Windows you can run this application executing fxrun.bat

All arguments must be passed from the command line.

This will run the application and display the output in your console.


You can run the application with no arguments, this will show the
application Help.


Arguments
===============================================================================
/login | --login | /l | -l
Your username.

/password | --password | /p | -p
Your password.

/url | --url | /u | -u
The server URL. For example, http://www.fxcorporate.com/Hosts.jsp.

/connection | --connection | /c | -c
The connection name. For example, "Demo" or "Real".

/sessionid | --sessionid
The database name. Required only for users who have accounts in more than one database. Optional parameter.

/pin | --pin
Your PIN code. Required only for users who have PIN codes. Optional parameter.

/account | --account
The account ID. Optional parameter.
If not specified and you have more than one account, the order will be created on one of the accounts. 

/orderid | --orderid
The order ID of the order you want the stop order to be attached to.

/ispegged | --ispegged
If it is present then rate of the stop order should be specified as an offset (in pips) from value specified by the --pegoffset argument.

/pegtype | --pegtype
Required only if --ispegged argument is present.
The value to be used to calculate the stop order offset from.
Possible values are: O - the open price of the related trade, M - the close price (the current market price) of the related trade. 

/pegoffset | --pegoffset
Required only if --ispegged argument is present.
The desired rate of the stop order in pips (offset from value specified by the --pegoffset argument).

/rate | --rate
Required if --ispegged argument is not present.
The desired rate of the stop order if the order is not pegged.
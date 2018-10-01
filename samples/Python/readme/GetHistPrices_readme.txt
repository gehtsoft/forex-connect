GetHistPrices Sample Script


Brief
===============================================================================
This sample script shows how to get historical prices for specified dates, 
instrument, and timeframe.

The sample script performs the following:
1. Logs in.
2. Gets historical prices.
3. Defines wether the specified timeframe is tick or bar.
4. For ticks, prints Date, Bid, Ask. 
   For bars, prints Date, BidOpen, BidHigh, BidLow, BidClose, Volume.
5. Logs out.


Running the Sample Script
===============================================================================
Prerequisites:
	- ForexConnect API Python must be installed.
	- common_samples folder must be in the same directory as the sample script.

If Python is added to the PATH Environmental Variable,
to run the sample script, execute the following command in a console:
python <Sample.py> <Arguments>


Arguments
===============================================================================
-l
Your username.

-p
Your password.

-u
The server URL. For example, http://www.fxcorporate.com/Hosts.jsp.

-c
The connection name. For example, "Demo" or "Real".

-session
The database name. Required only for users who have accounts in more than one database. Optional parameter.

-pin 
Your PIN code. Required only for users who have PIN codes. Optional parameter.

-i

The instrument you want to use in the sample script. Optional parameter.
The default value is EUR/USD 

-timeframe

The time period that forms a single bar. For example, m1 - for 1 minute, H1 - for 1 hour. Optional Parameter.
The default value is m1

-datefrom 

The date/time from which you want to receive historical prices. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
The date/time is in the UTC time zone. If not specified, will load the number of bars defined by -quotescount.

 
-dateto

The date/time until which you want to receive historical prices. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
The date/time is in the UTC time zone. If not specified, will load bars up to now.

-quotescount

The maximum number of bars. Optional parameter.
The default value is 300
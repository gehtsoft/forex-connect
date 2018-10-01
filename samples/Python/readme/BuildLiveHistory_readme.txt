BuildLiveHistory Script 


Brief
===============================================================================
This sample script shows how to load instrument historical prices and build live quotes.

The sample performs the following actions:
1. Logs in.
2. Checks if specified timeframe is not a tick.
3. Creates a live history creator.
4. Subscribes for the Offers table updates. 
5. Gets price hisotry and passes it to the live history creater.
6. Passes every Offers table update to the live history creator and prints it to the console. 
7. Calculates waiting (sleeping) time required to collect live history for specified number of bars.
8. Unsubscribes from the Offers table updates.
9. Logs out.


Running the Sample Script
===============================================================================
Prerequisites:
	- ForexConnect API Python must be installed.
	- common_samples folder must be in the same directory as the sample script.

If Python added to the PATH Environmental Variable,
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

The instrument you want to use in the sample application. Optional parameter.
The default value is EUR/USD

-timeframe

The time period that forms a single bar. For example, m1 - for 1 minute, H1 - for 1 hour. Optional Parameter.
The default value is m1

-datefrom 

The date/time from which you want to receive historical prices. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
The date/time is in the UTC time zone. If not specified, will load the number of bars defined by -quotescount.

 
-bars

The number of bars for live history. Optional parameter.
The default value is 3

-o
The parameter defines how a candle Open price is determined. Possible values are:
first_tick - the candle Open price is determined by a first tick of the candle.
prev_close - the candle Open price is determined by a Close price of the previous candle.
Optional Parameter.
The default value is prev_close
BuildLiveHistory Script 


Brief
===============================================================================
This sample script shows how to load instrument historical prices and build live quotes.

The sample performs the following actions:
1. Logs in.
2. Checks that the specified timeframe is not tick.
3. Creates a LiveHistoryCreator.
4. Subscribes to Offers table updates. 
5. Gets price history and passes it to the LiveHistoryCreator.
6. Passes each Offers table update to the LiveHistoryCreator and prints it to the console. 
7. Calculates the waiting (sleeping) time required to collect live history for the specified number of bars.
8. Unsubscribes from Offers table updates.
9. Logs out.


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

The instrument you want to use in the sample application. Optional parameter.
The default value is EUR/USD

-timeframe

The time period that forms a single bar. For example, m1 - for 1 minute, H1 - for 1 hour. Optional Parameter.
The default value is m1

-bars

The number of bars for live history. Optional parameter.
The default value is 3

-o
The parameter defines how a candle Open price is determined. Possible values:
first_tick - the candle Open price is determined by the first tick of the candle.
prev_close - the candle Open price is determined by the Close price of the previous candle.
Optional Parameter.
The default value is prev_close
MovingAveragesCrossTrading Script 


Brief
===============================================================================
This sample script shows how to automate trading with the Moving Average Crossover strategy.

The sample performs the following:
1. Logs in and subscribes to Orders table updates.
2. Creates a LiveHistoryCreator and subscribes it to bar updates.
3. Subscribes to session status changes.
4. Gets price history and sets it to the LiveHistoryCreator.
5. When a new bar is added to the LiveHistoryCreator, creates a MovingAverageCrossStrategy and passes the current history to it.
6. The MovingAverageCrossStrategy calculates two moving averages with the specified periods and generates Buy, Sell, or Hold crossover signals.
7. Gets a signal from the MovingAverageCrossStrategy and, if the signal is Buy or Sell, creates an open market order.
8. Gets Orders table updates and prints details for the created order.
9. If the session status changes and the specified number of orders to be created (-orderscount) is not reached, tries to reconnect.
10. When the number of created orders reaches -orderscount, logs out.
11. Unsubscribes from all updates.


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

-lots
The trade amount in lots. Optional parameter.
The default value is 1

-account

The account ID. Optional parameter.
If not specified and you have more than one account, the position will be opened on one of the accounts.

-i

The instrument you want to use in the sample application. Optional parameter.
The default value is EUR/USD

-timeframe

The time period that forms a single bar. For example, m1 - for 1 minute, H1 - for 1 hour. Optional Parameter.
The default value is m1

-datefrom 

The date/time from which you want to receive historical prices. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
The date/time is in the UTC time zone. 

-orderscount
The number of orders the script should create. Optional parameter.
The default value is 3

-shortperiods
The number of periods of the first Moving Average. Optional parameter.
The default value is 5

-longperiods
The number of periods of the second Moving Average. Optional parameter.
The default value is 15
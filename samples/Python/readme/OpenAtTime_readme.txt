OpenAtTime Sample Script


Brief
===============================================================================
This sample script shows how to open a position at specified time 
for specified account ID, instrument, buy/sell, and number of lots.

The sample script performs the following:
1. Logs in.
2. Gets the account ID (if not specified).
3. Creates a request for creating an open market order.
4. Subscribes to Orders table and Trades table updates for the open market order ID.
5. Waits until local time is the same as specified by the -openhour and -openminute parameters.
6. When local time is the same as specified, sends the request and gets the open market order ID.
7. Gets the Orders table and Trades table updates and prints them.
8. Unsubscribes from Orders table and Trades table updates.
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
The instrument you want to use in the sample script. Optional parameter.
The default value is EUR/USD

-d
The order type/direction. Possible values: B - buy, S - sell.

-lots
The trade amount in lots. Optional parameter.
The default value is 1

-account
The account ID. Optional parameter.
If not specified and you have more than one account, the position will be opened on one of the accounts.

-openhour
Hour of a day when the position will be open (Local time).

-openminute
Minute of the specified hour when the position will be open (Local time). Optional parameter.
The default value is 0
ClosePosition Sample Script


Brief
===============================================================================
This sample script shows how to close a position for a specified instrument
by creating a close market order with an opposite direction.

The sample script performs the following:
1. Logs in.
2. Gets the account ID (if not specified).
3. Gets the offer for the specified instrument from the Offers table.
4. Gets the trade ID of the open position, its amount and direction.
5. Creates a request for creating a close market order.
6. Subscribes to Closed Trades table and Orders table updates for the close market order ID.
7. Sends the request and gets the close market order ID.
8. Gets the Closed Trades table and Orders table updates for the close market order ID and prints them.
9. Unsubscribes from Closed Trades table and Orders table updates.
10. Logs out.


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
The instrument you want to use in sample script. Optional parameter.
The default value is EUR/USD

-account
The account ID. Optional parameter.
If not specified and you have more than one account, the position will be closed on one of the accounts.
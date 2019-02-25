CreateEntryWithPeggedStopLimit Sample Script


Brief
===============================================================================
This sample script shows how to create an entry order with an attached stop and/or limit order.

The sample script performs the following:
1. Logs in.
2. Gets the account ID (if not specified).
3. Creates a request for creating an entry order and stop and/or limit order attached to it.
4. Subscribes to Orders table updates.
5. Sends the request.
6. Gets updates from the Orders table and when the entry order added to the table,
prints details in the console.
7. Unsubscribes from Orders table updates.
8. Logs out.


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
The database name. Required only for users who have accounts in more than one database. Optional argument.

-pin
Your PIN code. Required only for users who have PIN codes. Optional argument.

-i
The instrument you want to use in the sample. Optional argument.
The default value is EUR/USD

-account
The account ID. Optional argument.
If not specified and you have more than one account, the order will be created on one of the accounts. 

-d
The order direction/type. Possible values: B - buy, S - sell.

-lots
The trade amount in lots. Optional argument.
The default value is 1

-r
The desired rate of the entry order.

-peggedstop
Defines whether rate of the stop order specified as an offset (in pips).
Possible values: y or n. Optional argument.
The default value is n

-peggedstoptype
Required only if value of the -peggedstop argument is y.
The value to be used to calculate the stop order offset from.
Possible values are: O - the open price of the related trade, 
M - the close price (the current market price) of the related trade.

-stop
The desired rate of the stop order.

-peggedlimit
Defines whether rate of the limit order specified as an offset (in pips).
Possible values: y or n. Optional argument.
The default value is n

-peggedlimittype
Required only if value of the -peggedlimit argument is y.
The value to be used to calculate the limit order offset from.
Possible values are: O - the open price of the related trade, 
M - the close price (the current market price) of the related trade.

-limit
The desired rate of the limit order.
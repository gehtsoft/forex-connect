AttachStopLimitOrder Sample Script


Brief
===============================================================================
This sample script shows how to attach a stop order to an existing entry order.

The sample script performs the following:
1. Logs in.
2. Gets the Order table and subscribes to its updates.
3. Finds order row for the order ID specified by the -orderid argument.
4. When attaching a stop order:
- If the order has attached stop order, creates and sends a request to change the stop order.
- If the order does not have an attached stop order, creates and sends a request to create a stop order.
5. When attaching a limit order:
- If the order has attached limit order, creates and sends a request to change the limit order.
- If the order does not have an attached limit order, creates and sends a request to create a limit order.
6. Gets updates from the Orders table and when the stop and/or limit order added/changed to the table,
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

-sessionid
The database name. Required only for users who have accounts in more than one database. Optional argument.

-pin
Your PIN code. Required only for users who have PIN codes. Optional argument.

-orderid
The order ID of the order you want the stop order to be attached to.

-peggedstop
Defines whether rate of the stop order specified as an offset (in pips).
Possible values: y or n. Optional argument.
The default value is n

-pegstoptype
Required only if value of the -peggedstop argument is y
The value to be used to calculate the stop order offset from.
Possible values are: O - the open price of the related trade, 
M - the close price (the current market price) of the related trade. 

-stop
The desired rate of the stop order.

-peggedlimit
Defines whether rate of the limit order specified as an offset (in pips).
Possible values: y or n. Optional argument.
The default value is n

-peglimittype
Required only if value of the -peggedlimit argument is y
The value to be used to calculate the limit order offset from.
Possible values are: O - the open price of the related trade, 
M - the close price (the current market price) of the related trade. 

-limit
The desired rate of the limit order.
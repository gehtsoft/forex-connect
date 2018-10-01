RemoveOrder Sample Script


Brief
===============================================================================
This sample script shows how to remove an order by its order ID.

The sample script performs the following:
1. Logs in.
2. Gets Order row from the Orders table by specified order ID.
3. Creates a request for deleting the order.
4. Subscribes to Orders table updates for the specified order ID.
5. Sends the request.
6. Gets the Orders table updates for the specified order ID and prints them.
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
The database name. Required only for users who have accounts in more than one database. Optional parameter.

-pin 
Your PIN code. Required only for users who have PIN codes. Optional parameter.

-account

The account ID. Optional parameter.

-orderid

The Order ID of the order you want to remove.
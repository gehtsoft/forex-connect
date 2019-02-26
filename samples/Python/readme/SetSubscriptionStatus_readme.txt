SetSubscriptionStatus Sample Script


Brief
===============================================================================
This sample script shows how to set subscription status for a specified instrument.

The sample script performs the following:
1. Logs in.
3. Gets the Offers table.
6. Creates a request for setting subscription status.
7. Subscribes to Offers table updates.
8. Sends the request.
9. Gets Offers table updates for the specified instrument and prints them.
10. Unsubscribes from Offers table updates.
11. Logs out.


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

-status
The subscription status.
Possible values are: T - Price updates are available, D - Price updates are not available,
V - Price updates are available not for trading. Used for calculating cross instruments.
 

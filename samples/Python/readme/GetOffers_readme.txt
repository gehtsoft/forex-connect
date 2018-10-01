GetOffers Sample Script


Brief
===============================================================================
This sample script shows how to get actual information about offers

The sample script performs the following:
1. Logs in.
2. Print actual bid and ask prices for all offers.
3. Wait 60 seconds and print offer updates.
4. Logs out.


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
The default value is EUR/USD.
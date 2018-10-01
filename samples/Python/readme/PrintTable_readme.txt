PrintTable Sample Script


Brief
===============================================================================
This sample script shows how to print data from the Accounts and Orders tables.

The sample script performs the following:
1. Logs in.
2. Gets the Accounts table.
3. For all accounts, prints the account ID and balance.
4. Gets the Orders table.
5. Prints order information from the Orders table for one account.
6. Logs out.


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
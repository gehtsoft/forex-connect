SetStop Sample Script


Brief
===============================================================================
This sample script shows how add stop orders for all open positions for a specified instrument.


The sample script performs the following:
1. Logs in.
2. Gets an account ID (if not specified).
3. Gets Trades table.
4. Checks all open position for specified instrument. For all the positions:
- If open position has attached stop order, changes its rate to value specified by -Stop parameter.
- If open position does not have attached stop order, creates it.
5. Logs out.


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

-Stop
Stop order distance in pips.

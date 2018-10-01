GetReport Sample Script


Brief
===============================================================================
This sample script shows how to download a CAS report for all user's accounts
for specified dates in the HTML format.

The sample script performs the following:
1. Logs in.
2. Gets account IDs from the Accounts table.
3. For each account ID, gets a CAS Report URL, and prints the account ID, balance and URL.
4. Downloads a report from the URL for each account ID and saves the reports to files.
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

-datefrom 

The date/time from which you want to receive the report. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.

-dateto

The date/time until which you want to receive the report. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
If it is not specified, the report is generated up to today.
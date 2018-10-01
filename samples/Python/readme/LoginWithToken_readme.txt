LoginWithToken Sample Script


Brief
===============================================================================
This sample script shows how to create a second trading session using an SSO token.

The sample script performs the following:
1. Logs in to the first trading session.
2. Gets a token.
3. Logs in to a second trading session using the token.
4. Logs out from the second trading session.
5. Logs out from the first trading session.


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
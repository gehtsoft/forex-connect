Indicators Sample Script


Brief
===============================================================================
This sample script shows how to calculate indicators for a specified instrument 
and add them as a additional column to the price history for a specified period.

The sample script performs the following:
1. Logs in.
2. Gets an account ID (if not specified).
3. Gets price history for specified dates, instrument, and timeframe in numpy.ndarray format and prints it.
4. Calculates indicators for each history row and prints it in additional column to the price history.
5. Logs out.

Indicators shown in this sample:
- ma - Simple Moving Average - http://fxcodebase.com/bin/products/FXTS/2016-R3/help/MS/NOTFIFO/web-content.html#i_MVA.html
- adx - Average Directional Index - http://fxcodebase.com/bin/products/FXTS/2016-R3/help/MS/NOTFIFO/web-content.html#i_ADX.html
- macd - Moving Average Convergence/Divergence - http://fxcodebase.com/bin/products/FXTS/2016-R3/help/MS/NOTFIFO/web-content.html#i_MACD.html
- rsi - Relative Strength Index - http://fxcodebase.com/bin/products/FXTS/2016-R3/help/MS/NOTFIFO/web-content.html#i_RSI.html
- bbands - Bollinger Band - http://fxcodebase.com/bin/products/FXTS/2016-R3/help/MS/NOTFIFO/web-content.html#i_BB.html
- zigzag - ZigZag - http://fxcodebase.com/bin/products/FXTS/2016-R3/help/MS/NOTFIFO/web-content.html#i_ZigZag.html


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

-timeframe
The time period that forms a single bar. For example, m1 - for 1 minute, H1 - for 1 hour. Optional Parameter.
The default value is m1

-datefrom
The date/time from which you want to receive historical prices. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
The date/time is in the UTC time zone. If not specified, will load the number of bars defined by -quotescount.
 
-dateto
The date/time until which you want to receive historical prices. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
The date/time is in the UTC time zone. If not specified, will load bars up to now.

-quotescount
The maximum number of bars. Optional parameter.
The default value is 300
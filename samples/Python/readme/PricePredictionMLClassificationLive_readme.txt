PricePredictionMLClassificationLive Sample Script


Brief
===============================================================================
This sample script shows how to use Machine Learning in Python on live prices
and how to predict prices by using Support Vector Classification.

The model divides objects into two categories:
"the price will grow on the next bar" and "the price will fall on the next bar".
The input data for the model are Open minus Close and High minus Low.
The model tries to define whether the price is growing or falling on the basis of the input data.

The sample script performs the following:
1. Logs in.
2. Requests historical prices for the specified timeframe from the date, specified by -datefrom up to now. 
3. Transforms the historical prices into the format required for the model.
4. Uses the historical prices as a train set for the model.
5. Predicts whether price for the next bars (number of the bars specified by the -nextbars) will grow or fall
and prints the prediction.
6. Waits until next bar is formed.
7. When next bar formed, uses it as a part of training set for the model, makes new prediction and prints it.
8. Logs out.


Running the Sample Script
===============================================================================

Prerequisites:
	- ForexConnect API Python must be installed.
	- scikit-learn Python library must be installed.
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
The date/time is in the UTC time zone.

 
-nextbars
Number of bars prediction will be made for. Optional parameter.
The default value is 5
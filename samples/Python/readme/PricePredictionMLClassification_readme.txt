PricePredictionMLClassification Sample Script


Brief
===============================================================================
This sample script shows how to use Machine Learning in Python and how to predict prices
by using Support Vector Classification.
The model divides objects into two groups:
"the price will grow on the next bar" and "the price will fall on the next bar".
The input data for the model are Open-Close and High-Low.
The model tries to define whether the price is growing or falling on the basis of the input data.

The sample script performs the following:
1. Logs in.
2. Requests historical prices with the specified timeframe for the specified dates/number of bars. 
3. Transforms the historical prices into the format required for the classification.
4. Divides all historical prices into a train set and a test set based on the specified percentage.
6. Runs the classification for the train set bars.
7. Calculates and prints the accuracy score for the train set and test set bars.
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
The date/time is in the UTC time zone. If not specified, will load the number of bars defined by -quotescount.

 
-dateto

The date/time until which you want to receive historical prices. The format is "%m.%d.%Y %H:%M:%S". Optional parameter.
The date/time is in the UTC time zone. If not specified, will load bars up to now.

-quotescount
The maximum number of bars. Optional parameter.
The default value is 300

-TrainPercent
The percentage of data for the train set. Optional parameter.
The default value is 80
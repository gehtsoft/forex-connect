PricePredictionMLregressionMA Sample Script


Brief
===============================================================================
This sample script shows how to use Machine Learning in Python and how to predict prices by using Linear Regression.
The input of the regression model is three Moving Averages calculated based on close prices.
The algorithm uses a linear model to minimize the residual sum of squares 
between the observed responses in the dataset and the responses predicted by the linear approximation.

The sample script performs the following:
1. Logs in.
2. Requests historical prices with the specified timeframe for the specified dates/number of bars. 
3. Transforms the historical prices into the format required for the model.
4. Calculates three Moving Averages with the specified number of bars.
5. Divides all historical prices into a train set and a test set based on the specified percentage.
6. Runs Linear Regression for the train set.
7. Predicts close prices for the test set bars.
8. Prints coefficients of Linear Regression, Mean Square Error and Variance Score
calculated for the predicted close prices and the historical close prices received initially for the test set bars.
9. Logs out.


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

-PeriodMA1
The number of periods of the first Moving Average. Optional parameter.
The default value is 5

-PeriodMA2
The number of periods of the second Moving Average. Optional parameter.
The default value is 10

-PeriodMA3
The number of periods of the third Moving Average. Optional parameter.
The default value is 15

-TrainPercent
The percentage of data for the train set. Optional parameter.
The default value is 80
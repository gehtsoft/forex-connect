LiveChartDataExport Sample Script

Brief
===============================================================================
This sample script shows how to exports instrument historical prices to CSV file and continue to build live quotes.
It can perform live price history export for multiple instruments and timeframe combinations at the same time.
The sample script uses an XML configuration file specifying all parameters to be used by the script.

The utility performs the following actions:
1. Logs in.
2. Gets price history for specified in the configuration file instrument(s), timeframe(s), and number of bars.
3. Exports the price history to a CSV file(s). 
4. Continiously prints live history to the console.
5. Once new bar formed, adds it to the specified CSV file.



Running the Sample Script
===============================================================================
Prerequisites:
    - ForexConnect API Python must be installed.
    - common_samples folder must be in the same directory as the sample script.
    - The configuration file must be in the same directory the sample script.

If Python is added to the PATH Environmental Variable,
to run the sample script, execute the following command in a console:
python <Sample.py> <Arguments>

Arguments
===============================================================================
-p
Your password.


Parameters to be specified in the configuration file
===============================================================================
<Login></Login>
Your username.

<Url></Url>
The server URL. For example, http://www.fxcorporate.com/Hosts.jsp.

<Connection></Connection>
The connection name. For example, Demo or Real.

<SessionID></SessionID>
The database name. Required only for users who have accounts in more than one database.

<Pin></Pin>
Your PIN code. Required only for users who have PIN codes.

<OutputDir></OutputDir>
The directory where the script will export the CSV files to. For example C:\DataExport. Optional parameter.
Default value is directory with the sample script.
    
<Delimiter></Delimiter> 
A CSV values delimeter. For example, , or ;

<DateTimeSeparator></DateTimeSeparator>
A string separating the date and time values of the timestamp. For example, | or :

<FormatDecimalPlaces></FormatDecimalPlaces>
Parameter specifying if all values will have the same number of decimal places, with trailing zeroes added as needed.
Possible values are Y or N. Optional parameter.
Default value is N. 
   
<History></History>
Container for parameters specifying price history for each exporting instrument.
You should have separate container for each instrument/timeframe.

	<Instrument></Instrument>
	The instrument you want to export to CSV. Optional parameter.
	The default value is EUR/USD.

	<Timeframe></Timeframe>
	The time period that forms a single bar. For example, m1 - for 1 minute, H1 - for 1 hour.

	<Filename></Filename>
	The name of the file to which the price history will be exported to. For example, USD_JPY_m1.csv

	<NumBars></NumBars>
	The number of bars for the price history to be exported (before adding new bars).

	<Headers></Headers>
	The headers for exported price history. For example, DateTime|Bid Open|Bid Close|Ask High|Ask Low|Volume

All the parameters should be specified inside <configuration> and <Settings> tags:
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <Settings>
    ...
  </Settings>
</configuration>
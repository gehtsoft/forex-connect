RemoveQuotes application

Brief
===============================================================================
This sample shows how to remove instrument prices that are kept in the local storage.

Building the application
===============================================================================
To build this application, you will need Java SDK 1.7 and Apache Ant.
You can download Java SDK from http://www.oracle.com/technetwork/java/javase/downloads
You can download Apache Ant from http://ant.apache.org/bindownload.cgi

In your command prompt, go to the current directory and type:
./fxbuild.sh -  on Linux and
fxbuild.bat - on Windows.

This will generate the directory structure .\build\classes\ and place the compiled files there.

Running the application
===============================================================================
In your command prompt, go to the current directory and type:
./fxrun.sh - on Linux
fxrun.bat - on Windows.

All arguments must be passed from the command line -
this will run the application and display the output in your console.

You can run the application with no arguments, this will show the
application Help and information about historical prices from the local storage.

Arguments
===============================================================================
/login | --login | /l | -l
Your user name.

/password | --password | /p | -p
Your password.

/url | --url | /u | -u
The server URL. For example, http://www.fxcorporate.com/Hosts.jsp.

/connection | --connection | /c | -c
The connection name. For example, "Demo" or "Real".

/sessionid | --sessionid 
The database name. This argument is optional. 
Required only for users who have accounts in more than one database.

/pin | --pin 
Your pin code. This argument is optional. Required only for users who have a pin. 

<instrument>
The instrument that you want to use in the sample. For example, "EUR/USD".

<year>
The year for which you want to remove historical prices from the local storage.

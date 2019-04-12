RemoveQuotes application

Brief
===============================================================================
This sample shows how to remove instrument prices that are kept in the local storage.

Building the application
===============================================================================
Windows:
    To build this application, you will need MS Visual Studio 2015 or later.

    To build the application, in MS Visual Studio, either run fxbuild.bat or select "build".

Linux/MacOS:
    To build this application, you will need:
        gcc-4.3 or later
        g++-4.3 or later
        CMake 2.6 or later
        
To build the application, run fxbuild.sh.

Running the application
===============================================================================
You can run this application from the bin directory.
Also you can run this application by executing fxrun.bat or fxrun.sh on Windows
and Linux respectively.
All arguments must be passed from the command line -
this will run the application and display the output in your console.

You can run the application with no arguments, this will show the
application Help and information about historical prices from the local storage.

Arguments
===============================================================================
<instrument>
The instrument that you want to use in the sample. For example, "EUR/USD".

<year>
The year for which you want to remove historical prices from the local storage.

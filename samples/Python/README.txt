ForexConnect API is a trading API for the FXCM Group: https://www.fxcm.com/uk/

ForexConnect API Python provides an ability to create analytics and trading applications in Python.
Fuctionality of ForexConnect API includes: downloading historical prices,
creating all of the available order types, getting offers, 
managing positions, getting account reports, and more.

To use ForexConnect API, you need to have an account with the FXCM Group.



Documentation and Support
===============================================================================

Sample scripts for ForexConnect API: https://github.com/gehtsoft/forex-connect/tree/master/samples

ForexConnect API forum: http://fxcodebase.com/code/viewforum.php?f=37

Online ForexConnect API documentation: http://fxcodebase.com/bin/forexconnect/1.6.0/python/web-content.html



Prerequisites
===============================================================================

Operating system: Windows 7 or newer, Mac OS X, CentOS 7, or Ubuntu 18.04
Python 3.5, 3.6, or 3.7


Installation of ForexConnect API
===============================================================================

To install ForexConnect API from PyPI repository:

	With Python added to the PATH Environmental Variable,

	1. Install the forexconnect library:
	python -m pip install forexconnect

	2. Install all the required dependencies from requirements.txt:
	python -m pip install -r requirements.txt

	You can find requirements.txt file in the ../forexconnect/lib/ folder or 
	download it from https://github.com/gehtsoft/forex-connect/blob/master/requirements.txt


To install forexconnect from a .whl file
	
	With Python added to the PATH Environmental Variable,
	
	1. Install the forexconnect library:
	python -m pip install <forexconnect wheel file name>

	2. Make sure all the required dependencies are installed.


Required dependencies:
numpy==1.14.5
pandas==0.23.4
python-dateutil==2.7.3
pytz==2018.5
six==1.11.0

The ForexConnect API is an API designed to trade Forex and CFD instruments.


ForexConnect API directory
===============================================================================

 * ForexConnect.framework - an Objective-C framework for an iOS device and simulator.           

 * Swift samples - Xcode projects showing how the framework is used:
	** GetHistory.Swift - shows how display a chart with a price history for a symbol.
	** GetReportURL.Swift - shows how to open a Combined Account Statement report in a browser.
	** iTradeStation(non-TM).Swift - shows how to display the Offers table and create an order.
	** iTradeStation(TM).Swift - shows how to display trading tables and create an order.

 * license.txt - software license information.

 * readme.txt - this file.


Prerequisites
===============================================================================

    Mac OS X v10.12 or later
    Xcode v8.3.3 or later
    iOS SDK v6

To build Swift samples you need Xcode 9.2 or later with Swift 4.1 support.

To build Swift samples using Xcode 10.* you should perform the following steps:
- Open the necessary sample project via Xcode 10.*.
- On the File menu, click Project Settings, and the click Build System.
- In the Build system list from Per-User Project Settings section, select Legacy Build System and click Done.
- Build project.

Also Apple Developer Program membership is needed to distribute your applications for iOS.
See https://developer.apple.com/programs/ for Apple Developer Program information.


Installation of ForexConnect API
===============================================================================

1. Go to the fxcodebase.com download page to find the latest ForexConnect API package for iOS.

2. Download the package to your Mac computer.

3. Select the file ForexConnectAPI-1.6.0-iOS.tar.gz in Downloads and unpack it by double-clicking.


Including ForexConnect.framework in your Xcode project for iOS (Objective-C)
===============================================================================

1. Choose Project > Add to Project and then select the framework directory. 
Alternatively, you can control-click your project group and choose Add Files > 
Existing Frameworks from the context menu. When you add the framework to your 
project, Xcode asks you to associate it with one or more targets in your project. 
Once associated, Xcode automatically links the framework against the resulting 
executable.

2. Select and add the libz.a and libiconv.a to your project.

3. Include framework header files in your code using the directive 
#import "ForexConnect/ForexConnect.h".



Read more about ForexConnect API at: http://fxcodebase.com/documents/ForexConnectAPI/web-content.html
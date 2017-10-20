The ForexConnect API is an API designed to trade Forex and CFD instruments.

The ForexConnect API online documentation is availaible at:
http://fxcodebase.com/documents/ForexConnectAPI/web-content.html


Recommended system requirements:

    Mac OS X v 10.12 or later
    Xcode v 8 or later
    iOS SDK v 5.2

To build Swift samples you need Xcode 8.3.3 or later with Swift 3.2 or 4.0 support.
  
Also iOS Developer Program membership is needed to develop and distribute your 
applications for iOS.
See http://developer.apple.com/programs/ios/ for iOS Developer Program information.


Installation

1. Go to the fxcodebase.com download page to find the latest ForexConnect API package for iOS.

2. Download the package to your Mac computer.

3. Select the file ForexConnectAPI_1.5.0-iOS.tar.gz in Downloads and unpack it by 
double-clicking.


The unpacked directory includes:

 * ForexConnect.framework - an Objective-C framework for an iOS device and simulator.           

 * iOS samples - an iOS samples are Xcode applications showing how the framework is used.  

 * license.txt - software license information.

 * readme.txt - this file.


Quick start

1. Open the ForexConnectAPI_1.5.0-iOS directory in Finder, then open any iOS sample and 
double-click a xcodeproj file to open the sample Xcode application.

2. Click the "Build and Run button" to build and execute the sample application 
in the simulator.

3. In the Login settings, type your username and password and then click the 
"Connect" button.


How to include the ForexConnect.framework in your Xcode project for iOS:

1. Choose Project > Add to Project and then select the framework directory. 
Alternatively, you can control-click your project group and choose Add Files > 
Existing Frameworks from the context menu. When you add the framework to your 
project, Xcode asks you to associate it with one or more targets in your project. 
Once associated, Xcode automatically links the framework against the resulting 
executable.

2. Select and add the libz.dylib, libiconv.dylib and Security frameworks to your project.

3. Include framework header files in your code using the directive 
#import "ForexConnect/ForexConnect.h".

To learn more about the ForexConnect API interface read the online documentation.

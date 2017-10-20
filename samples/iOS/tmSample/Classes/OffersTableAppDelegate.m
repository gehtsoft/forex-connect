
#import "TablesController.h"
#import "LoginViewController.h"
#import "OffersTableAppDelegate.h"

NSString *kUserNameKey          = @"userNameKey";
NSString *kPasswordKey          = @"passwordKey";
NSString *kConnectionKey        = @"connectionKey";
NSString *kHostKey              = @"hostKey";

@implementation OffersTableAppDelegate

@synthesize window;
@synthesize navigationController;
@synthesize userNameValue, passwordValue, connectionValue, hostValue;


#pragma mark -
#pragma mark Application lifecycle

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {    
    
    // Override point for customization after application launch.
    [self setupByPreferences];
    [CTablesController getInstance];
    
    // Add the navigation controller's view to the window and display.
    [self.window setRootViewController:navigationController];
    [self.window makeKeyAndVisible];
    
    return YES;
}


- (void)applicationWillResignActive:(UIApplication *)application {
    /*
     Sent when the application is about to move from active to inactive state. This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) or when the user quits the application and it begins the transition to the background state.
     Use this method to pause ongoing tasks, disable timers, and throttle down OpenGL ES frame rates. Games should use this method to pause the game.
     */
}


- (void)applicationDidEnterBackground:(UIApplication *)application {
    /*
     Use this method to release shared resources, save user data, invalidate timers, and store enough application state information to restore your application to its current state in case it is terminated later. 
     If your application supports background execution, called instead of applicationWillTerminate: when the user quits.
     */
}


- (void)applicationWillEnterForeground:(UIApplication *)application {
    /*
     Called as part of  transition from the background to the inactive state: here you can undo many of the changes made on entering the background.
     */
}


- (void)applicationDidBecomeActive:(UIApplication *)application {
    /*
     Restart any tasks that were paused (or not yet started) while the application was inactive. If the application was previously in the background, optionally refresh the user interface.
     */
}


- (void)applicationWillTerminate:(UIApplication *)application {
    /*
     Called when the application is about to terminate.
     See also applicationDidEnterBackground:.
     */
}


#pragma mark -
#pragma mark Memory management

- (void)applicationDidReceiveMemoryWarning:(UIApplication *)application {
    /*
     Free up as much memory as possible by purging cached data objects that can be recreated (or reloaded from disk) later.
     */
}


- (void)dealloc {
    CTablesController *pO2g2 = [CTablesController getInstance];
    if ([pO2g2 isLoggedIn])
    {
        [pO2g2 logout];
    }
    [pO2g2 release];
    [navigationController release];
    [window release];
    [userNameValue release];
    [passwordValue release];
    [connectionValue release];
    [hostValue release];
    [super dealloc];
}

- (void)setupByPreferences
{
    NSString *testValue = [[NSUserDefaults standardUserDefaults] stringForKey:kConnectionKey];
    if (testValue == nil)
    {
        // no default values have been set, create them here based on what's in our Settings bundle info
        //
        NSString *pathStr = [[NSBundle mainBundle] bundlePath];
        NSString *settingsBundlePath = [pathStr stringByAppendingPathComponent:@"Settings.bundle"];
        NSString *finalPath = [settingsBundlePath stringByAppendingPathComponent:@"Root.plist"];
        
        NSDictionary *settingsDict = [NSDictionary dictionaryWithContentsOfFile:finalPath];
        NSArray *prefSpecifierArray = [settingsDict objectForKey:@"PreferenceSpecifiers"];
        
        NSString *userNameDefault = nil;
        NSString *passwordDefault = nil;
        NSString *connectionDefault = nil;
        NSString *hostDefault = nil;
        
        NSDictionary *prefItem;
        for (prefItem in prefSpecifierArray)
        {
            NSString *keyValueStr = [prefItem objectForKey:@"Key"];
            id defaultValue = [prefItem objectForKey:@"DefaultValue"];
            
            if ([keyValueStr isEqualToString:kUserNameKey])
            {
                userNameDefault = defaultValue;
            }
            else if ([keyValueStr isEqualToString:kPasswordKey])
            {
                passwordDefault = defaultValue;
            }
            else if ([keyValueStr isEqualToString:kConnectionKey])
            {
                connectionDefault = defaultValue;
            }
            else if ([keyValueStr isEqualToString:kHostKey])
            {
                hostDefault = defaultValue;
            }
        }
        
        // since no default values have been set (i.e. no preferences file created), create it here     
        NSDictionary *appDefaults = [NSDictionary dictionaryWithObjectsAndKeys:
                                                userNameDefault, kUserNameKey,
                                                passwordDefault, kPasswordKey,
                                                connectionDefault, kConnectionKey,
                                                hostDefault, kHostKey,
                                     nil];
 
        [[NSUserDefaults standardUserDefaults] registerDefaults:appDefaults];
        [[NSUserDefaults standardUserDefaults] synchronize];
     }
 
    // we're ready to go, so lastly set the key preference values
    userNameValue = [[NSUserDefaults standardUserDefaults] stringForKey:kUserNameKey];
    passwordValue = [[NSUserDefaults standardUserDefaults] stringForKey:kPasswordKey];
    connectionValue = [[NSUserDefaults standardUserDefaults] stringForKey:kConnectionKey];
    hostValue = [[NSUserDefaults standardUserDefaults] stringForKey:kHostKey];
 }

- (void)savePreferences
{
    [[NSUserDefaults standardUserDefaults] setObject:userNameValue forKey:kUserNameKey];
    [[NSUserDefaults standardUserDefaults] setObject:passwordValue forKey:kPasswordKey];
    [[NSUserDefaults standardUserDefaults] setObject:connectionValue forKey:kConnectionKey];
    [[NSUserDefaults standardUserDefaults] setObject:hostValue forKey:kHostKey];
}
 @end

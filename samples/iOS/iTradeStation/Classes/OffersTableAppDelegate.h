
#import <UIKit/UIKit.h>

@interface OffersTableAppDelegate : NSObject <UIApplicationDelegate> {

    UIWindow *window;
    UINavigationController *navigationController;
    NSString *userNameValue;
    NSString *passwordValue;
    NSString *connectionValue;
    NSString *hostValue;
@public
    BOOL isLoggedIn;
}

@property (nonatomic, retain) IBOutlet UIWindow *window;
@property (nonatomic, retain) IBOutlet UINavigationController *navigationController;

@property (nonatomic, retain) NSString *userNameValue;
@property (nonatomic, retain) NSString *passwordValue;
@property (nonatomic, retain) NSString *connectionValue;
@property (nonatomic, retain) NSString *hostValue;

- (void)setupByPreferences;
- (void)savePreferences;


@end


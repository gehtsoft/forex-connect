
#import "RootViewController.h"


@interface AccountsViewController : UIViewController <UITextFieldDelegate> {
    IBOutlet UITextField *userNameField;
    IBOutlet UITextField *passwordField;
    IBOutlet UITextField *connectionField;
    IBOutlet UITextField *hostField;
    IBOutlet UIButton *okButton;
    RootViewController *rootViewController;
}

@property (nonatomic, retain) UITextField *userNameField;
@property (nonatomic, retain) UITextField *passwordField;
@property (nonatomic, retain) UITextField *connectionField;
@property (nonatomic, retain) UITextField *hostField;
@property (nonatomic, retain) UIButton *okButton;
@property (nonatomic, retain) RootViewController *rootViewController;

- (IBAction)okPressed;
- (void)changeOkButtonStatus:(NSNotification*)notification;


@end


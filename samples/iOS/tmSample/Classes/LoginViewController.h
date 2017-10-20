
#import "IndexViewController.h"


@interface LoginViewController : UIViewController  <UITextFieldDelegate> {
    IBOutlet UITextField *userNameField;
    IBOutlet UITextField *passwordField;
    IBOutlet UITextField *connectionField;
    IBOutlet UITextField *hostField;
    IBOutlet UIButton *okButton;
    IndexViewController *indexViewController;
}

@property (nonatomic, retain) UITextField *userNameField;
@property (nonatomic, retain) UITextField *passwordField;
@property (nonatomic, retain) UITextField *connectionField;
@property (nonatomic, retain) UITextField *hostField;
@property (nonatomic, retain) UIButton *okButton;

- (IBAction)okPressed;

@end


#import "LoginViewController.h"
#import "TablesController.h"
#import "IndexViewController.h"
#import "OffersTableAppDelegate.h"

@implementation LoginViewController


@synthesize userNameField, passwordField, connectionField, hostField, okButton;

#pragma mark -
#pragma mark View lifecycle

- (void)viewWillAppear:(BOOL)animated
{
    
    if ([[CTablesController getInstance] isLoggedIn])
    {
        self.title = @"Relogin settings";
        [[CTablesController getInstance] logout];
    }

    [super viewWillAppear:animated];
}

- (void)viewDidLoad {
    [super viewDidLoad];

    self.title = @"Login settings";

    OffersTableAppDelegate *appDelegate = (OffersTableAppDelegate *)[[UIApplication sharedApplication] delegate];
    userNameField.text = appDelegate.userNameValue;
    passwordField.text = appDelegate.passwordValue;
    connectionField.text = appDelegate.connectionValue;
    hostField.text = appDelegate.hostValue;
    indexViewController = [[IndexViewController alloc] initWithNibName:@"IndexViewController" bundle:nil];
}

- (BOOL)textFieldShouldReturn:(UITextField *)textField
{
    // the user pressed the "Done" button, so dismiss the keyboard
    [textField resignFirstResponder];
    return YES;
}


-(IBAction)okPressed
{
    OffersTableAppDelegate *appDelegate = (OffersTableAppDelegate *)[[UIApplication sharedApplication] delegate];
    appDelegate.userNameValue = userNameField.text;
    appDelegate.passwordValue = passwordField.text;
    appDelegate.connectionValue = connectionField.text;
    appDelegate.hostValue = hostField.text;
    
    CTablesController *pO2g = [CTablesController getInstance];

    // set CA (Certificate Authority) info for https connections (optional)
    [pO2g setCAInfo:[[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent: @"cacert.pem"]];

    [pO2g login:appDelegate.userNameValue :appDelegate.passwordValue
                      :[NSString stringWithFormat: @"%@/Hosts.jsp", appDelegate.hostValue]
                      :appDelegate.connectionValue];

    [appDelegate savePreferences];
    
    self.title = @"Logout";

    [[appDelegate navigationController] pushViewController:indexViewController animated:YES];
    
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation {
    // Override to allow orientations other than the default portrait orientation.
    return YES;
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
}

- (void)viewDidUnload 
{
    [indexViewController release];
}


- (void)dealloc {
    [super dealloc];
}


@end


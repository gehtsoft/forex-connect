#import "AccountsViewController.h"
#import "OffersController.h"
#import "OffersTableAppDelegate.h"


@interface AccountsViewController ()

@property(nonatomic) BOOL okButtonIsEnable;

@end


@implementation AccountsViewController

@synthesize userNameField, passwordField, connectionField, hostField, okButton, rootViewController;

#pragma mark -
#pragma mark View lifecycle

- (void)viewWillAppear:(BOOL)animated
{

    NSNotificationCenter *thisCenter = [NSNotificationCenter defaultCenter];
    [thisCenter addObserver:self selector:@selector(changeOkButtonStatus:) name:@"UIChangeOKButtonStatusNotification" object:nil];
    
    
    OffersTableAppDelegate *appDelegate = (OffersTableAppDelegate *)[[UIApplication sharedApplication] delegate];
	if (appDelegate->isLoggedIn == YES)
	{
		self.title = @"Relogin settings";
		COffersController::getInstance()->logout();
		appDelegate->isLoggedIn = NO;
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

/*	if (appDelegate->isLoggedIn == YES)
	{
		self.title = @"Relogin settings";
		logoutO2G2();
		O2AtUnLoad();
		appDelegate->isLoggedIn = NO;
	}*/

    // Uncomment the following line to preserve selection between presentations.
    // self.clearsSelectionOnViewWillAppear = NO;
 
    // Uncomment the following line to display an Edit button in the navigation bar for this view controller.
    // self.navigationItem.rightBarButtonItem = self.editButtonItem;
}

- (void)changeOkButtonStatus:(NSNotification*)notification
{
    NSDictionary *info = notification.userInfo;
    if ([info respondsToSelector:@selector(objectForKey:)]) {
        BOOL isEnabled = [[info objectForKey:@"okButtonIsEnable"] boolValue];
        NSLog(@"\"OK\" button is %@",  isEnabled ? @"enabled" : @"disabled");
        okButton.enabled = isEnabled;
    }
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
	
	COffersController *pO2g = COffersController::getInstance();
	pO2g->setLoginData([appDelegate.userNameValue UTF8String], [appDelegate.passwordValue UTF8String],
					   [[NSString stringWithFormat: @"%@/Hosts.jsp", appDelegate.hostValue] UTF8String],
					   [appDelegate.connectionValue UTF8String]);

        // set CA (Certificate Authority) info for https connections (optional)
        pO2g->setCAInfo([[[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent: @"cacert.pem"] UTF8String]);

	pO2g->login();

	appDelegate->isLoggedIn = YES;

	[appDelegate savePreferences];
	
	rootViewController = [[RootViewController alloc] initWithNibName:@"RootViewController" bundle:nil];
	
	self.title = @"Logout";

	[[appDelegate navigationController] pushViewController:rootViewController animated:YES];
	
}

/*
- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
}
*/
/*
- (void)viewDidAppear:(BOOL)animated {
    [super viewDidAppear:animated];
}
*/
/*
- (void)viewWillDisappear:(BOOL)animated {
    [super viewWillDisappear:animated];
}
*/
/*
- (void)viewDidDisappear:(BOOL)animated {
    [super viewDidDisappear:animated];
}
*/


- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation {
    // Override to allow orientations other than the default portrait orientation.
    return YES;
}


#pragma mark -
#pragma mark Table view data source

/*- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView {
    // Return the number of sections.
    return 1;
}


- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section {
    // Return the number of rows in the section.
    return 1;
}


// Customize the appearance of table view cells.
- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath {
    
    static NSString *CellIdentifier = @"Cell";
    
    UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:CellIdentifier];
    if (cell == nil) {
        cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:CellIdentifier] autorelease];
    }
    
    // Configure the cell...
    
    return cell;
}
*/

/*
// Override to support conditional editing of the table view.
- (BOOL)tableView:(UITableView *)tableView canEditRowAtIndexPath:(NSIndexPath *)indexPath {
    // Return NO if you do not want the specified item to be editable.
    return YES;
}
*/


/*
// Override to support editing the table view.
- (void)tableView:(UITableView *)tableView commitEditingStyle:(UITableViewCellEditingStyle)editingStyle forRowAtIndexPath:(NSIndexPath *)indexPath {
    
    if (editingStyle == UITableViewCellEditingStyleDelete) {
        // Delete the row from the data source.
        [tableView deleteRowsAtIndexPaths:[NSArray arrayWithObject:indexPath] withRowAnimation:UITableViewRowAnimationFade];
    }   
    else if (editingStyle == UITableViewCellEditingStyleInsert) {
        // Create a new instance of the appropriate class, insert it into the array, and add a new row to the table view.
    }   
}
*/


/*
// Override to support rearranging the table view.
- (void)tableView:(UITableView *)tableView moveRowAtIndexPath:(NSIndexPath *)fromIndexPath toIndexPath:(NSIndexPath *)toIndexPath {
}
*/


/*
// Override to support conditional rearranging of the table view.
- (BOOL)tableView:(UITableView *)tableView canMoveRowAtIndexPath:(NSIndexPath *)indexPath {
    // Return NO if you do not want the item to be re-orderable.
    return YES;
}
*/


#pragma mark -
#pragma mark Table view delegate

//- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath {
    // Navigation logic may go here. Create and push another view controller.
    /*
    <#DetailViewController#> *detailViewController = [[<#DetailViewController#> alloc] initWithNibName:@"<#Nib name#>" bundle:nil];
    // ...
    // Pass the selected object to the new view controller.
    [self.navigationController pushViewController:detailViewController animated:YES];
    [detailViewController release];
    */
//}


#pragma mark -
#pragma mark Memory management

- (void)didReceiveMemoryWarning {
    // Releases the view if it doesn't have a superview.
    [super didReceiveMemoryWarning];
    
    // Relinquish ownership any cached data, images, etc. that aren't in use.
}

- (void)viewDidUnload {
    // Relinquish ownership of anything that can be recreated in viewDidLoad or on demand.
    // For example: self.myOutlet = nil;
	//rootViewController = nil;
}


- (void)dealloc {
    [super dealloc];
	[rootViewController release];
}


@end


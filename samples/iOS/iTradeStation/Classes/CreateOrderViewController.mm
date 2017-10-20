
#import "CreateOrderViewController.h"


@implementation CreateOrderViewController


// The designated initializer.  Override if you create the controller programmatically and want to perform customization that is not appropriate for viewDidLoad.
- (id)initWithMyNibName:(NSString *)nibNameOrNil offerIndex:(NSInteger) index {//bundle:(NSBundle *)nibBundleOrNil {
    self = [super initWithNibName:nibNameOrNil bundle:nil];
    if (self) {
        // Custom initialization.
		self.offerIndex = index;
		pO2g = COffersController::getInstance();
    }
    return self;
}

@synthesize instrumentLabel, sellBuyControl, amountField, amountSlider, rateField, rateSlider, okButton, cancelButton//;
			,orderTypeControl,offerIndex;

// Implement viewDidLoad to do additional setup after loading the view, typically from a nib.
- (void)viewDidLoad {
    [super viewDidLoad];
	BOOL iPad = NO;
#ifdef UI_USER_INTERFACE_IDIOM
	iPad = (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad);
#endif
    if (iPad)
	{
		CGRect frame = [[UIScreen mainScreen] bounds];
		self.view.frame = frame;
	}
    /*if (iPad)
		orderTypeControl.segmentedControlStyle = UISegmentedControlStylePlain;*/
	amountSlider.minimumValue = 0.0;
	amountSlider.maximumValue = 99.0;
	amountSlider.value = 10.0;
	amountField.delegate = self;
	rateField.delegate = self;
	NSLog(@"Offer index %d", offerIndex);
	instrumentLabel.text = [NSString stringWithCString:(pO2g->getInstrumentText(offerIndex)) encoding:NSASCIIStringEncoding];
	[self refreshData];
	amountField.text = @"100";
	self.title = @"Create order";
	// create a custom navigation bar button and set it to always say "Back"
	/*UIBarButtonItem *temporaryBarButtonItem = [[UIBarButtonItem alloc] init];
	temporaryBarButtonItem.title = @"Back";
	self.navigationItem.backBarButtonItem = temporaryBarButtonItem;
	[temporaryBarButtonItem release];*/
}
/*
- (void)setOfferIndex:(int) index
{
	self->offerIndex = index;
}


- (int)offerIndex
{
	return self->offerIndex;
}*/
- (IBAction)canselPressed:(UIButton *)sender {
	[self.navigationController popViewControllerAnimated:YES];

}
- (IBAction)okPressed:(UIButton *)sender {
    
	// order creation
	pO2g->createOrder(offerIndex, (bool)sellBuyControl.selectedSegmentIndex, [amountField.text intValue] * 1000,
					  [rateField.text doubleValue], orderTypeControl.selectedSegmentIndex);
	[self.navigationController popViewControllerAnimated:YES];
	
}


- (void)refreshData
{
	double rateFieldValue;
	NSString *formatStr = [NSString stringWithFormat: @"%%.%df", pO2g->getDigits(offerIndex)];
	
	if (sellBuyControl.selectedSegmentIndex == 0)
		rateFieldValue =  pO2g->getBid(offerIndex);
	else
		rateFieldValue = pO2g->getAsk(offerIndex);
	rateField.text = [NSString stringWithFormat: formatStr, rateFieldValue];
	rateSlider.minimumValue = rateFieldValue * 0.95;
	rateSlider.maximumValue = rateFieldValue * 1.05;
	rateSlider.value = rateFieldValue;
}

- (IBAction)sellBuySwitched
{
	[self refreshData];
}


- (IBAction)amountSliderChanged
{
	
	amountField.text = [NSString stringWithFormat: @"%ld", 10 + lround(amountSlider.value) * 10];
}

- (IBAction)rateSliderChanged
{
	NSString *formatStr = [NSString stringWithFormat: @"%%.%df", pO2g->getDigits(offerIndex)];
	rateField.text = [NSString stringWithFormat: formatStr, rateSlider.value];
}

- (IBAction)amountFieldChanged
{
	double amountFieldValue = [amountField.text intValue] / 10;
	[amountSlider setValue: amountFieldValue animated: YES];
}

- (IBAction)rateFieldChanged
{
	double rateFieldValue = [rateField.text doubleValue];
	rateSlider.minimumValue = rateFieldValue * 0.95;
	rateSlider.maximumValue = rateFieldValue * 1.05;
	[rateSlider setValue: rateFieldValue animated: YES];
}

- (BOOL)textFieldShouldReturn:(UITextField *)textField
{
	// the user pressed the "Done" button, so dismiss the keyboard
	[textField resignFirstResponder];
	return YES;
}

/*
// Override to allow orientations other than the default portrait orientation.
- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation {
    // Return YES for supported orientations.
    return (interfaceOrientation == UIInterfaceOrientationPortrait);
}
*/

- (void)didReceiveMemoryWarning {
    // Releases the view if it doesn't have a superview.
    [super didReceiveMemoryWarning];
    
    // Release any cached data, images, etc. that aren't in use.
}

- (void)viewDidUnload {
    [super viewDidUnload];
    // Release any retained subviews of the main view.
    // e.g. self.myOutlet = nil;
}


- (void)dealloc {
    [super dealloc];
}

/*
// Customize the number of sections in the table view.
- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView {
    return 1;
}


// Customize the number of rows in the table view.
- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section {
    return 1;
}


// Customize the appearance of table view cells.
- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath {
}

- (CGFloat)tableView:(UITableView *)tableView heightForRowAtIndexPath:(NSIndexPath *)indexPath
{
	return 600.0;
}*/

@end


#import "RootViewController.h"
#import "CreateOrderViewController.h"
#include <iostream>

@implementation RootViewController


#pragma mark -
#pragma mark View lifecycle


- (void)viewDidLoad {

    [super viewDidLoad];
	COffersController *pO2g2 = COffersController::getInstance();

	self.title = @"Connecting...";

	myWrapper = (idWrapper *)malloc(sizeof(idWrapper));
	myWrapper->target = self;
	myWrapper->selector = @selector(myReloadData);
	myWrapper->param = YES;
	myWrapper->call = (objc_call)[self methodForSelector: @selector(myReloadData)];
	pO2g2->subscribe(myWrapper);
	
	// Uncomment the following line to display an Edit button in the navigation bar for this view controller.
    // self.navigationItem.rightBarButtonItem = self.editButtonItem;
	
	// create a custom navigation bar button and set it to always say "Back"
	/*UIBarButtonItem *temporaryBarButtonItem = [[UIBarButtonItem alloc] init];
	 temporaryBarButtonItem.title = @"Logout";
	 self.navigationItem.backBarButtonItem = temporaryBarButtonItem;
	 [temporaryBarButtonItem release];*/
	
	//self.navigationItem.backBarButtonItem.title = @"Logout";
}

- (void)myReloadData {
    NSAutoreleasePool * pool = [[NSAutoreleasePool alloc] init];
	[self.view performSelectorOnMainThread: @selector(reloadData) withObject: nil waitUntilDone: NO];
	self.title = [NSString stringWithCString:(COffersController::getInstance()->getStatusText()) encoding:NSASCIIStringEncoding];
    [pool release];
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

/*
 // Override to allow orientations other than the default portrait orientation.
- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation {
	// Return YES for supported orientations.
	return (interfaceOrientation == UIInterfaceOrientationPortrait);
}
 */


#pragma mark -
#pragma mark Table view data source

// Customize the number of sections in the table view.
- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView {
    return 1;
}


// Customize the number of rows in the table view.
- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section {
    return COffersController::getInstance()->size();
}

- (CGFloat)tableView: (UITableView *)tableView heightForHeaderInSection:(NSInteger)section {
	return 40.0;
}


- (UIView *)tableView :(UITableView *)tableView viewForHeaderInSection:(NSInteger)section {
	BOOL iPad = NO;
#ifdef UI_USER_INTERFACE_IDIOM
	iPad = (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad);
#endif
	if (headerLabel == nil)
	{
        headerLabel = [[UILabel alloc] initWithFrame:CGRectMake(30.0, 10.0, 600.0, 25.0)] ;
        headerLabel.tag = 11;
        headerLabel.font = [UIFont boldSystemFontOfSize:18.0];
		headerLabel.textAlignment = NSTextAlignmentLeft;
        headerLabel.textColor = [UIColor blackColor];
        headerLabel.backgroundColor = [UIColor lightGrayColor];
        headerLabel.autoresizingMask = UIViewAutoresizingFlexibleHeight;
		if (iPad)
			headerLabel.text = @"      Symbol                Bid             Ask                                   Low            High";
		else
			headerLabel.text = @"    Symbol          Bid            Ask";
	}
	return headerLabel;
}





// Customize the appearance of table view cells.
- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath {
    
    static NSString *CellIdentifier = @"Cell";
	UILabel *bidLabel, *askLabel, *instrumentLabel, *lowLabel, *highLabel;
//	UIImageView *bidArrow, *askArrow;
	int nBidDirection, nAskDirection;
	COffersController *pO2g = COffersController::getInstance();
	BOOL iPad = NO;
#ifdef UI_USER_INTERFACE_IDIOM
	iPad = (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad);
#endif
    
    UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:CellIdentifier];
    if (cell == nil)
	{
        cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:CellIdentifier] autorelease];
		cell.accessoryType = UITableViewCellAccessoryDisclosureIndicator;
		
        instrumentLabel = [[[UILabel alloc] initWithFrame:CGRectMake(iPad ? 30.0 : 15.0, 10.0, 90.0, 25.0)] autorelease];
        instrumentLabel.tag = 1;
        instrumentLabel.font = [UIFont systemFontOfSize:16.0];
        instrumentLabel.textAlignment = NSTextAlignmentLeft;
        instrumentLabel.textColor = [UIColor blackColor];
        instrumentLabel.autoresizingMask = /*UIViewAutoresizingFlexibleLeftMargin |*/   UIViewAutoresizingFlexibleHeight
		;//			| UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleRightMargin;
        [cell.contentView addSubview:instrumentLabel];
		
		if (!iPad)
		{
			bidLabel = [[[UILabel alloc] initWithFrame:CGRectMake(100.0, 10.0, 90.0, 25.0)] autorelease];
			bidLabel.tag = 2;
			bidLabel.font = [UIFont systemFontOfSize:18.0];
			bidLabel.textAlignment = NSTextAlignmentRight;
			bidLabel.textColor = [UIColor blackColor];
			bidLabel.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleHeight
					| UIViewAutoresizingFlexibleWidth| UIViewAutoresizingFlexibleRightMargin;
			[cell.contentView addSubview:bidLabel];
			
			askLabel = [[[UILabel alloc] initWithFrame:CGRectMake(200.0, 10.0, 90.0, 25.0)] autorelease];
			askLabel.tag = 3;
			askLabel.font = [UIFont systemFontOfSize:18.0];
			askLabel.textAlignment = NSTextAlignmentRight;
			askLabel.textColor = [UIColor blackColor];
			askLabel.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleHeight
			| UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleRightMargin;
			[cell.contentView addSubview:askLabel];
		}
		else
		{
			bidLabel = [[[UILabel alloc] initWithFrame:CGRectMake(140.0, 10.0, 80.0, 25.0)] autorelease];
			bidLabel.tag = 2;
			bidLabel.font = [UIFont systemFontOfSize:18.0];
			bidLabel.textAlignment = NSTextAlignmentRight;
			bidLabel.textColor = [UIColor blackColor];
			bidLabel.autoresizingMask = UIViewAutoresizingFlexibleHeight;
			[cell.contentView addSubview:bidLabel];
		
			askLabel = [[[UILabel alloc] initWithFrame:CGRectMake(240.0, 10.0, 80.0, 25.0)] autorelease];
			askLabel.tag = 3;
			askLabel.font = [UIFont systemFontOfSize:18.0];
			askLabel.textAlignment = NSTextAlignmentRight;
			askLabel.textColor = [UIColor blackColor];
			askLabel.autoresizingMask = UIViewAutoresizingFlexibleHeight;
			[cell.contentView addSubview:askLabel];

			lowLabel = [[[UILabel alloc] initWithFrame:CGRectMake(450.0, 10.0, 80.0, 25.0)] autorelease];
			lowLabel.tag = 4;
			lowLabel.font = [UIFont systemFontOfSize:18.0];
			lowLabel.textAlignment = NSTextAlignmentRight;
			lowLabel.textColor = [UIColor blackColor];
			lowLabel.autoresizingMask = UIViewAutoresizingFlexibleHeight;
			[cell.contentView addSubview:lowLabel];
		
			highLabel = [[[UILabel alloc] initWithFrame:CGRectMake(550.0, 10.0, 80.0, 25.0)] autorelease];
			highLabel.tag = 5;
			highLabel.font = [UIFont systemFontOfSize:18.0];
			highLabel.textAlignment = NSTextAlignmentRight;
			highLabel.textColor = [UIColor blackColor];
			highLabel.autoresizingMask = UIViewAutoresizingFlexibleHeight;
			[cell.contentView addSubview:highLabel];
		}
    }
	else
	{
		instrumentLabel = (UILabel *)[cell.contentView viewWithTag:1];
		bidLabel = (UILabel *)[cell.contentView viewWithTag:2];
		askLabel = (UILabel *)[cell.contentView viewWithTag:3];
		if (iPad)
		{
			lowLabel = (UILabel *)[cell.contentView viewWithTag:4];
			highLabel = (UILabel *)[cell.contentView viewWithTag:5];
		}
	}

    
// Configure the cell.
	pO2g->isCellChanged(indexPath.row);
	//if (pO2g->isCellChanged(indexPath.row))
	{
		NSString *formatStr = [NSString stringWithFormat: @"%%.%df", pO2g->getDigits(indexPath.row)];
		instrumentLabel.text = [NSString stringWithCString:(pO2g->getInstrumentText(indexPath.row)) encoding:NSASCIIStringEncoding];
		bidLabel.text = [NSString stringWithFormat: formatStr, pO2g->getBid(indexPath.row)];
		askLabel.text = [NSString stringWithFormat: formatStr, pO2g->getAsk(indexPath.row)];
		if (iPad)
		{
			lowLabel.text = [NSString stringWithFormat: formatStr, pO2g->getLow(indexPath.row)];
			highLabel.text = [NSString stringWithFormat: formatStr, pO2g->getHigh(indexPath.row)];
		}
		
		nBidDirection = pO2g->getBidDirection(indexPath.row);
		if (nBidDirection < 0)
		    bidLabel.textColor = [UIColor redColor];
		else if (nBidDirection > 0)
		    bidLabel.textColor = [UIColor blueColor];
		else
		    bidLabel.textColor = [UIColor blackColor];
		nAskDirection = pO2g->getAskDirection(indexPath.row);
		if (nAskDirection < 0)
		    askLabel.textColor = [UIColor redColor];
		else if (nAskDirection > 0)
		    askLabel.textColor = [UIColor blueColor];
		else
		    askLabel.textColor = [UIColor blackColor];
	}
    //cell.detailTextLabel.text = [[self.menuList objectAtIndex:indexPath.row] objectForKey:kExplainKey];

    return cell;
}


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

- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath {
    
	 NSInteger Row = indexPath.row;
 	 NSLog(@"Passed row %d\n", Row);
	
	 CreateOrderViewController *detailViewController = [[CreateOrderViewController alloc] initWithMyNibName:@"CreateOrderViewController" offerIndex:Row];
 	 detailViewController.offerIndex = Row;
 	 NSInteger resultRow = detailViewController.offerIndex;
	 NSLog(@"Set row %d\n", resultRow);
     // ...
     // Pass the selected object to the new view controller.
	 [self.navigationController pushViewController:detailViewController animated:YES];
	 [detailViewController release];
	 
}


#pragma mark -
#pragma mark Memory management

- (void)didReceiveMemoryWarning {
    // Releases the view if it doesn't have a superview.
    [super didReceiveMemoryWarning];
    
    // Relinquish ownership any cached data, images, etc that aren't in use.
}

- (void)viewDidUnload {
    // Relinquish ownership of anything that can be recreated in viewDidLoad or on demand.
    // For example: self.myOutlet = nil;
	COffersController::getInstance()->unsubscribe();
	free(myWrapper);
}


- (void)dealloc {
	[headerLabel release];
    [super dealloc];
}


@end


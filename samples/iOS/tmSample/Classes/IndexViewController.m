
#import "IndexViewController.h"
#import "TablesController.h"
#import "OffersTableAppDelegate.h"

@implementation IndexViewController


#pragma mark -
#pragma mark View lifecycle


- (void)viewDidLoad {

    [super viewDidLoad];
    
    listData = [[NSArray alloc] initWithObjects: @"Accounts", @"Offers"/*, @"Orders", @"Trades", @"ClosedTrades",
                @"Summary"*/, nil];
    offerTableViewController = [[OfferViewController alloc] initWithNibName:@"OfferViewController" bundle:nil];
    accountTableViewController = [[AccountViewController alloc] initWithNibName:@"AccountViewController" bundle:nil];
    
    self.title = @"Choose a table";
}

// Customize the number of rows in the table view.
- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section {
    return [listData count];
}

// Customize the appearance of table view cells.
- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath {
    
    
    static NSString *CellIdentifier = @"Cell";
    UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:CellIdentifier];
    if (cell == nil)
    {
        cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:CellIdentifier] autorelease];
        cell.accessoryType = UITableViewCellAccessoryDisclosureIndicator;
    }
    
    NSUInteger row = [indexPath row];
    cell.textLabel.text = [listData objectAtIndex:row];
    return cell;
}

- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath {
    
     NSInteger Row = indexPath.row;
     NSLog(@"Passed row %d\n", Row);
    
    
    OffersTableAppDelegate *appDelegate = (OffersTableAppDelegate *)[[UIApplication sharedApplication] delegate];
    if (Row == 1)
    {
        [[appDelegate navigationController] pushViewController:offerTableViewController animated:YES];
    }
    else
        [[appDelegate navigationController] pushViewController:accountTableViewController animated:YES];
    
     
}



- (void)viewDidUnload
{
    [offerTableViewController release];
    [accountTableViewController release];
}


- (void)dealloc
{
    [listData release];
    [super dealloc];
}


@end


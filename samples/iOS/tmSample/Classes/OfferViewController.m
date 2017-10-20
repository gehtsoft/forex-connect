
#import "OfferViewController.h"
#import "TablesController.h"

@implementation TableItem

- (id)init:(id)newRow
{
    self = [super init];
    row = [newRow retain];
    bidDirection = 0;
    askDirection = 0;
    isChanged = YES;
    return self;
}

- (void)dropBidDirection
{
    bidDirection = 0;
    isChanged = YES;
}

- (void)dropAskDirection
{
    askDirection = 0;
    isChanged = YES;
}

- (void)dealloc
{
    [row release];
    [super dealloc];
}

@end


@implementation OfferViewController



- (void)viewDidLoad
{

    [super viewDidLoad];

    // subscribe to wait connected and table refreshed state
    CTablesController *tc = [CTablesController getInstance];
    [tc subscribe:self];
    if ([tc isTablesLoaded])
    {
        [self initTable];
        self.title = @"Offers";
    }
    else
        self.title = [tc getStatusText];
}

- (void)viewDidUnload
{
    [[CTablesController getInstance] unsubscribe:self];
    [mTable unsubscribeUpdate:Update :self];
}

- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self)
    {
        listData = [[NSMutableArray alloc] initWithCapacity:40];
        mTable = nil;
        headerLabel = nil;
    }
    return self;
}

- (void)dealloc
{
    [headerLabel release];
    [listData release];
    [mTable release];
    [super dealloc];
}

- (void)onTableStatusChanged:(NSString *)statusText :(BOOL)isTablesLoaded :(BOOL)isResetTable
{
    if (isResetTable)
        @synchronized(listData)
        {
            [listData removeAllObjects];
            if (mTable != nil)
            {
                [mTable unsubscribeUpdate:Update :self];
                [mTable release];   
                mTable = nil;
                [self myReloadData];
            }
        }
    if (isTablesLoaded)
    {
#ifdef DEBUG_OUT
        NSLog(@"Offers Table initial filling");
#endif
        self.title = @"Offers";
        // now we are ready to read table
        [self initTable];
        [self myReloadData];
    }
    else
        self.title = statusText;
}

- (void)myReloadData
{
    if ([listData count] == 0)
        [self initTable];
    [self.view performSelectorOnMainThread: @selector(reloadData) withObject: nil waitUntilDone: NO];
}

- (void)initTable
{
    @synchronized(listData)
    {
        if (mTable != nil)
        {
            [mTable unsubscribeUpdate:Update :self];
            [mTable release];
        }
        mTable = [[CTablesController getInstance] getOffersTable];
        [listData removeAllObjects];
        int size;
    
        if (mTable && (size = [mTable size]) > 0)
        {
            for (int i = 0 ; i < size ; ++i)
            {
                TableItem* item = [[TableItem alloc] init:[[mTable getRow:i] autorelease]];
                [listData addObject: item];
                [item release];
            }
        }
        
        // sort by offer id
        [listData sortUsingComparator: ^(id obj1, id obj2) 
         {
             return [[((TableItem*)obj1)->row getOfferID] localizedCaseInsensitiveCompare:[((TableItem*)obj2)->row getOfferID]];
         } 
         ];
        
        [mTable subscribeUpdate:Update :self];
    }
}
    
// Customize the number of sections in the table view.
- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView {
    return 1;
}


// Customize the number of rows in the table view.
- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section {
    return [listData count];
}

- (CGFloat)tableView:(UITableView *)tableView heightForHeaderInSection:(NSInteger)section {
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
    BOOL iPad = NO;
#ifdef UI_USER_INTERFACE_IDIOM
    iPad = (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad);
#endif
    
    UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:CellIdentifier];
    if (cell == nil)
    {
        cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:CellIdentifier] autorelease];
//      cell.accessoryType = UITableViewCellAccessoryDisclosureIndicator;
        
        instrumentLabel = [[[UILabel alloc] initWithFrame:CGRectMake(iPad ? 30.0 : 15.0, 10.0, 90.0, 25.0)] autorelease];
        instrumentLabel.tag = 1;
        instrumentLabel.font = [UIFont systemFontOfSize:16.0];
        instrumentLabel.textAlignment = NSTextAlignmentLeft;
        instrumentLabel.textColor = [UIColor blackColor];
        instrumentLabel.autoresizingMask = /*UIViewAutoresizingFlexibleLeftMargin |*/   UIViewAutoresizingFlexibleHeight
        ;//         | UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleRightMargin;
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

    TableItem *item;
    id<IO2GOfferRow> row;
    
    @synchronized(listData)
    {
        if (indexPath.row >= [listData count])
            return nil;
        item = [listData objectAtIndex:indexPath.row];
        if (item == nil)
            return nil;
        row = [item->row retain];
    }

    {
        NSString *formatStr = [NSString stringWithFormat: @"%%.%df", [row getDigits]];
        instrumentLabel.text = [row getInstrument];
        bidLabel.text = [NSString stringWithFormat: formatStr, [row getBid]];
        askLabel.text = [NSString stringWithFormat: formatStr, [row getAsk]];
        if (iPad)
        {
        lowLabel.text = [NSString stringWithFormat: formatStr, [row getLow]];
            highLabel.text = [NSString stringWithFormat: formatStr, [row getHigh]];
        }
        
        

        if (item->bidDirection == 0)
            bidLabel.textColor = [UIColor blackColor];
        else
        {
            if (item->isChanged)
                [item performSelector:@selector(dropBidDirection) withObject:nil afterDelay:(NSTimeInterval)1];
            if (item->bidDirection < 0)
                bidLabel.textColor = [UIColor redColor];
            else
                bidLabel.textColor = [UIColor blueColor];
        }

        if (item->askDirection == 0)
            askLabel.textColor = [UIColor blackColor];
        else
        {
            if (item->isChanged)
                [item performSelector:@selector(dropAskDirection) withObject:nil afterDelay:(NSTimeInterval)1];
            if (item->askDirection < 0)
                askLabel.textColor = [UIColor redColor];
            else
                askLabel.textColor = [UIColor blueColor];
        }
        
        item->isChanged = NO;
    }
    [row release];

    return cell;
}



// IO2GTableListener methods

- (void)onAdded:(NSString*)rowID :(id<IO2GRow>)rowData
{
}

- (void)onChanged:(NSString*)rowID :(id<IO2GRow>)rowData
{
    @synchronized(listData)
    {
        id<IO2GOfferRow> newRow = (id<IO2GOfferRow>)rowData;
        for (TableItem *item in listData)
        {
            id<IO2GOfferRow> row = item->row;
            if ([[row getOfferID] isEqualToString: rowID])
            {
                BOOL isChanged = NO;
                double oldBid = [row getBid], newBid = [newRow getBid];
                if (newBid != oldBid)
                {
                    item->bidDirection = (newBid > oldBid) ? 1 : -1;
                    isChanged = YES;
                }
                double oldAsk = [row getAsk], newAsk = [newRow getAsk];
                if (newAsk > oldAsk || newAsk < oldAsk)
                {
                    item->askDirection = (newAsk > oldAsk) ? 1 : -1;
                    isChanged = YES;
                }

                BOOL iPad = NO;
                #ifdef UI_USER_INTERFACE_IDIOM
                    iPad = (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad);
                #endif
            
                if (iPad && ([row getLow] != [newRow getLow] || [row getHigh] != [newRow getHigh]))
                    isChanged = YES;
                  
                [row release];
                [newRow retain];
                item->row = newRow;
                if (isChanged)
                {
                    item->isChanged = YES;
                    [self myReloadData];
                }

                break;
            }
        }
    }
}

- (void)onDeleted:(NSString*)rowID :(id<IO2GRow>)rowData
{
}

- (void)onStatusChanged:(O2GTableStatus)status
{
}

- (oneway void)release
{
    [super release];
}

- (id)retain
{
    return [super retain];
}

- (id)autorelease
{
    return [super autorelease];
}
@end


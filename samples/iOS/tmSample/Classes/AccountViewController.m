
#import "AccountViewController.h"
#import "TablesController.h"

@implementation AccountTableItem

- (id)init:(id)newRow
{
    self = [super init];
    row = [newRow retain];
    isChanged = YES;
    return self;
}

- (void)dealloc
{
    [row release];
    [super dealloc];
}

@end


@implementation AccountViewController



- (void)viewDidLoad
{

    [super viewDidLoad];

    // subscribe to wait connected and table refreshed state
    CTablesController *tc = [CTablesController getInstance];
    [tc subscribe:self];
    if ([tc isTablesLoaded])
    {
        [self initTable];
        self.title = @"Accounts";
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
        listData = [[NSMutableArray alloc] initWithCapacity:10];
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
        NSLog(@"Accounts Table initial filling");
#endif
        self.title = @"Accounts";
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
    [self.view performSelectorOnMainThread:@selector(reloadData) withObject: nil waitUntilDone: NO];
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
        mTable = [[CTablesController getInstance] getAccountsTable];
        [listData removeAllObjects];
        int size;
    
        if (mTable && (size = [mTable size]) > 0)
        {
            for (int i = 0 ; i < size ; ++i)
            {
                AccountTableItem* item = [[AccountTableItem alloc] init:[[mTable getRow:i] autorelease]];
                [listData addObject: item];
                [item release];
            }
        }
    
        // sort by account name
        [listData sortUsingComparator: ^(id obj1, id obj2) 
         {
             return [[((AccountTableItem*)obj1)->row getAccountName] localizedCaseInsensitiveCompare:[((AccountTableItem*)obj2)->row getAccountName]];
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

- (CGFloat)tableView: (UITableView *)tableView heightForHeaderInSection:(NSInteger)section {
    return 40.0;
}


- (UIView *)tableView:(UITableView *)tableView viewForHeaderInSection:(NSInteger)section {
    if (headerLabel == nil)
    {
        headerLabel = [[UILabel alloc] initWithFrame:CGRectMake(30.0, 10.0, 600.0, 25.0)] ;
        headerLabel.tag = 11;
        headerLabel.font = [UIFont boldSystemFontOfSize:18.0];
        headerLabel.textAlignment = NSTextAlignmentLeft;
        headerLabel.textColor = [UIColor blackColor];
        headerLabel.backgroundColor = [UIColor lightGrayColor];
        headerLabel.autoresizingMask = UIViewAutoresizingFlexibleHeight;
        headerLabel.text = @"    Account          Usable Margin";
    }
    return headerLabel;
}

// Customize the appearance of table view cells.
- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath {
    
    static NSString *CellIdentifier = @"Cell";
    UILabel *accountLabel, *usedMarginLabel;
    
    UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:CellIdentifier];
    if (cell == nil)
    {
        cell = [[[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:CellIdentifier] autorelease];
//      cell.accessoryType = UITableViewCellAccessoryDisclosureIndicator;
        
        accountLabel = [[[UILabel alloc] initWithFrame:CGRectMake(15.0, 10.0, 90.0, 25.0)] autorelease];
        accountLabel.tag = 1;
        accountLabel.font = [UIFont systemFontOfSize:16.0];
        accountLabel.textAlignment = NSTextAlignmentLeft;
        accountLabel.textColor = [UIColor blackColor];
        accountLabel.autoresizingMask = /*UIViewAutoresizingFlexibleLeftMargin |*/   UIViewAutoresizingFlexibleHeight
        ;//         | UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleRightMargin;
        [cell.contentView addSubview:accountLabel];
    
        usedMarginLabel = [[[UILabel alloc] initWithFrame:CGRectMake(100.0, 10.0, 150.0, 25.0)] autorelease];
        usedMarginLabel.tag = 2;
        usedMarginLabel.font = [UIFont systemFontOfSize:18.0];
        usedMarginLabel.textAlignment = NSTextAlignmentRight;
        usedMarginLabel.textColor = [UIColor blackColor];
        usedMarginLabel.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleHeight
                    | UIViewAutoresizingFlexibleWidth| UIViewAutoresizingFlexibleRightMargin;
        [cell.contentView addSubview:usedMarginLabel];
            
    }
    else
    {
        accountLabel = (UILabel *)[cell.contentView viewWithTag:1];
        usedMarginLabel = (UILabel *)[cell.contentView viewWithTag:2];
    }

    
// Configure the cell.

    AccountTableItem *item;
    id<IO2GAccountTableRow> row;
    
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
        NSString *formatStr = [NSString stringWithFormat: @"%%.%df", 2];
        accountLabel.text = [row getAccountName];
        usedMarginLabel.text = [NSString stringWithFormat: formatStr, [row getUsableMargin]];
        usedMarginLabel.textColor = [UIColor blackColor];

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
        id<IO2GAccountTableRow> newRow = (id<IO2GAccountTableRow>)rowData;
        for (AccountTableItem *item in listData)
        {
            id<IO2GAccountTableRow> row = item->row;
            if ([[row getAccountID] isEqualToString: rowID])
            {   
                BOOL isChanged = NO;
                double oldUsedMargin = [row getUsableMargin], newUsedMargin = [newRow getUsableMargin];
                if (newUsedMargin != oldUsedMargin)
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



#import "ITableSubscriber.h"

@interface AccountTableItem : NSObject
{
 @public
    id<IO2GAccountTableRow> row;
    BOOL isChanged;
}
- (id)init:(id)newRow;
@end

@interface AccountViewController : UITableViewController<IO2GTableListener, IMyTableSubscriber>
{
    UILabel * headerLabel;
    NSMutableArray *listData;
    id<IO2GAccountsTable> mTable;
}

- (void)initTable;
- (void)myReloadData;

@end


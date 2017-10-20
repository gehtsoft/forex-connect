
#import "ITableSubscriber.h"

@interface TableItem : NSObject
{
 @public
    id<IO2GOfferRow> row;
    int bidDirection;
    int askDirection;
    BOOL isChanged;
}
- (id)init:(id)newRow;
- (void)dropBidDirection;
- (void)dropAskDirection;
@end

@interface OfferViewController : UITableViewController<IO2GTableListener, IMyTableSubscriber>
{
    UILabel* headerLabel;
    NSMutableArray *listData;
    id<IO2GOffersTable> mTable;
}

- (void)initTable;
- (void)myReloadData;

@end


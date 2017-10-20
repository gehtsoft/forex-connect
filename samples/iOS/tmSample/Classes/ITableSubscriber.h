#import "ForexConnect/ForexConnect.h"

@protocol IMyTableSubscriber<IAddRef>
- (void)onTableStatusChanged:(NSString *)statusText :(BOOL)isTablesLoaded :(BOOL)isResetTable;
@end

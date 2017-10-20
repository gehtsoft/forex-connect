
#import "TablesController.h"


@interface CTablesController ()

@property (nonatomic, retain) NSString *mSessionID;
@property (nonatomic, retain) NSString *mPin;
@property (nonatomic, retain) NSString *msFirstAccountID;

@end


@implementation CTablesController

static CTablesController *gCTablesController_instance = nil;

static void CTablesController_remover()
{
    [gCTablesController_instance release];
}

+ (CTablesController*)getInstance
{
    @synchronized(self)
    {
        if (gCTablesController_instance == nil)
           [[self alloc] init];
    }
    return gCTablesController_instance;
}

- (id)init
{
    self = [super init];
    if (self)
    {
      gCTablesController_instance = self;
      atexit(CTablesController_remover);

      self.mSessionID = @"";
      self.mPin = @"";
      self.msFirstAccountID = nil;
      self.mStatusText = nil;
      mIsTablesLoaded = NO;
      mIsResetTables = NO;
      mIsLoggedIn = NO;
      listeners = [[NSMutableArray alloc] initWithCapacity:2];
      mSession = [O2GTransport createSession];
      [mSession useTableManager: Yes : self];
      [mSession subscribeSessionStatus: self];
    }
    return self;
}


- (void)dealloc
{
    @synchronized(self)
    {
      [mSession unsubscribeSessionStatus: self];
      [mSession release];
    }

    [self.mSessionID release];
    [self.mPin release];
    [self.msFirstAccountID release];
    [self.mStatusText release];
    [listeners release];

    [super dealloc];
}


- (void)login:(NSString*)user :(NSString*)pwd :(NSString*)url :(NSString*)connection;
{
#ifdef DEBUG_OUT
    NSLog(@"Connect to: %@ * %@ %@\n", user, url, connection);
#endif
    [mSession login: user :pwd :url :connection];
}

- (void)logout
{
    [mSession logout];
}

- (void)setTradingSessionDescriptors:(NSString*)sessionID :(NSString*)pin
{                                            
    [self.mSessionID release];
    [self.mPin release];

    self.mSessionID = [sessionID retain];
    self.mPin = [pin retain];
}

- (void)setProxy:(NSString*)pname :(int)port :(NSString*)user :(NSString*)password
{
#ifdef DEBUG_OUT
    if (pname && [pname length] != 0)
      NSLog(@"Proxy set: %@:%d %@ *\n", pname, port, user);
#endif
    [O2GTransport setProxy :pname :port :user :password];
}

- (void)setCAInfo: (NSString*) caFilePath
{
    [self logString: caFilePath];
    [O2GTransport setCAInfo :caFilePath];
}

- (void)notify
{
    // Subscriber call
    @synchronized(listeners)
    {
        for (id<IMyTableSubscriber> listener in listeners)
            [listener onTableStatusChanged: self.mStatusText :mIsTablesLoaded :mIsResetTables];
    }
}

- (void)subscribe:(id) listener
{
    @synchronized(listeners)
    {
        if ([listeners indexOfObject:listener] == NSNotFound)
            [listeners addObject: listener];
    }
}

- (void)onSessionStatusChanged: (IO2GSessionStatus_O2GSessionStatus) status
{
    switch(status)
    {
     case IO2GSession_Disconnected:
        self.mStatusText = @"Disconnected";
        [self logString: @"Status: Disconnected"];
        mIsLoggedIn = NO;
        mIsTablesLoaded = NO;
        // table manager reset after disconnect - re-subscribe him
        [mSession useTableManager: Yes : self];
        mIsResetTables = YES;
        self.mSessionID = @"";
        break;
     case IO2GSession_Disconnecting:
        self.mStatusText = @"Disconnecting...";
        [self logString: @"Status: Disconnecting"];
        mIsTablesLoaded = NO;
        break;
     case IO2GSession_Connecting:
        self.mStatusText = @"Connecting...";
        [self logString: @"Status: Connecting"];
        mIsTablesLoaded = NO;
        break;

     case IO2GSession_TradingSessionRequested:
        self.mStatusText = @"TradingSessionRequested...";
        [self logString:@"Status: TradingSessionRequested"];
        mIsTablesLoaded = NO;
        NSLog(@"SessionID before: %@", self.mSessionID);
        if ([self.mSessionID length] == 0)
        {
            id<IO2GSessionDescriptorCollection> descriptors = [mSession getTradingSessionDescriptors];
            if ([descriptors size] > 0)
                self.mSessionID = [[descriptors get:0] getID];
            [descriptors release];
        }
            NSLog(@"SessionID after: %@", self.mSessionID);
        [mSession setTradingSession:self.mSessionID pin:self.mPin];
        break;

     case IO2GSession_Connected:

        self.mStatusText = @"Connected";
        [self logString:@"Status: Connected"];
        mIsLoggedIn = YES;
        id<IO2GTableManager> manager = [[mSession getTableManager] autorelease];
        O2GTableManagerStatus tablesStatus = [manager getStatus];
        
        mIsTablesLoaded = ( tablesStatus == TablesLoaded);

      break;
     case IO2GSession_Reconnecting:
      self.mStatusText = @"Reconnecting...";
      [self logString:@"Status: Reconnecting"];
      mIsTablesLoaded = NO;
      break;
     case IO2GSession_SessionLost:
      self.mStatusText = @"Session lost...";
      [self logString:@"Status: Session Lost"];
      mIsTablesLoaded = NO;
      //[self login];
      break;
     default:
      break;  
    }
    [self notify];
    mIsResetTables = NO;
}

- (void)onStatusChanged:(O2GTableManagerStatus)status :(id<IO2GTableManager>)tableManager
{
    switch (status)
    {
     case TablesLoading:
            self.mStatusText = @"Table loading";
            [self logString:@"Status: Tables loading"];
            mIsTablesLoaded = NO;
            break;
        case TablesLoadFailed:
            self.mStatusText = @"Load failed";
            [self logString:@"Status: Tables load failed"];
            mIsTablesLoaded = NO;
            break;
        case TablesLoaded:
            self.mStatusText = @"Tables loaded";
            [self logString:@"Status: Tables loaded"];
            mIsTablesLoaded = YES;
            break;
    }
    // Subscriber call
    [self notify];
}

- (void)onLoginFailed:(NSString*)error
{
    [self logString:error];
    self.mStatusText = @"Login failed";
    [self logString:@"Status: Login failed"];
    [self notify];
}

- (void)logString:(NSString*)logmessage
{
#ifdef DEBUG_OUT
    NSLog(@"%@\n", logmessage);
#endif
}

- (void)unsubscribe:(id)listener
{
    @synchronized(listeners)
    {
        [listeners removeObject:listener];
    }
}

- (id<IO2GSession>)getSession
{
    return mSession;
}

- (BOOL)isTablesLoaded
{
    return mIsTablesLoaded;
}

- (BOOL)isLoggedIn
{
    return mIsLoggedIn;
}

- (NSString *)getStatusText
{
    return self.mStatusText;
}

- (id<IO2GOffersTable>)getOffersTable
{
    return (id<IO2GOffersTable>)[[[mSession getTableManager] autorelease] getTable:Offers];
}

- (id<IO2GAccountsTable>)getAccountsTable
{
    return (id<IO2GAccountsTable>)[[[mSession getTableManager] autorelease] getTable:Accounts];
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
#pragma once
#define DEBUG_OUT

#import "ForexConnect/ForexConnect.h"
#import "ITableSubscriber.h"

@interface CTablesController : NSObject<IO2GSessionStatus, IO2GTableManagerListener>
{
    NSObject<IO2GSession> *mSession;
    IO2GSessionStatus_O2GSessionStatus mCurrentStatus;
    BOOL mIsTablesLoaded, mIsResetTables, mIsLoggedIn;
    NSMutableArray *listeners;
}

@property (nonatomic, retain) NSString *mStatusText;

    + (CTablesController*) getInstance;

    - (void)login:(NSString*)user :(NSString*)pwd :(NSString*)url :(NSString*)connection;
    - (void)logout;
    - (void)setTradingSessionDescriptors:(NSString*)sessionID :(NSString*)pin;
    - (void)setProxy:(NSString*)pname :(int)port :(NSString*)user :(NSString*)password;
    - (void)setCAInfo:(NSString*)caFilePath;

    - (void)subscribe:(id)pListener;
    - (void)unsubscribe:(id)pListener;

    /** Log string to console.*/        
    - (void)logString:(NSString*)logmessage;

    - (void)onSessionStatusChanged:(IO2GSessionStatus_O2GSessionStatus)status;
    - (void)onLoginFailed:(NSString*)error;

    - (void)notify;

    - (id<IO2GSession>)getSession;
    - (BOOL)isLoggedIn;
    - (BOOL)isTablesLoaded;
    - (NSString *)getStatusText;

    - (id<IO2GOffersTable>)getOffersTable;
    - (id<IO2GAccountsTable>)getAccountsTable;

@end
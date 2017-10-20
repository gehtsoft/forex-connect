
#pragma once
#include <vector>
#include <string>
#include <iostream>
#include "ForexConnect/ForexConnect.h"

#ifndef id
typedef struct objc_object *id;
#endif

typedef struct objc_selector    *SEL;
typedef void (*objc_call)(id, SEL, signed char);

typedef struct
{
    id target;
    SEL selector;
    objc_call call;
    signed char param;
} idWrapper;

class COffersController : private IO2GSessionStatus, IO2GResponseListener
{
 public:

    static COffersController* getInstance();

    double getLow(size_t index) const;
    double getHigh(size_t index) const;
    double getBid(size_t index) const;
    double getAsk(size_t index) const;
    const char * getInstrumentText(size_t index);
    int getBidDirection(size_t index) const;
    int getAskDirection(size_t index) const;
    int getDigits(size_t index) const;
    const char * getStatusText();

    bool isCellChanged(size_t index);

    void setLoginData(const char *user, const char *pwd, const char *url, const char *connection);
    void login();
    void logout();
    void setTradingSessionDescriptors(const char *sessionID, const char *pin);
    void setProxy(const char *pname, int port, const char *user, const char *password);
    void setCAInfo(const char *caFilePath);


    size_t size() const
        { return mRows.size(); }

    void setData(IO2GOffersTableResponseReader *pOffersReader);

    void subscribe(void *pListener);

    void unsubscribe()
        { mIdWrapper = NULL; }

    void createOrder(int offerIndex, bool isBuy, int nAmount, double fRate, int orderType);

    // Thread safe implementation.
    long addRef();
    long release();

protected:

    COffersController();
    ~COffersController();


    void onOffersRecieved(IO2GResponse *response);
    void onTableUpdateReceive(IO2GResponse *response);
    void onAccountsReceived(IO2GResponse *response);

    void onRequestCompleted(const char * requestId, IO2GResponse  *response = 0)
    {}
    void onRequestFailed(const char *requestId , const char *error)
    {}
    void onTablesUpdates(IO2GResponse *data);

    /** Log string to console.*/
    void logString(const char *logmessage);

    void onSessionStatusChanged(O2GSessionStatus status);
    void onLoginFailed(const char *error);

    void notify();

    static unsigned long getTickCount();

    struct COfferRow
    {
        double fBid;
        double fAsk;
        double fLow;
        double fHigh;
        std::string sOfferId;
        std::string sInstrument;
        bool isChanged;
        int nBidDirection;
        int nAskDirection;
        int nDigits;
        unsigned int dropDirectionCount;
    };

    static COffersController* inst;
    std::vector<COfferRow*> mRows;
    pthread_mutex_t mRowsLock;
    IO2GSession *mSession;

    std::string mUserName;
    std::string mPassword;
    std::string mConnection;
    std::string mHost;
    std::string mSessionID;
    std::string mPin;

    std::string msFirstAccountID;
    std::string mStatusText;

    idWrapper *mIdWrapper;
    volatile long dwRef;
};


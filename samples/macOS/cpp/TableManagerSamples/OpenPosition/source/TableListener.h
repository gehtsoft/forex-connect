#pragma once

class OrderMonitor;
class TableListener :
    public IO2GTableListener
{
public:
    TableListener(ResponseListener *responseListener);

    /** Increase reference counter. */
    virtual long addRef();

    /** Decrease reference counter. */
    virtual long release();

    /** Set request ID. */
    void setRequestID(const char *sRequestID);

    void onStatusChanged(O2GTableStatus);
    void onAdded(const char *, IO2GRow *);
    void onChanged(const char *, IO2GRow *);
    void onDeleted(const char *, IO2GRow *);

    void subscribeEvents(IO2GTableManager *manager);
    void unsubscribeEvents(IO2GTableManager *manager);

private:
    long mRefCount;
    /** Response listener. */
    ResponseListener *mResponseListener;
    /** Request we are waiting for. */
    std::string mRequestID;
    OrderMonitor *mOrderMonitor;

    void printResult();
    void printTrades(std::vector<IO2GTradeRow*> &trades, std::string &orderID);
    void printClosedTrades(std::vector<IO2GClosedTradeRow*> &trades, std::string &orderID);

 protected:
    /** Destructor. */
    virtual ~TableListener();
};


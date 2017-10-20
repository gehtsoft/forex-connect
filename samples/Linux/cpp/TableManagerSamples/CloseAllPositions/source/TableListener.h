#pragma once

class BatchOrderMonitor;
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
    void setRequestIDs(std::vector<std::string> &requestIDs);

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
    std::vector<std::string> mRequestIDs;
    BatchOrderMonitor *mBatchOrderMonitor;

    void printResult();
    void printOrderMonitor(OrderMonitor *monitor);
    void printTrades(std::vector<IO2GTradeRow*> &trades, std::string &orderID);
    void printClosedTrades(std::vector<IO2GClosedTradeRow*> &trades, std::string &orderID);

 protected:
    /** Destructor. */
    virtual ~TableListener();
};


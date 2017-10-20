#pragma once

/** Response listener class. */

class OrderMonitorNetting;

class ResponseListener : public IO2GResponseListener
{
 public:
    ResponseListener(IO2GSession *session);

    /** Increase reference counter. */
    virtual long addRef();

    /** Decrease reference counter. */
    virtual long release();

    /** Set request ID. */
    void setRequestID(const char *sRequestID);

    /** Store initial trades state. */
    void setTradesTable(IO2GTradesTableResponseReader *tradesTable);

    /** Wait for request execution or error. */
    bool waitEvents();

    /** Get response.*/
    IO2GResponse *getResponse();

    /** Request execution completed data handler. */
    virtual void onRequestCompleted(const char *requestId, IO2GResponse *response = 0);

    /** Request execution failed data handler. */
    virtual void onRequestFailed(const char *requestId , const char *error);

    /** Request update data received data handler. */
    virtual void onTablesUpdates(IO2GResponse *data);

 private:
    long mRefCount;
    /** Session object. */
    IO2GSession *mSession;
    /** Request we are waiting for. */
    std::string mRequestID;
    /** Response Event handle. */
    HANDLE mResponseEvent;
    void printResult();
    void printTrades(std::vector<IO2GTradeRow*> &trades, std::string &sOrderID);
    void printUpdatedTrades(std::vector<IO2GTradeRow*> &updatedTrades, std::string &sOrderID);
    void printClosedTrades(std::vector<IO2GClosedTradeRow*> &trades, std::string &sOrderID);
    OrderMonitorNetting *mOrderMonitorNetting;

    /** State of last request. */
    IO2GResponse *mResponse;
    
    IO2GTradesTableResponseReader *mTradesTable;

 protected:
    /** Destructor. */
    virtual ~ResponseListener();

};


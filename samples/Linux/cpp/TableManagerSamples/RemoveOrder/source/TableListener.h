#pragma once

bool isLimitEntryOrder(IO2GOrderRow *order);

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

    std::string getOrderID();

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

    std::string mOrderID;

 protected:
    /** Destructor. */
    virtual ~TableListener();
};


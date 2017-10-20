#pragma once

/** Response listener class. */

class Offer;
class OfferCollection;

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

    void setInstrument(const char *sInstrument);

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

    void printOffers(IO2GSession *session, IO2GResponse *response, const char *sInstrument);
    void printLevel2MarketData(IO2GSession *session, IO2GResponse *response, const char *sInstrument);

 private:
    long mRefCount;
    /** Session object. */
    IO2GSession *mSession;
    /** Request we are waiting for. */
    std::string mRequestID;
    std::string mInstrument;
    /** Response Event handle. */
    HANDLE mResponseEvent;

    /** State of last request. */
    IO2GResponse *mResponse;

    OfferCollection *mOffers;

 protected:
    /** Destructor. */
    virtual ~ResponseListener();

};


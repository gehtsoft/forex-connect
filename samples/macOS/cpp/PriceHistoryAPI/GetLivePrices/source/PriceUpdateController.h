/* Copyright 2019 FXCM Global Services, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use these files except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
#pragma once
#include "ThreadSafeAddRefImpl.h"

/** Forward declaration. */
class IOffer;
class Offer;

/** Interface of a listener to update prices.
    Used by IPriceUpdateController.
 */
class IPriceUpdateListener
{
 public:
    virtual void onCollectionUpdate(IOffer *offer) = 0;
};

///////////////////////////////////////////////////////////////////////////////

/** Interface of a price update controller which notifies its listeners
    when a new price (tick) of some instrument is received. 
    It also contains some helper time conversion methods.
 */
class IPriceUpdateController
{
 public:
    /** Converts NYT to UTC */
    virtual DATE estToUtc(DATE time) = 0;

    /** Converts UTC to NYT */
    virtual DATE utcToEst(DATE time) = 0;

    /** Add listener. */
    virtual bool addListener(IPriceUpdateListener *listener) = 0;

    /** Remove listener. */
    virtual bool removeListener(IPriceUpdateListener *listener) = 0;

    /** Gets the trading day offset */
    virtual int getTradingDayOffset() = 0;

    /** Waits until the Offers table is loaded. */
    virtual bool wait() = 0;
};

///////////////////////////////////////////////////////////////////////////////

/** The class listens for ticks notifications which are sent by ForexConnect 
    and notifies its listeners.
 */
class PriceUpdateController : public IPriceUpdateController
{
 public:
    /** Constructor. */
    PriceUpdateController(IO2GSession *session, const char *instrument);
    ~PriceUpdateController();

    /** Unsubscribes the object from the session events. */
    void unsubscribe();

 public:
    /** @name IPriceUpdateController interface implementation */
    //@{
    virtual bool wait();
    virtual bool addListener(IPriceUpdateListener *callback);
    virtual bool removeListener(IPriceUpdateListener *callback);
    virtual DATE estToUtc(DATE time);
    virtual DATE utcToEst(DATE time);
    virtual int getTradingDayOffset();
    //@}

 private:
    void getLatestOffer();
    void notifyPriceUpdate(IOffer *offer);
    bool splitTime(const std::string &stringToParse, int &hour, int &minutes, int &seconds);

    // The following methods are called by ResponseListener when the appropriate
    // IO2GResponseListener method is called
    void onRequestCompleted(const char *requestId, IO2GResponse *response);
    void onTablesUpdates(IO2GResponse *data);

 private:
    /** The mutex. */
    hptools::Mutex mMutex;
    /** The ForexConnect trading session */
    IO2GSession *mSession;
    /** The instrument */
    std::string mInstrument;
    /** The latest offer of the instrument available for the trading session */
    O2G2Ptr<Offer> mOffer;
    /** The event used to wait for Offers loading state. */
    HANDLE mSyncOfferEvent;
    /** Time zone converter */
    O2G2Ptr<IO2GTimeConverter> mTZConverter;
    /** The offet in hours of when the trading day start in relation to the midnight */
    int mTradingDayOffset;
    /** The flag that indicates that we failed to find an Offer. */
    bool mInitFailed;

    /** Listeners. */
    typedef std::list<IPriceUpdateListener*> Listeners;
    Listeners mListeners;

 private:
    /** The listener of ForexConnect responses. It simply redirects the event to the parent
        controller.
     */
    class ResponseListener : public TThreadSafeAddRefImpl<IO2GResponseListener>
    {
        hptools::Mutex mMutex;
        PriceUpdateController *mPriceUpdateController;

     public:
        ResponseListener(PriceUpdateController *priceUpdateController);
        void detach();

     protected:
        /** @name IO2GResponseListener interface implementation */
        //@{
        virtual void onRequestCompleted(const char *requestId, IO2GResponse *response);
        virtual void onRequestFailed(const char *requestId, const char *error);
        virtual void onTablesUpdates(IO2GResponse *data);
        //@}
    };
    O2G2Ptr<ResponseListener> mResponseListener;
};

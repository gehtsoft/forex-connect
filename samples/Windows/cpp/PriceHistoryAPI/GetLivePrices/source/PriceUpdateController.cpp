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
#include "stdafx.h"

#include "PriceUpdateController.h"
#include "PriceData/Offer.h"

/** Constructs a listener for a specified PriceUpdateController.
 */
PriceUpdateController::ResponseListener::ResponseListener(PriceUpdateController *priceUpdateController) :
    mPriceUpdateController(priceUpdateController)
{
}

/** It must be called when the parent PriceUpdateController is about to be destroyed. */
void PriceUpdateController::ResponseListener::detach()
{
    // if we are calling the parent controller right now - wait 
    hptools::Mutex::Lock lock(mMutex);
    mPriceUpdateController = 0;
}

void PriceUpdateController::ResponseListener::onRequestCompleted(const char *requestId, IO2GResponse *response)
{
    hptools::Mutex::Lock lock(mMutex);
    if (mPriceUpdateController)
        mPriceUpdateController->onRequestCompleted(requestId, response);
}

void PriceUpdateController::ResponseListener::onRequestFailed(const char *requestId, const char *error)
{

}

void PriceUpdateController::ResponseListener::onTablesUpdates(IO2GResponse *data)
{
    hptools::Mutex::Lock lock(mMutex);
    if (mPriceUpdateController)
        mPriceUpdateController->onTablesUpdates(data);
}

///////////////////////////////////////////////////////////////////////////////

PriceUpdateController::PriceUpdateController(IO2GSession *session, const char *instrument)
{
    mSyncOfferEvent = CreateEvent(0, FALSE, FALSE, 0);

    mInstrument = instrument;
    mSession = session;
    mInitFailed = false;

    mResponseListener = new ResponseListener(this);
    mSession->subscribeResponse(mResponseListener);

    mTZConverter = mSession->getTimeConverter();

    // get the trading day offset
    O2G2Ptr<IO2GLoginRules> loginRules(session->getLoginRules());
    O2G2Ptr<IO2GResponse> response(loginRules->getSystemPropertiesResponse());
    O2G2Ptr<IO2GResponseReaderFactory> readerFac(session->getResponseReaderFactory());
    O2G2Ptr<IO2GSystemPropertiesReader> reader(readerFac->createSystemPropertiesReader(response));

    // set up trading day offset
    int defaultTradingDayOffset = -7; // it corresponds to default 17:00 EST
    const char *endTradingDayUTC = reader->findProperty("END_TRADING_DAY");
    if (endTradingDayUTC)
    {
        std::string eod(endTradingDayUTC);

        int hour = 0;
        int minutes = 0;
        int seconds = 0;

        // parse (split) time
        if ((splitTime(endTradingDayUTC, hour, minutes, seconds))
            && (0 == minutes) && (0 == seconds))
        {
            // convert Trading day start to EST time because the trading day is always closed by New York time
            // so to avoid handling different hour depending on daylight saying time - use EST always
            // for candle calculations
            DATE date = hptools::date::DateNow();
            std::tm t;
            hptools::date::OleTimeToCTime(date, &t);
            t.tm_hour = hour;

            hptools::date::CTimeToOleTime(&t, &date);
            DATE dateInEST = mTZConverter->convert(date, IO2GTimeConverter::UTC, IO2GTimeConverter::EST);
            hptools::date::OleTimeToCTime(dateInEST, &t);

            // here we have the date when trading day begins, e.g. 17:00:00
            // please note that if trading day begins before noon - it begins AFTER calendar date is started,
            // so the offset is positive (e.g. 03:00 is +3 offset).
            // if trading day begins after noon, it begins BEFORE calendar date is istarted,
            // so the offset is negative (e.g. 17:00 is -7 offset).
            if (t.tm_hour <= 12)
                mTradingDayOffset = t.tm_hour;
            else
                mTradingDayOffset = t.tm_hour - 24;
        }
    }
    else
    {
        // set default
        mTradingDayOffset = defaultTradingDayOffset;
    }

    // get latest offer for the instrument
    getLatestOffer();
}

PriceUpdateController::~PriceUpdateController()
{
    unsubscribe();
    mResponseListener->detach();

    CloseHandle(mSyncOfferEvent);
}

bool PriceUpdateController::wait()
{
    if (mInitFailed)
        return false;

    bool eventOccurred = (WaitForSingleObject(mSyncOfferEvent, _TIMEOUT) == WAIT_OBJECT_0);
    if (!eventOccurred)
        std::cout << "Timeout occurred during waiting for loading of Offers table" << std::endl;

    if (mInitFailed)
        return false;

    return eventOccurred;
}

void PriceUpdateController::unsubscribe()
{
    mSession->unsubscribeResponse(mResponseListener);
}

void PriceUpdateController::onRequestCompleted(const char *requestId, IO2GResponse *response)
{
    // we need only offer table refresh for our example
    if (GetOffers == response->getType())
    {
        // so simply read and store offers
        O2G2Ptr<IO2GResponseReaderFactory> readerFac(mSession->getResponseReaderFactory());
        O2G2Ptr<IO2GOffersTableResponseReader> reader(readerFac->createOffersTableReader(response));
        for (int i = 0; i < reader->size(); ++i)
        {
            O2G2Ptr<IO2GOfferRow> row(reader->getRow(i));
            if (std::string(row->getInstrument()) == mInstrument)
            {
                mOffer = new Offer(row->getInstrument(), row->getTime(), row->getBid(), row->getAsk(), row->getVolume(), row->getDigits());
                SetEvent(mSyncOfferEvent);
                break;
            }
        }
        if (!mOffer)
        {
            std::cout << "Instrument is not found" << std::endl;
            mInitFailed = true;
            SetEvent(mSyncOfferEvent);
        }
    }
}

bool PriceUpdateController::addListener(IPriceUpdateListener *callback)
{
    if (callback == NULL)
        return false;

    hptools::Mutex::Lock lock(mMutex);
    for (Listeners::iterator it = mListeners.begin(); it != mListeners.end(); ++it)
    {
        if ((*it) == callback)
            return false;
    }

    mListeners.push_back(callback);

    return true;
}

bool PriceUpdateController::removeListener(IPriceUpdateListener *callback)
{
    if (callback == NULL)
        return false ;

    hptools::Mutex::Lock lock(mMutex);
    for (Listeners::iterator it = mListeners.begin(); it != mListeners.end(); ++it)
    {
        if ((*it) == callback)
        {
            mListeners.erase(it);
            return true;
        }
    }

    return false;
}

void PriceUpdateController::onTablesUpdates(IO2GResponse *data)
{
    if (TablesUpdates == data->getType())
    {
        if (NULL == mOffer)
            return;

        O2G2Ptr<IO2GResponseReaderFactory> readerFac(mSession->getResponseReaderFactory());
        O2G2Ptr<IO2GTablesUpdatesReader> reader(readerFac->createTablesUpdatesReader(data));
        for (int i = 0; i < reader->size(); ++i)
        {
            // We are looking only for updates and only for offers

            // NOTE: in order to support offer subscribtion, the real application also will need 
            // to read add/remove events and change the offer collection correspondingly
            if (Update == reader->getUpdateType(i) &&
                Offers == reader->getUpdateTable(i))
            {
                // read the offer update
                O2G2Ptr<IO2GOfferRow> row(reader->getOfferRow(i));

                if (!row->isInstrumentValid())
                    continue;

                std::string str(row->getInstrument());
                if (str != mInstrument)
                    continue;

                // update latest offer
                if (row->isTimeValid())
                    mOffer->setLastUpdate(row->getTime());
                if (row->isBidValid())
                    mOffer->setBid(row->getBid());
                if (row->isAskValid())
                    mOffer->setAsk(row->getAsk());
                if (row->isVolumeValid())
                    mOffer->setMinuteVolume(row->getVolume());

                // please note that we send a clone of the offer,
                // we need it in order to make sure that no ticks
                // will be lost, if, for example, subscriber doesn't handle the 
                // offer immideatelly 
                O2G2Ptr<IOffer> offer(mOffer->clone());
                notifyPriceUpdate(offer);
            }
        }
    }
}

/** Notifies all subscribed listeners that the offer has changed. */
void PriceUpdateController::notifyPriceUpdate(IOffer *offer)
{
    // NOTE: for simplicity subscription cannot be changed during notification 
    Listeners listeners;
    {
        hptools::Mutex::Lock lock(mMutex);
        listeners = mListeners;
    }

    for (Listeners::iterator it = listeners.begin(); it != listeners.end(); ++it)
        (*it)->onCollectionUpdate(offer);
}

DATE PriceUpdateController::estToUtc(DATE time)
{
    return mTZConverter->convert(time, IO2GTimeConverter::EST, IO2GTimeConverter::UTC);
}

DATE PriceUpdateController::utcToEst(DATE time)
{
    return mTZConverter->convert(time, IO2GTimeConverter::UTC, IO2GTimeConverter::EST);
}

/** Get the latest offer to which the user is subscribed. */
void PriceUpdateController::getLatestOffer()
{
    // get the list of the offers to which the user is subscribed
    O2G2Ptr<IO2GLoginRules> loginRules(mSession->getLoginRules());
    O2G2Ptr<IO2GResponse> response(loginRules->getSystemPropertiesResponse());

    if (loginRules->isTableLoadedByDefault(Offers))
    {
        // if it is already loaded - just handle them
        response = loginRules->getTableRefreshResponse(Offers);
        onRequestCompleted(NULL, response);
    }
    else
    {
        // otherwise create the request to get offers from the server
        O2G2Ptr<IO2GRequestFactory> factory(mSession->getRequestFactory());
        O2G2Ptr<IO2GRequest> offerRequest(factory->createRefreshTableRequest(Offers));
        mSession->sendRequest(offerRequest);
    }
}

int PriceUpdateController::getTradingDayOffset()
{
    return mTradingDayOffset;
}

/** Splits time to hour, minutes and seconds. */
bool PriceUpdateController::splitTime(const std::string &stringToParse, int &hour, int &minutes, int &seconds)
{
    const char delimiter = ':';
    std::locale loc;
    bool correct = (stringToParse.size() == 8
        && std::isdigit(stringToParse[0], loc)
        && std::isdigit(stringToParse[1], loc)
        && stringToParse[2] == delimiter
        && std::isdigit(stringToParse[3], loc)
        && std::isdigit(stringToParse[4], loc)
        && stringToParse[5] == delimiter
        && std::isdigit(stringToParse[6], loc)
        && std::isdigit(stringToParse[7], loc));

    if (!correct)
        return false;

    hour = std::atoi(stringToParse.substr(0, 2).c_str());
    minutes = std::atoi(stringToParse.substr(3, 2).c_str());
    seconds = std::atoi(stringToParse.substr(6, 2).c_str());

    correct = hour < 24 && minutes < 60 && seconds < 60;
    return correct;
}

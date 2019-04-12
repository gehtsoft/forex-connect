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
#include "../stdafx.h"

#include "PeriodCollection.h"
#include "Period.h"

#define HALF_SECOND 0.5 / 86400.0 // 1/2 of the second

PeriodCollection::PeriodCollection(const char *instrument, const char *timeframe, bool alive, IPriceUpdateController *controller) :
    mInstrument(instrument),
    mTimeframe(timeframe),
    mAlive(alive),
    mFilled(false)
{
    if (alive)
    {
        // if collection is alive - we will need to calculate the date/time of the candle
        // to which each tick belongs to, so we need to parse the time frame name for
        // further usage
        if (!quotesmgr::CandlePeriod::parsePeriod(timeframe, mTimeframeUnit, mTimeframeLength))
            throw std::logic_error("Invalid timeframe");

        // and we need to subscribe to tick updates
        mTradingDayOffset = controller->getTradingDayOffset();
        controller->addListener(this);
        mController = controller;
    }
}

PeriodCollection::~PeriodCollection()
{
    mController->removeListener(this);
}

void PeriodCollection::add(DATE time, double bidOpen, double bidHigh, double bidLow, double bidClose,
                           double askOpen, double askHigh, double askLow, double askClose,
                           int volume)
{
    hptools::Mutex::Lock lock(mMutex);
    O2G2Ptr<Period> period(new Period(time, bidOpen, bidHigh, bidLow, bidClose, askOpen, askHigh, askLow, askClose, volume));

    mPeriods.push_back(period);
}

void PeriodCollection::finish(DATE lastMinute, int lastMinuteVolume)
{
    mLastMinute = dateToMinute(lastMinute);
    mLastMinuteVolume = lastMinuteVolume;

    if (mAlive)
    {
        // process all ticks collected while we were waiting for the server response and were handling it.
        // The ticks are collected in OnPriceUpdate method.
        while (mWaitingUpdates.size() > 0)
        {
            O2G2Ptr<IOffer> offer(mWaitingUpdates.front());
            mWaitingUpdates.pop();
            handleOffer(offer);
        }
        mFilled = true;
        notifyLastPeriodUpdated();
    }
}

/** Notifies subscribed listeners that the last period is updated. */
void PeriodCollection::notifyLastPeriodUpdated()
{
    // NOTE: for simplicity subscription cannot be changed during notification 
    Listeners listeners;
    {
        hptools::Mutex::Lock lock(mMutex);
        listeners = mListeners;
    }

    for (Listeners::iterator it = listeners.begin(); it != listeners.end(); ++it)
        (*it)->onCollectionUpdate(this, (int)(mPeriods.size() - 1));
}

/** Called when a price is updated (tick). */
void PeriodCollection::onCollectionUpdate(IOffer *offer)
{
    // handle ticks only for alive collections (e.g. these which were requested
    // from the server with "up to now" parameter) and only for 
    // the instrument of collection
    if (mAlive && offer->getInstrument() == mInstrument)
    {
        if (mFilled)
        {
            // if collection is already filled - handle the tick right now
            handleOffer(offer);
            notifyLastPeriodUpdated();
        }
        else
        {
            // otherwise - keep it and handle later, when collection is filled
            // see Finalize() methods for handling these offers
            offer->addRef();
            mWaitingUpdates.push(offer);
        }
    }
}

/** Handling one tick. */
void PeriodCollection::handleOffer(IOffer *offer)
{
    if (offer == NULL)
        return;

    hptools::Mutex::Lock lock(mMutex);
    
    // calculate the start time of the period to which the tick belong to
    DATE start = -1;
    DATE end = -1 ;
    
    // calculate candle in EST time because the trading day is always closed by New York time
    // so to avoid handling different hour depending on daylight saying time - use EST always
    // for candle calculations

    // NOTE: for real application this part can be optimized. The candle calculation 
    // is quite complex process, so it is better to avoid it when it is not actually required.
    // the way to optimize it is to keep end time of the period and check whether the tick belongs to 
    // the period using the following condition start <= tick < end
    // so the calculation of new candle will be used only when tick is actually >= of the end 
    // of the current candle.

    double time = mController->utcToEst(offer->getLastUpdate());
    quotesmgr::CandlePeriod::getCandle(time, start, end, mTimeframeUnit, mTimeframeLength, mTradingDayOffset, -1);
    start = mController->estToUtc(start);
    // calculate the serial number of minute (for easier comparing) 
    INT64 currMinute = dateToMinute(offer->getLastUpdate());

    if (mPeriods.size() == 0)
    {
        // if here is no data in the collection yet - just add a dummy candle
        O2G2Ptr<Period> period(new Period(start, offer->getBid(), offer->getAsk(), offer->getMinuteVolume()));
        mPeriods.push_back(period);
        mLastMinute = currMinute;
        mLastMinuteVolume = offer->getMinuteVolume();
    }
    else 
    {
        // otherwise get the most recent candle
        O2G2Ptr<Period> period(mPeriods[mPeriods.size() - 1]);
        if (fabs(period->getTime() - start) < HALF_SECOND) // period->getTime() == start
        {
            //if tick belongs to that period...

            //update the latest (close) price of bid and ask bars
            O2G2Ptr<Bar> ask = dynamic_cast<Bar *>(period->getAsk());
            O2G2Ptr<Bar> bid = dynamic_cast<Bar *>(period->getBid());
            ask->setClose(offer->getAsk());
            bid->setClose(offer->getBid());

            //if tick higher than high value of bars - update 
            if (ask->getHigh() < offer->getAsk())
                ask->setHigh(offer->getAsk());
            if (bid->getHigh() < offer->getBid())
                bid->setHigh(offer->getBid());

            //if tick lower than low value of bars - update 
            if (ask->getLow() > offer->getAsk())
                ask->setLow(offer->getAsk());
            if (bid->getLow() > offer->getBid())
                bid->setLow(offer->getBid());

            //here is a trick. 
            //We don't receive EVERY tick, so we can't simply count them. 
            //It is not a problem for calculating open, high, low and close, because 
            //the tick filter keeps every first, last, and the current extremum ticks 
            //In order to make the volume calculation also correct, the server
            //broadcasts the accumulated tick volume for the current minute. 
            
            //So, if the tick belongs to the same minute as the previous tick - 
            //we must substract previous accumulated volume and add a new value.
            //If the tick is the first tick of a new minute - we must simply 
            //add new accumulated value.
            if (mLastMinute == currMinute)
            {
                period->setVolume((period->getVolume() - mLastMinuteVolume) + offer->getMinuteVolume());
            }
            else if (currMinute > mLastMinute)
            {
                period->setVolume(period->getVolume() + offer->getMinuteVolume());
            }

            mLastMinute = currMinute;
            mLastMinuteVolume = offer->getMinuteVolume();
        }
        else if (period->getTime() < start - HALF_SECOND) // period->getTime() < start
        {
            //This is a first tick of new period, simply create this period

            //Please pay attention that we don't use the first tick as an open
            //value but use the previous close instead.
            //This is how the current system works by default.

            //Soon, here should be an option to use the first tick for the open 
            //price instead.
            O2G2Ptr<Bar> bid(dynamic_cast<Bar *>(period->getBid()));
            O2G2Ptr<Bar> ask(dynamic_cast<Bar *>(period->getAsk()));
            
            period = new Period(start, bid->getClose(), ask->getClose(), offer->getMinuteVolume());
            mPeriods.push_back(period);

            ask = dynamic_cast<Bar *>(period->getAsk());
            bid = dynamic_cast<Bar *>(period->getBid());
            ask->setClose(offer->getAsk());
            bid->setClose(offer->getBid());

            //if tick higher than high value of bars - update 
            if (ask->getHigh() < offer->getAsk())
                ask->setHigh(offer->getAsk());
            if (bid->getHigh() < offer->getBid())
                bid->setHigh(offer->getBid());

            //if tick lower than low value of bars - update 
            if (ask->getLow() > offer->getAsk())
                ask->setLow(offer->getAsk());
            if (bid->getLow() > offer->getBid())
                bid->setLow(offer->getBid());

            mLastMinute = currMinute;
            mLastMinuteVolume = offer->getMinuteVolume();
        }
        else
        {
            // yep, it is possible that tick is older than the last candle.
            // it may happen because we start to collect ticks actually BEFORE
            // we sent the request to the server. So on the border of the minute 
            // it is possible that we "catch" some ticks of the previous 
            // minute 

            // so, simply ignore them
            ;
        }
    }
}

int PeriodCollection::size()
{
    return (int)mPeriods.size();
}

const char *PeriodCollection::getInstrument()
{
    return mInstrument.c_str();
}

const char *PeriodCollection::getTimeframe()
{
    return mTimeframe.c_str();
}

bool PeriodCollection::isAlive()
{
    return mAlive;
}

void PeriodCollection::addListener(ICollectionUpdateListener *callback)
{
    if (callback == NULL)
        return;

    hptools::Mutex::Lock lock(mMutex);
    for (Listeners::iterator it = mListeners.begin(); it != mListeners.end(); ++it)
    {
        if ((*it) == callback)
            return;
    }

    mListeners.push_back(callback);
}

void PeriodCollection::removeListener(ICollectionUpdateListener* callback)
{
    if (callback == NULL)
        return;

    hptools::Mutex::Lock lock(mMutex);
    for (Listeners::iterator it = mListeners.begin(); it != mListeners.end(); ++it)
    {
        if ((*it)==callback)
        {
            mListeners.erase(it);
            return;
        }
        ++it;
    }
}

IPeriod *PeriodCollection::getPeriod(int index)
{
    mPeriods[index]->addRef();
    return mPeriods[index];
}

/** Calculates the serial number of a minute. */
INT64 PeriodCollection::dateToMinute(DATE time)
{
    double msec = time * 86400 * 1000;
    return (INT64)(floor(msec + 0.5)) / 60000;
}

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
package getliveprices.pricedata;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.GregorianCalendar;
import java.util.Iterator;
import java.util.List;
import java.util.ArrayList;
import java.util.Queue;
import java.util.TimeZone;
import java.util.concurrent.atomic.AtomicReference;

import com.candleworks.quotesmgr.CandlePeriod;
import com.candleworks.quotesmgr.CandlePeriod.Unit;

import getliveprices.IPriceUpdateController;

/**
 * Implementation of the period collection
 */
public class PeriodCollection implements IPeriodCollection, 
    IPriceUpdateController.IListener {

    /**
     * Storage of the periods
     */
    private List<IPeriod> mPeriods = new ArrayList<IPeriod>();

    /**
     * The listeners collection
     */
    private List<IListener> mListeners = new ArrayList<IListener>();

    /**
     * The name of the instrument
     */
    private String mInstrument;

    /**
     * The name of the time frame
     */
    private String mTimeframe;

    /**
     * Timeframe parsed
     */
    private AtomicReference<Unit> mTimeframeUnit;
    private int mTimeframeLength;

    /**
     * The offset when the trading day starts (in hours)
     */
    private int mTradingDayOffset;

    /**
     * Last minute when tick volume was accumulated
     */
    private long mLastMinute;

    /**
     * The accumulated tick volume
     */
    private int mLastMinuteVolume;

    /**
     * The flag indicating whether collection is alive (e.g. receives updates)
     */
    private boolean mAlive;

    /**
     * The flag indicating whether collection is completely filled with initial
     * data
     */
    private boolean mFilled;

    /**
     * The queue of the ticks received when collection is already created, but
     * isn't filled yet by the data, received from the server
     */
    private Queue<IOffer> mWaitingUpdates;

    /**
     * The reference to the price update controller
     */
    private IPriceUpdateController mController;

    /**
     * Constructor
     * 
     * @param instrument
     *            The instrument name
     * @param timeframe
     *            The timeframe id (e.g. m1)
     * @param alive
     *            The flag indicating whether the collection shall be subscribed
     *            for updates
     * @param controller
     *            The price update controller
     */
    public PeriodCollection(String instrument, String timeframe, boolean alive,
        IPriceUpdateController controller) {
        mInstrument = instrument;
        mTimeframe = timeframe;
        mTimeframeUnit = new AtomicReference<CandlePeriod.Unit>();
        mAlive = alive;
        mFilled = false;

        if (alive) {
            // if collection is alive - we will need to calculate the date/time
            // of the candle to which each tick belongs to, so we need to parse
            // the time frame name for further usage
            Integer timeframeLength = new Integer(0); 
            if (!CandlePeriod.parsePeriod(timeframe, mTimeframeUnit, timeframeLength)) {
                throw new IllegalArgumentException("Invalid timeframe" + timeframe);
            }
            mTimeframeLength = timeframeLength.intValue();

            // and we need to subscribe to tick updates
            mWaitingUpdates = new ArrayDeque<IOffer>();
            mTradingDayOffset = controller.getTradingDayOffset();
            controller.addListener(this);
            mController = controller;
        }
    }

    /**
     * Disposes the object data
     */
    public void dispose() {
        if (mPeriods != null) {
            mPeriods.clear();
            mPeriods = null;
        }
        if (mController != null) {
            mController.removeListener(this);
            mController = null;
        }
    }

    /**
     * Adds a new historical period into the collection
     * 
     * @param time
     *            The date/time when the period starts
     * @param bidOpen
     *            The open bid price of the period
     * @param bidHigh
     *            The high bid price of the period
     * @param bidLow
     *            The low bid price of the period
     * @param bidClose
     *            The close bid price of the period
     * @param askOpen
     *            The open ask price of the period
     * @param askHigh
     *            The high ask price of the period
     * @param askLow
     *            The low ask price of the period
     * @param askClose
     *            The close ask price of the period
     * @param volume
     *            The minute volume of the period
     */
    public void add(Calendar time, double bidOpen, double bidHigh, double bidLow, double bidClose,
        double askOpen, double askHigh, double askLow, double askClose, int volume) {
        mPeriods.add(new Period(time, bidOpen, bidHigh, bidLow, bidClose, askOpen, askHigh, askLow,
            askClose, volume));
    }

    /**
     * Indicates that loading of the historical data finished and sets the
     * accumulated volume of the last minute
     * 
     * @param lastMinute
     *            The last minute when the server accumulated the tick volume
     * @param lastMinuteVolume
     *            The last minute accumulated tick volume
     */
    public void finish(Calendar lastMinute, int lastMinuteVolume) {
        mLastMinute = dateToMinute(lastMinute);
        mLastMinuteVolume = lastMinuteVolume;

        if (mAlive) {
            // process all ticks collected while we were waiting for the server
            // response and were handling it.
            // The ticks are collected in OnPriceUpdate method.
            while (mWaitingUpdates.size() > 0) {
                handleOffer(mWaitingUpdates.poll());
            }
            mFilled = true;
            notifyLastPeriodUpdated();
        }
    }

    /**
     * Notifies subscribed listeners that the last period is updated.
     */
    private void notifyLastPeriodUpdated() {
        List<IListener> listeners = new ArrayList<IListener>();
        synchronized (mListeners)  {
            listeners.addAll(mListeners);
        }

        for (IListener listener : listeners) {
            listener.onCollectionUpdate(this, mPeriods.size() - 1);
        }
    }

    /**
     * Handling one tick
     * 
     * @param offer
     */
    private void handleOffer(IOffer offer) {
        synchronized (mPeriods)
        {
            // calculate the start time of the period to which the tick belong to
            Calendar start = new GregorianCalendar(TimeZone.getTimeZone("US/Eastern"));
            Calendar end = new GregorianCalendar(TimeZone.getTimeZone("US/Eastern"));

            // calculate candle in EST time because the trading day is always
            // closed by New York time
            // so to avoid handling different hour depending on daylight saying
            // time - use EST always for candle calculations

            // NOTE: for real application this part can be optimized. The candle
            // calculation
            // is quite complex process, so it is better to avoid it when it is
            // not actually required.
            // the way to optimize it is to keep end time of the period and
            // check whether the tick belongs to
            // the period using the following condition start <= tick < end
            // so the calculation of new candle will be used only when tick is
            // actually >= of the end of the current candle.
            CandlePeriod.getCandle(mController.utcToEst(offer.getLastUpdate()), start, end,
                mTimeframeUnit.get(), mTimeframeLength, mTradingDayOffset, -1);

            start = mController.estToUtc(start);
            start.set(Calendar.SECOND, 0);
            start.set(Calendar.MILLISECOND, 0);

            // calculate the serial number of minute (for easier comparing)
            long currMinute = dateToMinute(offer.getLastUpdate());

            if (mPeriods.size() == 0) {
                // if here is no data in the collection yet - just add a dummy
                // candle
                mPeriods.add(new Period(start, offer.getBid(), offer.getAsk(), offer
                    .getMinuteVolume()));
                mLastMinute = currMinute;
                mLastMinuteVolume = offer.getMinuteVolume();
            } else {
                // otherwise get the most recent candle
                Period period = (Period) mPeriods.get(mPeriods.size() - 1);

                if (period.getDate().compareTo(start) == 0) {
                    // if tick belongs to that period...

                    // update the latest (close) price of bid and ask bars
                    period.get_Ask().setClose(offer.getAsk());
                    period.get_Bid().setClose(offer.getBid());

                    // if tick higher than high value of bars - update
                    if (period.get_Ask().getHigh() < offer.getAsk()) {
                        period.get_Ask().setHigh(offer.getAsk());
                    }
                    if (period.get_Bid().getHigh() < offer.getBid()) {
                        period.get_Bid().setHigh(offer.getBid());
                    }

                    // if tick lower than low value of bars - update
                    if (period.get_Ask().getLow() > offer.getAsk()) {
                        period.get_Ask().setLow(offer.getAsk());
                    }
                    if (period.get_Bid().getLow() > offer.getBid()) {
                        period.get_Bid().setLow(offer.getBid());
                    }

                    // here is a trick.
                    // we don't receive EVERY tick, so we can't simply count them.
                    // It is not a problem for calculating open, high, low and close, because
                    // the tick filter keeps every first, last, and the current extremum ticks
                    // In order to make the volume calculation also correct, the server
                    // broadcasts the accumulated tick volume for the current minute.

                    // so, if the tick belongs to the same minute as the previous tick -
                    // we must substract previous accumulated volume and add a new value.
                    // If the tick is the first tick of a new minute - we must simply
                    // add new accumulated value.
                    if (mLastMinute == currMinute) {
                        period.setVolume(period.getVolume() - mLastMinuteVolume);
                        period.setVolume(period.getVolume() + offer.getMinuteVolume());
                    } else if (currMinute > mLastMinute) {
                        period.setVolume(period.getVolume() + offer.getMinuteVolume());
                    }

                    mLastMinute = currMinute;
                    mLastMinuteVolume = offer.getMinuteVolume();
                } else if (period.getDate().before(start)) {
                    // this is a first tick of new period, simply create thisperiod

                    // please pay attention that we don't use the first tick as an open
                    // value but use the previous close instead.
                    // This is how the current system works by default.

                    // soon, here should be an option to use the first tick for
                    // the open price instead.
                    mPeriods.add(period = new Period(start, period.getBid().getClose(), period
                        .getAsk().getClose(), offer.getMinuteVolume()));

                    // update the latest (close) price of bid and ask bars
                    period.get_Ask().setClose(offer.getAsk());
                    period.get_Bid().setClose(offer.getBid());

                    // if tick higher than high value of bars - update
                    if (period.get_Ask().getHigh() < offer.getAsk()) {
                        period.get_Ask().setHigh(offer.getAsk());
                    }
                    if (period.get_Bid().getHigh() < offer.getBid()) {
                        period.get_Bid().setHigh(offer.getBid());
                    }

                    // if tick lower than low value of bars - update
                    if (period.get_Ask().getLow() > offer.getAsk()) {
                        period.get_Ask().setLow(offer.getAsk());
                    }
                    if (period.get_Bid().getLow() > offer.getBid()) {
                        period.get_Bid().setLow(offer.getBid());
                    }

                    mLastMinute = currMinute;
                    mLastMinuteVolume = offer.getMinuteVolume();
                } else {
                    // yep, it is possible that tick is older than the last candle.
                    // it may happen because we start to collect ticks actually BEFORE
                    // we sent the request to the server. So on the border of the minute
                    // it is possible that we "catch" some ticks of the previous minute
                    
                    // so, simply ignore them
                    ;
                }
            }
        }
    }

    /**
     * Calculates the serial number of a minute
     * 
     * @param time
     * 
     * @return
     */
    private long dateToMinute(Calendar date) {
        // get time in OLE automation date format
        Calendar OLE_BASE_DATE = new GregorianCalendar(1899, 11, 30);
        double dateOA = (date.getTimeInMillis() - OLE_BASE_DATE.getTimeInMillis()) / (60*60*24*1000);
            
        return (long) (date.get(Calendar.HOUR) * 60 + date.get(Calendar.MINUTE))
            + ((long) Math.floor(dateOA)) * 1440;
    }

    /** IPriceUpdateController.IListener */
    @Override
    public void onCollectionUpdate(IOffer offer) {
        // handle ticks only for alive collections (e.g. these which were
        // requested from the server with "up to now" parameter) and only for
        // the instrument of collection
        if (mAlive && offer.getInstrument().equals(mInstrument)) {
            if (mFilled) {
                // if collection is already filled - handle the tick right now
                handleOffer(offer);
                notifyLastPeriodUpdated();
            } else {
                // otherwise - keep it and handle later, when collection is
                // filled see Finalize() methods for handling these offers
                mWaitingUpdates.add(offer);
            }
        }
    }

    /** IPeriodCollection */
    @Override
    public IPeriod get(int index) {
        return mPeriods.get(index);
    }

    /** IPeriodCollection */
    @Override
    public String getInstrument() {
        return mInstrument;
    }

    /** IPeriodCollection */
    @Override
    public String getTimeframe() {
        return mTimeframe;
    }

    /** IPeriodCollection */
    @Override
    public boolean isAlive() {
        return mAlive;
    }

    /** IPeriodCollection */
    @Override
    public int size() {
        return mPeriods.size();
    }

    /** IPeriodCollection */
    @Override
    public Iterator<IPeriod> iterator() {
        return mPeriods.iterator();
    }

    /** IPeriodCollection */
    @Override
    public boolean addListener(IListener listener) {
        if (listener == null) {
            return false;
        }
        synchronized (mListeners) {
            mListeners.add(listener);
            return true;
        }
    }

    /** IPeriodCollection */
    @Override
    public void removeListener(IListener listener) {
        synchronized (mListeners) {
            mListeners.remove(listener);
        }
    }
}

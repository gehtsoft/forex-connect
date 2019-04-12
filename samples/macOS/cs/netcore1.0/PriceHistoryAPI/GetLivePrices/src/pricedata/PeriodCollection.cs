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
using System;
using System.Collections.Generic;
using System.Text;
using Candleworks.PriceHistoryMgr;
using Candleworks.QuotesMgr;

namespace GetLivePrices
{
    /// <summary>
    /// Implementation of the period collection
    /// </summary>
    class PeriodCollection : IPeriodCollection
    {
        #region Data fields

        /// <summary>
        /// Storage of the periods
        /// </summary>
        private List<Period> mPeriods = new List<Period>();

        /// <summary>
        /// The name of the instrument
        /// </summary>
        private string mInstrument;

        /// <summary>
        /// The name of the time frame
        /// </summary>
        private string mTimeframe;

        /// <summary>
        /// Timeframe parsed 
        /// </summary>
        private CandlePeriod.Unit mTimeframeUnit;
        private int mTimeframeLength;

        /// <summary>
        /// The offset when the trading day starts (in hours)
        /// </summary>
        private int mTradingDayOffset;

        /// <summary>
        /// Last minute when tick volume was accumulated
        /// </summary>
        private long mLastMinute;

        /// <summary>
        /// The accumulated tick volume
        /// </summary>
        private int mLastMinuteVolume;

        /// <summary>
        /// The flag indicating whether collection is alive (e.g. receives updates)
        /// </summary>
        private bool mAlive;

        /// <summary>
        /// The flag indicating whether collection is completely filled with initial data
        /// </summary>
        private bool mFilled;

        /// <summary>
        /// The queue of the ticks received when collection is already created, but
        /// isn't filled yet by the data, received from the server
        /// </summary>
        private Queue<IOffer> mWaitingUpdates;

        /// <summary>
        /// The reference to the price update controller
        /// </summary>
        private IPriceUpdateController mController;

        #endregion

        #region IPriceCollection members

        public int Count
        {
            get
            {
                return mPeriods.Count;
            }
        }

        public IPeriod this[int index]
        {
            get
            {
                return mPeriods[index];
            }
        }

        public string Instrument
        {
            get
            {
                return mInstrument;
            }
        }

        public string Timeframe
        {
            get
            {
                return mTimeframe;
            }
        }

        public bool IsAlive
        {
            get
            {
                return mAlive;
            }
        }

        /// <summary>
        /// The event is called when the collection is updated by a new tick (for alive collections)
        /// </summary>
        public event IPeriodCollection_Updated OnCollectionUpdate;

        public IEnumerator<IPeriod> GetEnumerator()
        {
            return new EnumeratorHelper<Period, IPeriod>(mPeriods.GetEnumerator());
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mPeriods.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instrument">The instrument name</param>
        /// <param name="timeframe">The timeframe id (e.g. m1)</param>
        /// <param name="alive">The flag indicating whether the collection shall be subscribed for updates</param>
        /// <param name="controller">The price update controller</param>
        public PeriodCollection(string instrument, string timeframe, bool alive, IPriceUpdateController controller)
        {
            mInstrument = instrument;
            mTimeframe = timeframe;
            mAlive = alive;
            mFilled = false;

            if (alive)
            {
                // if collection is alive - we will need to calculate the date/time of the candle
                // to which each tick belongs to, so we need to parse the time frame name for
                // further usage
                if (!CandlePeriod.parsePeriod(timeframe, ref mTimeframeUnit, ref mTimeframeLength))
                    throw new ArgumentException("Invalid timeframe", "timeframe");
                // and we need to subscribe to tick updates
                mWaitingUpdates = new Queue<IOffer>();
                mTradingDayOffset = controller.TradingDayOffset;
                controller.OnPriceUpdate += OnPriceUpdate;
                mController = controller;
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~PeriodCollection()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the object data
        /// </summary>
        public void Dispose()
        {
            if (mPeriods != null)
            {
                mPeriods.Clear();
                mPeriods = null;
            }
            if (mController != null)
            {
                mController.OnPriceUpdate -= OnPriceUpdate;
                mController = null;
            }
        }

        /// <summary>
        /// Adds a new historical period into the collection
        /// </summary>
        /// <param name="time">The date/time when the period starts</param>
        /// <param name="bidOpen">The open bid price of the period</param>
        /// <param name="bidHigh">The high bid price of the period</param>
        /// <param name="bidLow">The low bid price of the period</param>
        /// <param name="bidClose">The close bid price of the period</param>
        /// <param name="askOpen">The open ask price of the period</param>
        /// <param name="askHigh">The high ask price of the period</param>
        /// <param name="askLow">The low ask price of the period</param>
        /// <param name="askClose">The close ask price of the period</param>
        /// <param name="volume">The minute volume of the period</param>
        public void Add(DateTime time, double bidOpen, double bidHigh, double bidLow, double bidClose,
                                       double askOpen, double askHigh, double askLow, double askClose,
                                       int volume)
        {
            mPeriods.Add(new Period(time, bidOpen, bidHigh, bidLow, bidClose, askOpen, askHigh, askLow, askClose, volume));
        }

        /// <summary>
        /// Indicates that loading of the historical data finished and sets 
        /// the accumulated volume of the last minute
        /// </summary>
        /// <param name="lastMinute">The last minute when the server accumulated the tick volume</param>
        /// <param name="lastMinuteVolume">The last minute accumulated tick volume</param>
        public void Finish(DateTime lastMinute, int lastMinuteVolume)
        {
            mLastMinute = DateToMinute(lastMinute);
            mLastMinuteVolume = lastMinuteVolume;

            if (mAlive)
            {
                // process all ticks collected while we were waiting for the server response and were handling it.
                // The ticks are collected in OnPriceUpdate method.
                while (mWaitingUpdates.Count > 0)
                    HandleOffer(mWaitingUpdates.Dequeue());
                mFilled = true;
                if (OnCollectionUpdate != null)
                    OnCollectionUpdate(this, mPeriods.Count - 1);
            }
        }

        /// <summary>
        /// Event of PriceUpdateController: When a new tick comes
        /// </summary>
        /// <param name="offer"></param>
        private void OnPriceUpdate(IOffer offer)
        {
            // handle ticks only for alive collections (e.g. these which were requested
            // from the server with "up to now" parameter) and only for 
            // the instrument of collection
            if (mAlive && offer.Instrument == mInstrument)
            {
                if (mFilled)
                {
                    // if collection is already filled - handle the tick right now
                    HandleOffer(offer);
                    if (OnCollectionUpdate != null)
                        OnCollectionUpdate(this, mPeriods.Count - 1);
                }
                else
                {
                    // otherwise - keep it and handle later, when collection is filled
                    // see Finalize() methods for handling these offers
                    mWaitingUpdates.Enqueue(offer);
                }
            }
        }

        /// <summary>
        /// Handling one tick
        /// </summary>
        /// <param name="offer"></param>
        private void HandleOffer(IOffer offer)
        {
            lock (mPeriods)
            {
                // calculate the start time of the period to which the tick belong to
                DateTime start = DateTime.MinValue, end = DateTime.MinValue;
                
                // calculate candle in EST time because the trading day is always closed by New York time
                // so to avoid handling different hour depending on daylight saying time - use EST always
                // for candle calculations

                // NOTE: for real application this part can be optimized. The candle calculation 
                // is quite complex process, so it is better to avoid it when it is not actually required.
                // the way to optimize it is to keep end time of the period and check whether the tick belongs to 
                // the period using the following condition start <= tick < end
                // so the calculation of new candle will be used only when tick is actually >= of the end 
                // of the current candle.
                CandlePeriod.getCandle(mController.UtcToEst(offer.LastUpdate), ref start, ref end, mTimeframeUnit, mTimeframeLength, mTradingDayOffset, -1);
                start = mController.EstToUtc(start);
                // calculate the serial number of minute (for easier comparing) 
                long currMinute = DateToMinute(offer.LastUpdate);

                if (mPeriods.Count == 0)
                {
                    // if here is no data in the collection yet - just add a dummy candle
                    mPeriods.Add(new Period(start, offer.Bid, offer.Ask, offer.MinuteVolume));
                    mLastMinute = currMinute;
                    mLastMinuteVolume = offer.MinuteVolume;
                }
                else 
                {
                    // otherwise get the most recent candle
                    Period period = mPeriods[mPeriods.Count - 1];
                    if (period.Time == start)
                    {
                        // if tick belongs to that period...

                        // update the latest (close) price of bid and ask bars
                        period._Ask.Close = offer.Ask;
                        period._Bid.Close = offer.Bid;

                        // if tick higher than high value of bars - update 
                        if (period._Ask.High < offer.Ask)
                            period._Ask.High = offer.Ask;
                        if (period._Bid.High < offer.Bid)
                            period._Bid.High = offer.Bid;

                        // if tick lower than low value of bars - update 
                        if (period._Ask.Low > offer.Ask)
                            period._Ask.Low = offer.Ask;
                        if (period._Bid.Low > offer.Bid)
                            period._Bid.Low = offer.Bid;

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
                        if (mLastMinute == currMinute)
                        {
                            period.Volume -= mLastMinuteVolume;
                            period.Volume += offer.MinuteVolume;
                        }
                        else if (currMinute > mLastMinute)
                        {
                            period.Volume += offer.MinuteVolume;
                        }

                        mLastMinute = currMinute;
                        mLastMinuteVolume = offer.MinuteVolume;
                    }
                    else if (period.Time < start)
                    {
                        // this is a first tick of new period, simply create this period
                        
                        // please pay attention that we don't use the first tick as an open
                        // value but use the previous close instead.
                        // This is how the current system works by default.

                        // soon, here should be an option to use the first tick for the open 
                        // price instead. 
                        mPeriods.Add(period = new Period(start, period.Bid.Close, period.Ask.Close, offer.MinuteVolume));

                        // update the latest (close) price of bid and ask bars
                        period._Ask.Close = offer.Ask;
                        period._Bid.Close = offer.Bid;

                        // if tick higher than high value of bars - update 
                        if (period._Ask.High < offer.Ask)
                            period._Ask.High = offer.Ask;
                        if (period._Bid.High < offer.Bid)
                            period._Bid.High = offer.Bid;

                        // if tick lower than low value of bars - update 
                        if (period._Ask.Low > offer.Ask)
                            period._Ask.Low = offer.Ask;
                        if (period._Bid.Low > offer.Bid)
                            period._Bid.Low = offer.Bid;

                        mLastMinute = currMinute;
                        mLastMinuteVolume = offer.MinuteVolume;
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
        }

        /// <summary>
        /// Calculates the serial number of a minute
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private long DateToMinute(DateTime time)
        {
            return (long)(time.Hour * 60 + time.Minute) + ((long)Math.Floor(OLEDateTimeConverter.ToOADate(time))) * 1440;
        }
    }
}

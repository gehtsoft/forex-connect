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
using System.Globalization;
using System.Threading;

using fxcore2;

namespace GetLivePrices
{
    public delegate void PriceUpdateController_PriceUpdate(IOffer offer);

    /// <summary>
    /// Interface of a price update controller which notifies an event
    /// when a new price (tick) of some instrument is received. 
    /// It also contains some helper time conversion methods.
    /// </summary>
    public interface IPriceUpdateController
    {
        /// <summary>
        /// Converts NYT to UTC
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        DateTime EstToUtc(DateTime time);

        /// <summary>
        /// Converts UTC to NYT
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        DateTime UtcToEst(DateTime time);

        /// <summary>
        /// Event: When the price is updated
        /// </summary>
        event PriceUpdateController_PriceUpdate OnPriceUpdate;

        /// <summary>
        /// Gets the trading day offset
        /// </summary>
        int TradingDayOffset
        {
            get;
        }
    }

    /// <summary>
    /// The class listens for ticks notifications which are sent by ForexConnect
    /// and notifies its listeners.
    /// </summary>
    public class PriceUpdateController : IO2GResponseListener, IPriceUpdateController
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PriceUpdateController(O2GSession session, string instrument)
        {
            mSyncOfferEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

            mInstrument = instrument;
            mSession = session;
            mInitFailed = false;

            mSession.subscribeResponse(this);

            mTZConverter = session.getTimeConverter();

            // get the trading day offset
            O2GLoginRules loginRules = session.getLoginRules();
            O2GResponse response = loginRules.getSystemPropertiesResponse();
            O2GSystemPropertiesReader reader = session.getResponseReaderFactory().createSystemPropertiesReader(response);
            string eod = reader.Properties["END_TRADING_DAY"];
            DateTime time = DateTime.ParseExact("01.01.1900_" + eod, "MM.dd.yyyy_HH:mm:ss", CultureInfo.InvariantCulture);
            // convert Trading day start to EST time because the trading day is always closed by New York time
            // so to avoid handling different hour depending on daylight saying time - use EST always
            // for candle calculations
            time = mTZConverter.convert(time, O2GTimeConverterTimeZone.UTC, O2GTimeConverterTimeZone.EST);
            // here we have the date when trading day begins, e.g. 17:00:00
            // please note that if trading day begins before noon - it begins AFTER calendar date is started,
            // so the offset is positive (e.g. 03:00 is +3 offset).
            // if trading day begins after noon, it begins BEFORE calendar date is istarted,
            // so the offset is negative (e.g. 17:00 is -7 offset).
            if (time.Hour <= 12)
                mTradingDayOffset = time.Hour;
            else
                mTradingDayOffset = time.Hour - 24;

            // get latest offer for the instrument
            GetLatestOffer();
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~PriceUpdateController()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Waits until the Offers table is loaded.
        /// </summary>
        public bool Wait()
        {
            if (mInitFailed)
                return false;
            
            bool eventOcurred = mSyncOfferEvent.WaitOne(30000);
            if (!eventOcurred)
                Console.WriteLine("Timeout occurred during waiting for loading of Offers table");
            
            if (mInitFailed)
                return false;
            
            return eventOcurred;
        }

        /// <summary>
        /// Unsubscribes the object from the session events.
        /// </summary>
        public void Unsubscribe()
        {
            mSession.unsubscribeResponse(this);
        }

        #region  O2GResponseListener members

        /// <summary>
        /// Listener: Forex Connect request handled.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="response"></param>
        public void onRequestCompleted(string requestId, O2GResponse response)
        {
            // we need only offer table refresh for our example
            if (response.Type == O2GResponseType.GetOffers)
            {
                // so simply read and store offers
                O2GOffersTableResponseReader reader = mSession.getResponseReaderFactory().createOffersTableReader(response);
                for (int i = 0; i < reader.Count; i++)
                {
                    O2GOfferRow row = reader.getRow(i);
                    if (mInstrument.Equals(row.Instrument))
                    {
                        mOffer = new Offer(row.Instrument, row.Time, row.Bid, row.Ask, row.Volume, row.Digits);
                        mSyncOfferEvent.Set();
                        break;
                    }
                }
                
                if (mOffer == null) 
                {
                    Console.WriteLine("Instrument is not found.\n");
                    mInitFailed = true;
                    mSyncOfferEvent.Set();
                }
                
            }
        }

        /// <summary>
        /// Listener: Forex Connect request handled.
        /// </summary>
        /// <param name="requestId"></param>
        public void onRequestCompleted(string requestId)
        {
        }

        /// <summary>
        /// Listener: Forex Connect Request failed.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="error"></param>
        public void onRequestFailed(string requestId, string error)
        {
        }

        /// <summary>
        /// Listener: Forex Connect received update for trading tables.
        /// </summary>
        /// <param name="data"></param>
        public void onTablesUpdates(O2GResponse data)
        {
            if (data.Type == O2GResponseType.TablesUpdates)
            {
                if (mOffer == null)
                    return;

                O2GTablesUpdatesReader reader = mSession.getResponseReaderFactory().createTablesUpdatesReader(data);
                for (int i = 0; i < reader.Count; i++)
                {
                    // We are looking only for updates and only for offers

                    // NOTE: in order to support offer subscribtion, the real application also will need 
                    // to read add/remove events and change the offer collection correspondingly
                    if (reader.getUpdateType(i) == O2GTableUpdateType.Update &&
                        reader.getUpdateTable(i) == O2GTableType.Offers)
                    {
                        // read the offer update
                        O2GOfferRow row = reader.getOfferRow(i);

                        if (!row.isInstrumentValid)
                            continue;

                        if (!mInstrument.Equals(row.Instrument))
                            continue;

                        // update latest offer
                        if (row.isTimeValid)
                            mOffer.LastUpdate = row.Time;
                        if (row.isBidValid)
                            mOffer.Bid = row.Bid;
                        if (row.isAskValid)
                            mOffer.Ask = row.Ask;
                        if (row.isVolumeValid)
                            mOffer.MinuteVolume = row.Volume;

                        // please note that we send a clone of the offer,
                        // we need it in order to make sure that no ticks
                        // will be lost, if, for example, subscriber doesn't handle the 
                        // offer immideatelly 
                        if (OnPriceUpdate != null)
                            OnPriceUpdate(mOffer.Clone());
                    }
                }
            }
        }

        #endregion

        #region IPriceUpdateController methods

        /// <summary>
        /// Converts NYT to UTC
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public DateTime EstToUtc(DateTime time)
        {
            return mTZConverter.convert(time, O2GTimeConverterTimeZone.EST, O2GTimeConverterTimeZone.UTC);
        }

        /// <summary>
        /// Converts UTC to NYT
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public DateTime UtcToEst(DateTime time)
        {
            return mTZConverter.convert(time, O2GTimeConverterTimeZone.UTC, O2GTimeConverterTimeZone.EST);
        }

        /// <summary>
        /// Gets the trading day offset
        /// </summary>
        public int TradingDayOffset
        {
            get 
            {
                return mTradingDayOffset;
            }
        }

        #endregion

        /// <summary>
        /// Get the latest offer to which the user is subscribed
        /// </summary>
        private void GetLatestOffer()
        {
            // get the list of the offers to which the user is subscribed
            O2GLoginRules loginRules = mSession.getLoginRules();
            O2GResponse response = loginRules.getSystemPropertiesResponse();

            if (loginRules.isTableLoadedByDefault(O2GTableType.Offers))
            {
                // if it is already loaded - just handle them
                response = loginRules.getTableRefreshResponse(O2GTableType.Offers);
                onRequestCompleted(null, response);
            }
            else
            {
                // otherwise create the request to get offers from the server
                O2GRequestFactory factory = mSession.getRequestFactory();
                O2GRequest offerRequest = factory.createRefreshTableRequest(O2GTableType.Offers);
                mSession.sendRequest(offerRequest);
            }
        }

        #region Data fields

        /// <summary>
        /// Event: When the price is updated
        /// </summary>
        public event PriceUpdateController_PriceUpdate OnPriceUpdate;

        /// <summary>
        /// The ForexConnect trading session
        /// </summary>
        private O2GSession mSession;

        /// <summary>
        /// The instrument
        /// </summary>
        private string mInstrument;

        /// <summary>
        /// The latest offer of the instrument available for the trading session
        /// </summary>
        private Offer mOffer;

        /// <summary>
        /// The event used to wait for Offers loading state.
        /// </summary>
        private EventWaitHandle mSyncOfferEvent;

        /// <summary>
        /// Time zone converter
        /// </summary>
        private O2GTimeConverter mTZConverter;

        /// <summary>
        /// The offet in hours of when the trading day start in relation to the midnight
        /// </summary>
        private int mTradingDayOffset;
        
        /// <summary>
        /// The flag that indicates that we failed to find an Offer.
        /// </summary>
        private bool mInitFailed;

        #endregion
    }

}

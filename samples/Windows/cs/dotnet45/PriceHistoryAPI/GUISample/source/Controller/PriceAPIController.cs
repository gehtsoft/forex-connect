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
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Text;

using fxcore2;
using Candleworks.PriceHistoryMgr;
using Candleworks.QuotesMgr;

namespace GUISample
{
    delegate void PriceAPIController_Error(string error);
    delegate void PriceAPIController_PriceUpdate(IOffer offer);
    delegate void PriceAPIController_StateChange(bool isReady);
    delegate void PriceAPIController_CollectionLoaded(IPeriodCollection collection);

    /// <summary>
    /// The controller for ForexConnect API and Price History API operations
    /// </summary>
    class PriceAPIController : IO2GSessionStatus, IO2GResponseListener, IPriceHistoryCommunicatorListener, IPriceHistoryCommunicatorStatusListener
    {
        #region Controller data

        /// <summary>
        /// The handle of the ForexConnect trading session
        /// </summary>
        private O2GSession mTradingSession;
        /// <summary>
        /// The handle of the Price History API communicator
        /// </summary>
        private IPriceHistoryCommunicator mPriceHistoryCommunicator;
        /// <summary>
        /// The collection of the offers available for the trading session
        /// </summary>
        private OfferCollection mOffers = new OfferCollection();
        /// <summary>
        /// The offet in hours of when the2 trading day start in relation to the midnight
        /// </summary>
        private int mTradingDayOffset;
        /// <summary>
        /// The request which are being currently executed
        /// </summary>
        private Dictionary<IPriceHistoryCommunicatorRequest, PeriodCollection> mHistories = new Dictionary<IPriceHistoryCommunicatorRequest, PeriodCollection>();
        /// <summary>
        /// Time zone converter
        /// </summary>
        private O2GTimeConverter mTZConverter;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public PriceAPIController()
        {

        }

        #region Initialization/deinitialization

        /// <summary>
        /// Initializes the controller.
        /// 
        /// Please note that controller doesn't become available immediatelly. Watch for OnStateChange event.
        /// </summary>
        /// <param name="userName">Trader's user name</param>
        /// <param name="password">Trader's password</param>
        /// <param name="url">Connection URL</param>
        /// <param name="terminal">The terminal name (e.g. Demo or Real)</param>
        /// <param name="cachePath">Absolute or relative path where the cache data shall be located</param>
        public void initialize(string userName, string password, string url, string terminal, string cachePath)
        {
            // create the trading session and subscribe for it's events
            mTradingSession = O2GTransport.createSession();
            mTradingSession.subscribeSessionStatus(this);
            mTradingSession.subscribeResponse(this);
            // create a price history communicator and subscribe for its events
            cachePath = Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName, cachePath);
            mPriceHistoryCommunicator = PriceHistoryCommunicatorFactory.createCommunicator(mTradingSession, cachePath);
            mPriceHistoryCommunicator.addStatusListener(this);
            mPriceHistoryCommunicator.addListener(this);
            // Start login process
            mTradingSession.login(userName, password, url, terminal);
        }

        /// <summary>
        /// Releases the controller
        /// </summary>
        public void release()
        {
            if (mTradingSession != null)
            {
                mTradingSession.unsubscribeResponse(this);
                mTradingSession.unsubscribeSessionStatus(this);
                // terminate the trading session
                mTradingSession.logout();
                // wait while it is finished
                // NOTE: in a real application use event listener for the session statuses to avoid this locking loop
                while (mTradingSession.getSessionStatus() != O2GSessionStatusCode.Disconnected)
                    Thread.Sleep(100);
                // finalize commnicator and dispose session
                mPriceHistoryCommunicator.removeStatusListener(this);
                mPriceHistoryCommunicator.removeListener(this);
                mPriceHistoryCommunicator = null;
                mTradingSession = null;
            }
        }

        #endregion

        #region Properties and commands

        /// <summary>
        /// Gets the collection of the offers
        /// </summary>
        public IOfferCollection Offers
        {
            get
            {
                return mOffers;
            }
        }

        /// <summary>
        /// Checks whether the controller is ready
        /// </summary>
        public bool IsReady
        {
            get
            {
                if (mTradingSession != null && mPriceHistoryCommunicator != null)
                {
                    return mTradingSession.getSessionStatus() == O2GSessionStatusCode.Connected &&
                           mPriceHistoryCommunicator.isReady() &&
                           mOffers.Count > 0;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets an instance of the Quotes Manager associated with the price controller.
        /// </summary>
        public QuotesManager QuotesManager
        {
            get
            {
                if (mPriceHistoryCommunicator == null || !mPriceHistoryCommunicator.isReady())
                    return null;
                return mPriceHistoryCommunicator.getQuotesManager();
            }
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

        /// <summary>
        /// Gets the list of the timeframes
        /// </summary>
        public O2GTimeframeCollection Timeframes
        {
            get
            {
                return mTradingSession.getRequestFactory().Timeframes;
            }
        }

        /// <summary>
        /// Event: When any error in the handling occurs
        /// </summary>
        public event PriceAPIController_Error OnErrorEvent;

        /// <summary>
        /// Event: When the price is updated
        /// </summary>
        public event PriceAPIController_PriceUpdate OnPriceUpdate;

        /// <summary>
        /// Event: When the readyness state is changed
        /// </summary>
        public event PriceAPIController_StateChange OnStateChange;

        /// <summary>
        /// Event: When the collection is loaded
        /// </summary>
        public event PriceAPIController_CollectionLoaded OnCollectionLoaded;

        /// <summary>
        /// Gets the historical data up to now and subscribe it for price updates
        /// </summary>
        /// <param name="instrument">The instrument</param>
        /// <param name="timeframe">The timeframe</param>
        /// <param name="from">The date/time to get the data from</param>
        public void RequestAndSubscribeHistory(string instrument, O2GTimeframe timeframe, DateTime from)
        {
            IPriceHistoryCommunicatorRequest request = mPriceHistoryCommunicator.createRequest(instrument, timeframe, from, 
                Candleworks.PriceHistoryMgr.Constants.ZERODATE, -1);
            // here we create a collection right now in order to let it subscribe for all ticks (offer changes)
            // which may occur while the request is being executed, so no data is lost because of gap while the server
            // is already collected all data, but the client isn't started to update collection yet.
            PeriodCollection collection = new PeriodCollection(instrument, timeframe.ID, true, this);
            mHistories[request] = collection;
            mPriceHistoryCommunicator.sendRequest(request);
            // ... to response handling see onRequestCompleted in Price Communicator Event handler section
        }

        /// <summary>
        /// Requests the historical data
        /// </summary>
        /// <param name="instrument">The instrument</param>
        /// <param name="timeframe">The timeframe</param>
        /// <param name="from">Date/time to get the data from</param>
        /// <param name="to">Date/time to get the data to</param>
        public void RequestHistory(string instrument, O2GTimeframe timeframe, DateTime from, DateTime to)
        {
            IPriceHistoryCommunicatorRequest request = mPriceHistoryCommunicator.createRequest(instrument, timeframe, from, to, -1);
            // We don't need a pre-created collection for pure historical data, but just lets create it
            // to handle the response in the same way.
            PeriodCollection collection = new PeriodCollection(instrument, timeframe.ID, false, this);
            mHistories[request] = collection;
            mPriceHistoryCommunicator.sendRequest(request);
            // ... to response handling see onRequestCompleted in Price Communicator Event handler section
        }

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

        #endregion

        #region IO2GSessionStatus members

        /// <summary>
        /// Listener: When Trading session login failed
        /// </summary>
        /// <param name="error"></param>
        public void onLoginFailed(string error)
        {
            if (OnErrorEvent != null)
                OnErrorEvent(error);
        }

        /// <summary>
        /// Listener: When Trading session status is changed
        /// </summary>
        /// <param name="status"></param>
        public void onSessionStatusChanged(O2GSessionStatusCode status)
        {
            switch (status)
            {
            case    O2GSessionStatusCode.TradingSessionRequested:
                    // If the trading session requires the session name or pin code...
                    if (OnErrorEvent != null)
                        OnErrorEvent("Multi-session connectors aren't supported by this example\n");
                    mTradingSession.logout();
                    break;
            case    O2GSessionStatusCode.Connected:
                    // login is completed

                    // now we need collect data about the system properties 
                    O2GLoginRules loginRules = mTradingSession.getLoginRules();
                    mTZConverter = mTradingSession.getTimeConverter();
                    // get the trading day offset.
                    O2GResponse response;
                    response = loginRules.getSystemPropertiesResponse();
                    O2GSystemPropertiesReader reader = mTradingSession.getResponseReaderFactory().createSystemPropertiesReader(response);
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

                    // ...and now get the list of the offers to which the user is subscribed
                    if (loginRules.isTableLoadedByDefault(O2GTableType.Offers))
                    {
                        // if it is already loaded - just handle them
                        response = loginRules.getTableRefreshResponse(O2GTableType.Offers);
                        onRequestCompleted(null, response);
                    }
                    else
                    {
                        // otherwise create the request to get offers from the server
                        O2GRequestFactory factory = mTradingSession.getRequestFactory();
                        O2GRequest offerRequest = factory.createRefreshTableRequest(O2GTableType.Offers);
                        mTradingSession.sendRequest(offerRequest);
                    }
                    break;
            default:
                    if (OnStateChange != null)
                        OnStateChange(false);
                    break;
            }
        }

        #endregion

        #region IPriceHistoryCommunicatorListener members

        /// <summary>
        /// Listener: When the commnicator status is changed
        /// </summary>
        /// <param name="isReady"></param>
        public void onCommunicatorStatusChanged(bool isReady)
        {
            if (OnStateChange != null)
            {
                // if offers are already read - notify that we're ready.
                // so, the ready event may come either way - or from here, or from 
                // the place where offer table is formed, which of these two happened first.
                if (isReady && mOffers.Count > 0)
                    OnStateChange(true);
                else
                    OnStateChange(false);
            }
        }

        /// <summary>
        /// Listener: when the communicator initialization is failed
        /// </summary>
        /// <param name="error"></param>
        public void onCommunicatorInitFailed(PriceHistoryError error)
        {
            if (OnErrorEvent != null)
                OnErrorEvent(error.Message);
        }

        /// <summary>
        /// Listener: when the request is cancelled
        /// </summary>
        /// <param name="request"></param>
        public void onRequestCancelled(IPriceHistoryCommunicatorRequest request)
        {
            // simply remove the request from the list of waiting histories
            mHistories.Remove(request);
        }

        /// <summary>
        /// Listener: Price history request is completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public void onRequestCompleted(IPriceHistoryCommunicatorRequest request, IPriceHistoryCommunicatorResponse response)
        {
            // try to find the collection (must be created at the moment when the request is sent)
            PeriodCollection coll = null;
            mHistories.TryGetValue(request, out coll);
            if (coll != null)
            {
                // create a reader for the price history data 
                O2GMarketDataSnapshotResponseReader reader = mPriceHistoryCommunicator.createResponseReader(response);
                // and read all bars
                for (int i = 0; i < reader.Count; i++)
                {
                    coll.Add(reader.getDate(i), reader.getBidOpen(i), reader.getBidHigh(i), reader.getBidLow(i), reader.getBidClose(i),
                                                reader.getAskOpen(i), reader.getAskHigh(i), reader.getAskLow(i), reader.getAskClose(i),
                                                reader.getVolume(i));
                }
                // finally notify the collection that all bars are added, so it can
                // add all ticks collected while the request was being executed
                // and start update the data by forthcoming ticks (for alive collections)
                coll.Finish(reader.getLastBarTime(), reader.getLastBarVolume());
            }
            // now we can remove collection from our cache and send it to the application
            mHistories.Remove(request);
            if (coll != null && OnCollectionLoaded != null)
                OnCollectionLoaded(coll);
        }

        /// <summary>
        /// Listener: request failed.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="error"></param>
        public void onRequestFailed(IPriceHistoryCommunicatorRequest request, PriceHistoryError error)
        {
            mHistories.Remove(request);
            if (OnErrorEvent != null)
                OnErrorEvent(error.Message);
        }

        #endregion

        #region Forex Connect response listener

        /// <summary>
        /// Listener: Forex Connect request handled
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="response"></param>
        public void onRequestCompleted(string requestId, O2GResponse response)
        {
            // we need only offer table refresh for our example
            if (response.Type == O2GResponseType.GetOffers)
            {
                // so simply read and store offers
                O2GOffersTableResponseReader reader = mTradingSession.getResponseReaderFactory().createOffersTableReader(response);
                mOffers.Clear();
                for (int i = 0; i < reader.Count; i++)
                {
                    O2GOfferRow row = reader.getRow(i);
                    mOffers.Add(row.OfferID, row.Instrument, row.Time, row.Bid, row.Ask, row.Volume, row.Digits);
                }
                // if price history communicator has been initialized before we get offer - notify that we're ready.
                if (mPriceHistoryCommunicator.isReady() && OnStateChange != null)
                    OnStateChange(true);
            }
        }

        /// <summary>
        /// Listener: Forex Connect Request failed
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="error"></param>
        public void onRequestFailed(string requestId, string error)
        {
            if (OnErrorEvent != null)
                OnErrorEvent(error);
        }

        /// <summary>
        /// Listener: Forex Connect received update for trading tables
        /// </summary>
        /// <param name="data"></param>
        public void onTablesUpdates(O2GResponse data)
        {
            if (data.Type == O2GResponseType.TablesUpdates)
            {
                O2GTablesUpdatesReader reader = mTradingSession.getResponseReaderFactory().createTablesUpdatesReader(data);
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
                        Offer offer;
                        // find the offer in our list either by the instrument name or 
                        // by the offer id
                        string id = null;
                        if (row.isInstrumentValid)
                            id = row.Instrument;
                        else if (row.isOfferIDValid)
                            id = row.OfferID;

                        if (id == null)
                            continue;

                        offer = mOffers[id];
                        if (offer == null)
                            continue;

                        // if we have that offer - update it
                        if (row.isTimeValid)
                            offer.LastUpdate = row.Time;
                        if (row.isBidValid)
                            offer.Bid = row.Bid;
                        if (row.isAskValid)
                            offer.Ask = row.Ask;
                        if (row.isVolumeValid)
                            offer.MinuteVolume = row.Volume;

                        // please note that we send a clone of the offer,
                        // we need it in order to make sure that no ticks
                        // will be lost, if, for example, subscriber doesn't handle the 
                        // offer immideatelly 
                        if (OnPriceUpdate != null)
                            OnPriceUpdate(offer.Clone());
                    }
                }
            }
        }

        #endregion
    }
}

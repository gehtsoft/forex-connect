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
package getliveprices;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.List;
import java.util.ArrayList;
import java.util.TimeZone;
import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

import getliveprices.pricedata.IPeriodCollection;
import getliveprices.pricedata.Offer;
import getliveprices.pricedata.IOffer;

/**
 * The class listens for ticks notifications which are sent by ForexConnect and
 * notifies its listeners.
 */
public class PriceUpdateController implements IO2GResponseListener,
    IPriceUpdateController {

    /**
     * The listeners collection
     */
    List<IListener> mListeners = new ArrayList<IListener>();

    /**
     * The ForexConnect trading session
     */
    private O2GSession mSession;

    /**
     * The instrument
     */
    private String mInstrument;

    /**
     * The latest offer of the instrument available for the trading session
     */
    private Offer mOffer;

    /**
     * The event used to wait for Offers loading state.
     */
    private Semaphore mSemaphore;

    /**
     * Time zone converter
     */
    private O2GTimeConverter mTZConverter;

    /**
     * The offet in hours of when the2 trading day start in relation to the
     * midnight
     */
    private int mTradingDayOffset;
    
    /** The flag that indicates that we failed to find an Offer. */
    private boolean mInitFailed;

    /**
     * Constructor.
     * 
     * @param session
     * @param instrument
     * @throws Exception
     */
    public PriceUpdateController(O2GSession session, String instrument) throws Exception {
        mSemaphore = new Semaphore(0);

        mInstrument = instrument;
        mSession = session;
        mInitFailed = false;

        mSession.subscribeResponse(this);

        mTZConverter = session.getTimeConverter();

        // get the trading day offset
        O2GLoginRules loginRules = session.getLoginRules();
        O2GResponse response = loginRules.getSystemPropertiesResponse();
        O2GSystemPropertiesReader reader = session.getResponseReaderFactory()
            .createSystemPropertiesReader(response);

        String eod = reader.getProperties().get("END_TRADING_DAY");
        if (null == eod) {
            throw new Exception("Can't get 'end trading day' property from response");
        }

        SimpleDateFormat dateFormat = new SimpleDateFormat("MM.dd.yyyy_HH:mm:ss");
        dateFormat.setTimeZone(TimeZone.getTimeZone("UTC"));
        Calendar time = Calendar.getInstance(TimeZone.getTimeZone("UTC"));

        try {
            Date date = dateFormat.parse("01.01.1900_" + eod);
            time.setTime(date);
        } catch (ParseException e) {
            throw new Exception("Can't get hour offset of start of trading day");
        }

        // convert Trading day start to EST time because the trading day is
        // always closed by New York time so to avoid handling different hour
        // depending on daylight saying time - use EST always for candle
        // calculations

        time = mTZConverter.convert(time, O2GTimeConverterTimeZone.EST);

        // here we have the date when trading day begins, e.g. 17:00:00
        // please note that if trading day begins before noon - it begins AFTER
        // calendar date is started, so the offset is positive (e.g. 03:00 is +3
        // offset). if trading day begins after noon, it begins BEFORE calendar
        // date is istarted, so the offset is negative (e.g. 17:00 is -7
        // offset).

        if (time.get(Calendar.HOUR_OF_DAY) <= 12) {
            mTradingDayOffset = time.get(Calendar.HOUR_OF_DAY);
        } else {
            mTradingDayOffset = time.get(Calendar.HOUR_OF_DAY) - 24;
        }

        // get latest offer for the instrument
        getLatestOffer();
    }

    /**
     * Waits until the Offers table is loaded.
     * 
     * @throws InterruptedException
     */
    public boolean waitEvents() throws InterruptedException {
        if (mInitFailed) {
            return false;
        }
    
        boolean eventOcurred = mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
        if (!eventOcurred) {
            System.out.println("Timeout occurred during waiting for loading of Offers table");
        }
        
        if (mInitFailed) {
            return false;
        }
    
        return eventOcurred;
    }

    /**
     * Unsubscribes the object from the session events.
     */
    public void unsubscribe() {
        mSession.unsubscribeResponse(this);
    }

    /**
     * Listener: Forex Connect request handled.
     * 
     * @param requestId
     * @param response
     */
    @Override
    public void onRequestCompleted(String requestId, O2GResponse response) {
        // we need only offer table refresh for our example
        if (response.getType() == O2GResponseType.GET_OFFERS) {
            // so simply read and store offers
            O2GOffersTableResponseReader reader = mSession.getResponseReaderFactory()
                .createOffersTableReader(response);
            for (int i = 0; i < reader.size(); i++) {
                O2GOfferRow row = reader.getRow(i);
                if (mInstrument.equals(row.getInstrument())) {
                    mOffer = new Offer(row.getInstrument(), row.getTime(), row.getBid(),
                        row.getAsk(), row.getVolume(), row.getDigits());

                    mSemaphore.release();
                    break;
                }
            }
            
            if (mOffer == null) {
                System.out.println("Instrument is not found.\n");
                mInitFailed = true;
                mSemaphore.release();
            }
        
        }
    }

    /**
     * Listener: Forex Connect request handled.
     * 
     * @param requestId
     */
    public void onRequestCompleted(String requestId) {
    }

    /**
     * Listener: Forex Connect Request failed.
     * 
     * @param requestId
     * 
     * @param error
     */
    @Override
    public void onRequestFailed(String requestId, String error) {
    }

    /**
     * Listener: Forex Connect received update for trading tables.
     * 
     * @param data
     */
    @Override
    public void onTablesUpdates(O2GResponse data) {
        if (data.getType() == O2GResponseType.TABLES_UPDATES) {
            if (mOffer == null) {
                return;
            }

            O2GTablesUpdatesReader reader = mSession.getResponseReaderFactory()
                .createTablesUpdatesReader(data);
            for (int i = 0; i < reader.size(); i++) {
                // We are looking only for updates and only for offers

                // NOTE: in order to support offer subscribtion, the real
                // application also will need
                // to read add/remove events and change the offer collection
                // correspondingly
                if (reader.getUpdateType(i) == O2GTableUpdateType.UPDATE
                    && reader.getUpdateTable(i) == O2GTableType.OFFERS) {
                    // read the offer update
                    O2GOfferRow row = reader.getOfferRow(i);

                    if (!row.isInstrumentValid()) {
                        continue;
                    }

                    if (!mInstrument.equals(row.getInstrument())) {
                        continue;
                    }

                    // update latest offer
                    if (row.isTimeValid()) {
                        mOffer.setLastUpdate(row.getTime());
                    }
                    if (row.isBidValid()) {
                        mOffer.setBid(row.getBid());
                    }
                    if (row.isAskValid()) {
                        mOffer.setAsk(row.getAsk());
                    }
                    if (row.isVolumeValid()) {
                        mOffer.setMinuteVolume(row.getVolume());
                    }

                    // please note that we send a clone of the offer,
                    // we need it in order to make sure that no ticks
                    // will be lost, if, for example, subscriber doesn't handle
                    // the offer immediately
                    notifyPriceUpdate(mOffer.clone());
                }
            }
        }
    }

    /**
     * IPriceUpdateController
     * Converts NYT to UTC
     * 
     * @param time
     * @return
     */
    @Override
    public Calendar estToUtc(Calendar time) {
        return mTZConverter.convert(time, O2GTimeConverterTimeZone.UTC);
    }

    /**
     * IPriceUpdateController
     * Converts UTC to NYT
     * 
     * @param time
     * @return
     */
    @Override
    public Calendar utcToEst(Calendar time) {
        return mTZConverter.convert(time, O2GTimeConverterTimeZone.EST);
    }

    /**
     * IPriceUpdateController
     * Gets the trading day offset
     */
    @Override
    public int getTradingDayOffset() {
        return mTradingDayOffset;
    }

    /**
     * IPriceUpdateController
     * Add listener.
     */
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

    /**
     * IPriceUpdateController
     * Remove listener.
     */
    @Override
    public void removeListener(IListener listener) {
        synchronized (mListeners) {
            mListeners.remove(listener);
        }
    }

    /**
     * Get the latest offer to which the user is subscribed
     */
    private void getLatestOffer() {
        // get the list of the offers to which the user is subscribed
        O2GLoginRules loginRules = mSession.getLoginRules();
        O2GResponse response = loginRules.getSystemPropertiesResponse();

        if (loginRules.isTableLoadedByDefault(O2GTableType.OFFERS)) {
            // if it is already loaded - just handle them
            response = loginRules.getTableRefreshResponse(O2GTableType.OFFERS);
            onRequestCompleted(null, response);
        } else {
            // otherwise create the request to get offers from the server
            O2GRequestFactory factory = mSession.getRequestFactory();
            O2GRequest offerRequest = factory.createRefreshTableRequest(O2GTableType.OFFERS);
            mSession.sendRequest(offerRequest);
        }
    }

    /**
     * Notifies all subscribed listeners that the offer has changed
     */
    private void notifyPriceUpdate(IOffer offer) {
        List<IListener> listeners = new ArrayList<IListener>();
        synchronized (mListeners)  {
            listeners.addAll(mListeners);
        }

        for (IListener listener : listeners) {
            listener.onCollectionUpdate(offer);
        }
    }
}

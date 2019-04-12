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

import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.TimeZone;
import java.util.SimpleTimeZone;

import com.fxcore2.*;
import com.candleworks.pricehistorymgr.*;

import getliveprices.pricedata.PeriodCollection;
import common.*;

// NOTE: the example doesn't handle the session reconnecting event.
public class Main {

    static SimpleDateFormat mDateFormat = new SimpleDateFormat("MM.dd.yyyy HH:mm:ss");

    public static void main(String[] args) {
        mDateFormat.setTimeZone(new SimpleTimeZone(SimpleTimeZone.UTC_TIME, "UTC"));

        O2GSession session = null;
        IPriceHistoryCommunicator communicator = null;
        SessionStatusListener statusListener = null;
        boolean loggedIn = false;

        try {
            String sProcName = "GetLivePrices";
            if (args.length == 0) {
                printHelp(sProcName);
                return;
            }

            LoginParams loginParams = new LoginParams(args);

            SampleParams sampleParams = new SampleParams(args);
            printSampleParams(sProcName, loginParams, sampleParams);
            checkObligatoryParams(loginParams, sampleParams);

            // create the ForexConnect trading session
            session = O2GTransport.createSession();
            statusListener = new SessionStatusListener(session, loginParams.getSessionID(), loginParams.getPin());
            // subscribe IPriceHistoryCommunicatorStatusListener interface implementation for the status events
            session.subscribeSessionStatus(statusListener);
            statusListener.reset();

            // create an instance of IPriceHistoryCommunicator
            communicator = PriceHistoryCommunicatorFactory.createCommunicator(session, "History");

            // log in to ForexConnect
            session.login(loginParams.getLogin(), loginParams.getPassword(), loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                loggedIn = true;

                CommunicatorStatusListener communicatorStatusListener = new CommunicatorStatusListener();
                communicator.addStatusListener(communicatorStatusListener);

                // wait until the communicator signals that it is ready
                if (communicator.isReady() || 
                    communicatorStatusListener.waitEvents() && communicatorStatusListener.isReady()) {
                    // attach the instance of the class that implements the IPriceHistoryCommunicatorListener
                    // interface to the communicator
                    ResponseListener responseListener = new ResponseListener();
                    communicator.addListener(responseListener);

                    getLivePrices(communicator, sampleParams.getInstrument(),
                        sampleParams.getTimeframe(), sampleParams.getDateFrom(),
                        sampleParams.getDateTo(), sampleParams.getQuotesCount(), responseListener,
                        session, statusListener);
                    System.out.println("Done!");

                    communicator.removeListener(responseListener);
                }

                communicator.removeStatusListener(communicatorStatusListener);
            }

        } catch (Exception e) {
            System.out.println("Exception: " + e.toString());
        } finally {
            if (communicator != null) {
                communicator.dispose();
            }
            if (session != null) {
                if (loggedIn) {
                    try {
                        statusListener.reset();
                        session.logout();
                        statusListener.waitEvents();
                    } catch (Exception ee) {
                    }
                }

                session.unsubscribeSessionStatus(statusListener);
                session.dispose();
            }
        }
    }

    /** 
     * Request historical prices for the specified timeframe of the specified period and then show live prices.
     */
    public static void getLivePrices(IPriceHistoryCommunicator communicator, String instrument,
        String timeframe, Calendar from, Calendar to, int quotesCount,
        ResponseListener responseListener, O2GSession session, SessionStatusListener sessionListener)
        throws Exception {
        if (!communicator.isReady()) {
            System.out.println("History communicator is not ready.");
            return;
        }

        // create timeframe entity
        ITimeframeFactory timeframeFactory = communicator.getTimeframeFactory();
        O2GTimeframe timeframeObj = timeframeFactory.create(timeframe);

        // check timeframe for ticks
        if (O2GTimeframeUnit.TICK == timeframeObj.getUnit()) {
            throw new Exception("Application works only for bars. Don't use tick as timeframe.");
        }

        // load Offers table and start ticks listening
        PriceUpdateController priceUpdateController = new PriceUpdateController(session, instrument);
        if (!priceUpdateController.waitEvents()) {
            return;
        }

        // create period collection
        boolean alive = true;
        PeriodCollection periods = new PeriodCollection(instrument, timeframe, alive,
            priceUpdateController);

        PeriodCollectionUpdateObserver livePriceViewer = new PeriodCollectionUpdateObserver(periods);

        // create and send a history request
        IPriceHistoryCommunicatorRequest request = communicator.createRequest(instrument,
            timeframeObj, from, to, quotesCount);
        responseListener.setRequest(request);
        communicator.sendRequest(request);

        // wait results
        responseListener.waitEvents();

        IPriceHistoryCommunicatorResponse response = responseListener.getResponse();
        if (response != null) {
            O2GMarketDataSnapshotResponseReader reader = communicator.createResponseReader(response);
            processHistoricalPrices(communicator, response, reader, periods);

            // finally notify the collection that all bars are added, so it can
            // add all ticks collected while the request was being executed
            // and start update the data by forthcoming ticks
            periods.finish(reader.getLastBarTime(), reader.getLastBarVolume());

            // continue update the data until cancelled by a user
            System.out.println("\nPress ENTER to cancel. \n");
            System.in.read();
        }

        livePriceViewer.unsubscribe();
        priceUpdateController.unsubscribe();
    }

    /** 
     * Print history data from response and fills periods collection.
     */
    public static void processHistoricalPrices(IPriceHistoryCommunicator communicator,
        IPriceHistoryCommunicatorResponse response, O2GMarketDataSnapshotResponseReader reader,
        PeriodCollection periods) throws PriceHistoryError, Exception {
        // use O2GMarketDataSnapshotResponseReader to extract price data from the response object
        for (int i = 0; i < reader.size(); i++) {
            if (reader.isBar()) {
                periods.add(reader.getDate(i), reader.getBidOpen(i), reader.getBidHigh(i),
                    reader.getBidLow(i), reader.getBidClose(i), reader.getAskOpen(i),
                    reader.getAskHigh(i), reader.getAskLow(i), reader.getAskClose(i),
                    reader.getVolume(i));

                System.out.println(String.format("DateTime=%s, BidOpen=%s, BidHigh=%s, BidLow=%s, BidClose=%s, AskOpen=%s, AskHigh=%s, AskLow=%s, AskClose=%s, Volume=%s",
                    mDateFormat.format(reader.getDate(i).getTime()), reader.getBidOpen(i), reader.getBidHigh(i), reader.getBidLow(i), reader.getBidClose(i),
                    reader.getAskOpen(i), reader.getAskHigh(i), reader.getAskLow(i), reader.getAskClose(i), reader.getVolume(i)));
            }
        }
    }
    
    private static void printHelp(String sProcName)
    {
        System.out.println(sProcName + " sample parameters:\n");

        System.out.println("/login | --login | /l | -l");
        System.out.println("Your user name.\n");

        System.out.println("/password | --password | /p | -p");
        System.out.println("Your password.\n");

        System.out.println("/url | --url | /u | -u");
        System.out.println("The server URL. For example, http://www.fxcorporate.com/Hosts.jsp.\n");

        System.out.println("/connection | --connection | /c | -c");
        System.out.println("The connection name. For example, \"Demo\" or \"Real\".\n");

        System.out.println("/sessionid | --sessionid ");
        System.out.println("The database name. Required only for users who have accounts in more than one database. Optional parameter.\n");

        System.out.println("/pin | --pin ");
        System.out.println("Your pin code. Required only for users who have a pin. Optional parameter.\n");

        System.out.println("/instrument | --instrument | /i | -i");
        System.out.println("An instrument which you want to use in sample. For example, \"EUR/USD\".\n");

        System.out.println("/timeframe | --timeframe ");
        System.out.println("Time period which forms a single candle. For example, m1 - for 1 minute, H1 - for 1 hour.\n");

        System.out.println("/datefrom | --datefrom ");
        System.out.println("Date/time from which you want to receive historical prices. If you leave this argument as it is, it will mean from last trading day. Format is \"m.d.Y H:M:S\". Optional parameter.\n");

        System.out.println("/count | --count ");
        System.out.println("Count of historical prices you want to receive. If you leave this argument as it is, it will mean -1 (use some default value or ignore if datefrom is specified)");
    }

    /**
     * Check obligatory login parameters and sample parameters.
     */
    private static void checkObligatoryParams(LoginParams loginParams, SampleParams sampleParams) throws Exception {
        if (loginParams.getLogin().isEmpty()) {
            throw new Exception(LoginParams.LOGIN_NOT_SPECIFIED);
        }
        if (loginParams.getPassword().isEmpty()) {
            throw new Exception(LoginParams.PASSWORD_NOT_SPECIFIED);
        }
        if (loginParams.getURL().isEmpty()) {
            throw new Exception(LoginParams.URL_NOT_SPECIFIED);
        }
        if (loginParams.getConnection().isEmpty()) {
            throw new Exception(LoginParams.CONNECTION_NOT_SPECIFIED);
        }
        if (sampleParams.getInstrument().isEmpty()) {
            throw new Exception(SampleParams.INSTRUMENT_NOT_SPECIFIED);
        }
        if (sampleParams.getTimeframe().isEmpty()) {
            throw new Exception(SampleParams.TIMEFRAME_NOT_SPECIFIED);
        }

        boolean bIsDateFromNotSpecified = false;
        boolean bIsDateToNotSpecified = false;
        Calendar dtFrom = sampleParams.getDateFrom();
        Calendar dtTo = sampleParams.getDateTo();
        if (dtFrom == null) {
            bIsDateFromNotSpecified = true;
        } else {
            if (!dtFrom.before(Calendar.getInstance(TimeZone.getTimeZone("UTC")))) {
                DateFormat df = new SimpleDateFormat("MM.dd.yyyy HH:mm:ss");
                df.setTimeZone(new SimpleTimeZone(SimpleTimeZone.UTC_TIME, "UTC"));
                throw new Exception(String.format("Sorry, \"DateFrom\" value %s should be in the past", df.format(dtFrom.getTime())));
            }
        }
        if (dtTo == null) {
            bIsDateToNotSpecified = true;
        } else {
            if (!bIsDateFromNotSpecified && !dtFrom.before(dtTo)) {
                DateFormat df = new SimpleDateFormat("MM.dd.yyyy HH:mm:ss");
                df.setTimeZone(new SimpleTimeZone(SimpleTimeZone.UTC_TIME, "UTC"));
                throw new Exception(String.format("Sorry, \"DateTo\" value %s should be later than \"DateFrom\" value %s",
                    df.format(dtTo.getTime()), df.format(dtFrom.getTime())));
            }
        }
    }

    /**
     * Print process name and sample parameters.
     */
    private static void printSampleParams(String procName,
            LoginParams loginPrm, SampleParams prm) {
        System.out.println(String.format("Running %s with arguments:", procName));
        if (loginPrm != null) {
            System.out.println(String.format("%s * %s %s %s %s", loginPrm.getLogin(), loginPrm.getURL(),
                loginPrm.getConnection(), loginPrm.getSessionID(), loginPrm.getPin()));
        }
        if (prm != null) {
            System.out.println(String.format("Instrument='%s', Timeframe='%s', DateFrom='%s', DateTo='%s', QuotesCount='%d'",
                prm.getInstrument(), prm.getTimeframe(),
                prm.getDateFrom() == null ? "" : mDateFormat.format(prm.getDateFrom().getTime()),
                prm.getDateTo() == null ? "" : mDateFormat.format(prm.getDateTo().getTime()),
                prm.getQuotesCount()));
        }
    }
}

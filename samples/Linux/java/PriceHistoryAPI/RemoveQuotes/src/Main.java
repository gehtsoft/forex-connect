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
package removequotes;

import java.text.DecimalFormat;
import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Collection;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Queue;

import com.fxcore2.*;
import com.candleworks.pricehistorymgr.*;
import com.candleworks.quotesmgr.*;

import common.*;

class Main {

    public static void main(String[] args) {
        O2GSession session = null;        
        IPriceHistoryCommunicator communicator = null;
        SessionStatusListener statusListener = null;
        boolean loggedIn = false;

        try {
            String sProcName = "RemoveQuotes";
            if (args.length < 2) {
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
                    QuotesManager quotesManager = communicator.getQuotesManager();
                    removeQuotes(quotesManager, sampleParams);
                    System.out.println();
                    showLocalQuotes(quotesManager);
                }

                communicator.removeStatusListener(communicatorStatusListener);
            }


        }
        catch (Exception e) {
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
     * Remove quotes.
     * 
     * @param quotesManager
     *            The QuotesManager instance
     * @param sampleParams
     *            Parameters of remove command
     */
    static void removeQuotes(QuotesManager quotesManager, SampleParams sampleParams)
        throws QuotesManagerError {

        if (sampleParams.getYear() <= 0){
           System.out.println("Error : year must be an integer value.");
           return;
        }

        try {
            removeLocalQuotes(quotesManager, sampleParams.getInstrument(), sampleParams.getYear());
        } catch (Exception e) {
            System.out.println("Error : " + e.getMessage());
            return;
        }
    }

    /**
     * Removes quotes from local cache.
     * 
     * @param quotesManager
     *            The QuotesManager instance
     * @param instrument
     *            Instrument name
     * @param year
     *            Year
     * @throws Exception 
     */
    static void removeLocalQuotes(QuotesManager quotesManager, String instrument, int year)
        throws Exception {
        if (instrument == null) {
            throw new IllegalArgumentException("instrument");
        }

        if (quotesManager == null) {
            System.out.println("Failed to remove local quotes");
            return;
        }

        Collection<IQMData> quotes = null;
        try {
            quotes = getQuotes(quotesManager, instrument, year);
        } catch (Exception error) {
            System.out.println("Failed to remove local quotes : " + error.getMessage());
            return;
        }

        if (quotes == null || quotes.isEmpty()) {
            System.out.println("There are no quotes to remove");
            return;
        }

        removeData(quotesManager, quotes);
    }

    /**
     * Fills QuotesManager data for the specified instrument and year.
     * 
     * @param quotesManager
     *            QuotesManager instance
     * @param instrument
     *            Instrument name
     * @param year
     *            Year
     * @return Collection of quotes manager storage data
     */
    static Collection<IQMData> getQuotes(QuotesManager quotesManager, String instrument, int year)
        throws QuotesManagerError {
        BaseTimeframes baseTimeframes = quotesManager.getBaseTimeframes();
        int timeframesCount = baseTimeframes.size();
        List<IQMData> quotes = new ArrayList<IQMData>(timeframesCount);

        for (int i = 0; i < timeframesCount; ++i) {
            String timeframe = baseTimeframes.get(i);
            long size = quotesManager.getDataSize(instrument, timeframe, year);
            if (size > 0) {
                quotes.add(new QMData(instrument, timeframe, year, size));
            }
        }

        return quotes;
    }

    /**
     * Shows quotes that are available in local cache.
     * 
     * @param quotesManager
     *            QuotesManager instance
     * @throws Exception
     */
    static void showLocalQuotes(QuotesManager quotesManager) throws Exception {
        if (quotesManager == null) {
            System.out.println("Failed to get local quotes");
            return;
        }

        // if the instruments list is not updated, update it now
        if (!quotesManager.areInstrumentsUpdated()) {
            UpdateInstrumentsListener instrumentsCallback = new UpdateInstrumentsListener();
            UpdateInstrumentsTask task = quotesManager
                .createUpdateInstrumentsTask(instrumentsCallback);
            quotesManager.executeTask(task);
            instrumentsCallback.waitEvents();
        }

        IQMDataCollection collection = prepareListOfQMData(quotesManager);
        showPreparedQMDataList(collection);
    }

    /**
     * Prepares the collection of the quotes stored in the Quotes Manager cache.
     * 
     * @param quotesManager
     *            QuotesManager instance
     */
    public static IQMDataCollection prepareListOfQMData(QuotesManager quotesManager)
        throws QuotesManagerError {
        QMDataCollection collection = new QMDataCollection();
        BaseTimeframes timeframes = quotesManager.getBaseTimeframes();
        Instruments instruments = quotesManager.getInstruments();

        int instrumentCount = instruments.size();
        for (int i = 0; i < instrumentCount; i++) {
            Instrument instrument = instruments.get(i);
            int timeframeCount = timeframes.size();
            for (int j = 0; j < timeframeCount; j++) {
                String timeframe = timeframes.get(j);
                int y1 = instrument.getOldestQuoteDate(timeframe).get(Calendar.YEAR);
                int y2 = instrument.getLatestQuoteDate(timeframe).get(Calendar.YEAR);
                if (y2 >= y1) {
                    for (int y = y1; y <= y2; y++) {
                        long size = quotesManager.getDataSize(instrument.getName(), timeframe, y);
                        if (size > 0) {
                            collection.add(new QMData(instrument.getName(), timeframe, y, size));
                        }
                    }
                }
            }
        }

        return collection;
    }

    /**
     * Show list of available quotes.
     * 
     * @param collection
     *            Collection of quotes manager storage data
     */
    static void showPreparedQMDataList(IQMDataCollection collection) {
        Map<String, Map<Integer, Long>> instrumentsInfo = summariseInstrumentDataSize(collection);
        if (instrumentsInfo == null || instrumentsInfo.size() == 0) {
            System.out.println("There are no quotes in the local storage.");
        } else {
            System.out.println("Quotes in the local storage");

            for (String instrument : instrumentsInfo.keySet()) {
                Map<Integer, Long> sizeByYear = instrumentsInfo.get(instrument);
                for (int year : sizeByYear.keySet()) {
                    System.out.println("    " + instrument + " " + year + " "
                        + readableSize(sizeByYear.get(year)));
                }
            }
        }
    }

    /**
     * Get string suffix according to number size (KB, MB, GB).
     * 
     * @param bytes
     *            Number
     * @return String suffix
     */
    static String readableSize(long bytes) {
        final double byteConversion = 1024;
        int range = (int) (Math.log(bytes) / Math.log(byteConversion));
        DecimalFormat format = new DecimalFormat("#.##");
        
        switch (range) {
        case 3:
            return format.format(bytes / Math.pow(byteConversion, 3)) + " GB";
        case 2:
            return format.format(bytes / Math.pow(byteConversion, 2)) + " MB";
        case 1:
            return format.format(bytes / byteConversion) + " KB";
        default:
            return bytes + " Bytes";
        }
    }

    /**
     * Get full size of available instrument's quotes.
     * 
     * @param collection
     *            Collection of quotes manager storage data
     * @return Collection of info about quotes in starage
     */
    static Map<String, Map<Integer, Long>> summariseInstrumentDataSize(IQMDataCollection collection) {
        if (collection == null || collection.size() == 0) {
            return null;
        }

        Map<String, Map<Integer, Long>> instrumentsInfo = new HashMap<String, Map<Integer, Long>>();
        for (IQMData quote : collection) {
            String instrument = quote.getInstrument();
            if (!instrumentsInfo.containsKey(instrument)) {
                instrumentsInfo.put(instrument, new HashMap<Integer, Long>());
            }

            int year = quote.getYear();
            if (!instrumentsInfo.get(instrument).containsKey(year)) {
                instrumentsInfo.get(instrument).put(year, (long) 0);
            }
            long size = instrumentsInfo.get(instrument).get(year) + quote.getSize();
            instrumentsInfo.get(instrument).put(year, size);
        }

        return instrumentsInfo;
    }

    /**
     * Removes the data from cache.
     * 
     * @param quotesManager
     *            The QuotesManager instance
     * @param list
     *            Collection of quotes manager storage data.
     * @throws Exception 
     */
    public static void removeData(QuotesManager quotesManager, Collection<IQMData> list)
        throws Exception {
        Queue<RemoveQuotesTask> removeTasks = new ArrayDeque<RemoveQuotesTask>();
        RemoveQuotesListener removeQuotesListener = new RemoveQuotesListener();

        for (IQMData data : list) {
            RemoveQuotesTask task = quotesManager.createRemoveQuotesTask(data.getInstrument(),
                data.getTimeframe(), removeQuotesListener);
            task.addYear(data.getYear());
            removeTasks.add(task);
        }

        while (!removeTasks.isEmpty()) {
            startNextRemoveTask(quotesManager, removeTasks, removeQuotesListener);
        }
    }

    /**
     * Tries to start next remove task.
     * 
     * @param quotesManager
     *            The QuotesManager instance
     * @param removeTasks
     *            Collection of remove tasks
     * @param removeQuoteListener
     *            Listener of remove quotes task
     * @throws Exception 
     */
    private static void startNextRemoveTask(QuotesManager quotesManager,
        Queue<RemoveQuotesTask> removeTasks, RemoveQuotesListener removeQuoteListener)
        throws Exception {

        RemoveQuotesTask removeTask = removeTasks.poll();
        quotesManager.executeTask(removeTask);
        removeQuoteListener.waitEvents();
    }

    /**
     * Shows help information.
     */
    private static void showHelp() {
        System.out.println("Removes local quotes of some instrument for the specified year.");
        System.out.println("usage: RemoveQuotes <instrument> <year>");
        System.out.println("where:");
        System.out.println("  instrument is an instrument such as EUR/USD");
        System.out.println("  year is an year.");
        System.out.println();
    }

    /** Print expected sample-login parameters and their description.
     @param sProcName
                    The sample process name.
    */
    private static void printHelp(String sProcName)
    {
        System.out.println(sProcName + " : " + " Remove local quotes of some instrument for the specified year.\n");

        System.out.println("sample parameters:\n");

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

        System.out.println("/year | --year | /y | -y");
        System.out.println("The specific year. For example, \"2018\".\n");
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
        if (sampleParams.getYear() == -1) {
            throw new Exception(SampleParams.YEAR_NOT_SPECIFIED);
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
            System.out.println(String.format("Instrument='%s', Year='%d'",
                prm.getInstrument(), prm.getYear()));
        }
    }
}

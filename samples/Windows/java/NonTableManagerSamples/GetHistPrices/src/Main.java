package gethistprices;

import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.TimeZone;

import com.fxcore2.*;
import common.*;


public class Main {

    static SimpleDateFormat mDateFormat = new SimpleDateFormat("MM.dd.yyyy HH:mm:ss");
    
    public static void main(String[] args) {
        O2GSession session = null;
        try {
            String sProcName = "GetHistPrices";
            if (args.length == 0) {
                printHelp(sProcName);
                return;
            }

            LoginParams loginParams = new LoginParams(args);
            SampleParams sampleParams = new SampleParams(args);
            printSampleParams(sProcName, loginParams, sampleParams);
            checkObligatoryParams(loginParams, sampleParams);

            session = O2GTransport.createSession();
            SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.getSessionID(), loginParams.getPin());
            session.subscribeSessionStatus(statusListener);
            statusListener.reset();
            session.login(loginParams.getLogin(), loginParams.getPassword(), loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                ResponseListener responseListener = new ResponseListener();
                session.subscribeResponse(responseListener);
                getHistoryPrices(session, sampleParams.getInstrument(), sampleParams.getTimeframe(), sampleParams.getDateFrom(), sampleParams.getDateTo(), responseListener);
                System.out.println("Done!");

                statusListener.reset();
                session.logout();
                statusListener.waitEvents();
                session.unsubscribeResponse(responseListener);
            }
            session.unsubscribeSessionStatus(statusListener);
        } catch (Exception e) {
            System.out.println("Exception: " + e.toString());
        } finally {
            if (session != null) {
                session.dispose();
            }
        }
    }
    
    // Request historical prices for the specified timeframe of the specified period
    public static void getHistoryPrices(O2GSession session, String sInstrument, String sTimeframe, Calendar dtFrom, Calendar dtTo, ResponseListener responseListener) throws Exception {
        O2GRequestFactory factory = session.getRequestFactory();
        O2GTimeframe timeframe = factory.getTimeFrameCollection().get(sTimeframe);
        if (timeframe == null) {
            throw new Exception(String.format("Timeframe '%s' is incorrect!", sTimeframe));
        }
        O2GRequest request = factory.createMarketDataSnapshotRequestInstrument(sInstrument, timeframe, 300);
        if (request == null)
        {
            throw new Exception("Could not create request.");
        }
        Calendar dtFirst = dtTo;
        Calendar dtEarliest;
        if (dtFrom == null) {
            dtEarliest = Calendar.getInstance();
            dtEarliest.setTime(new Date(Long.MIN_VALUE));
        } else {
            dtEarliest = dtFrom;
        }
        do { // cause there is limit for returned candles amount
            factory.fillMarketDataSnapshotRequestTime(request, dtFrom, dtFirst, false, O2GCandleOpenPriceMode.PREVIOUS_CLOSE);
            responseListener.setRequestID(request.getRequestId());
            session.sendRequest(request);
            if (!responseListener.waitEvents()) {
                throw new Exception("Response waiting timeout expired");
            }
            // shift "to" bound to oldest datetime of returned data
            O2GResponse response = responseListener.getResponse();
            if (response != null && response.getType() == O2GResponseType.MARKET_DATA_SNAPSHOT) {
                O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
                if (readerFactory != null) {
                    O2GMarketDataSnapshotResponseReader reader = readerFactory.createMarketDataSnapshotReader(response);
                    if (reader.size() > 0) {
                        if (dtFirst == null || dtFirst.compareTo(reader.getDate(0)) != 0) {
                            dtFirst = reader.getDate(0); // earliest datetime of returned data
                        } else {
                            break;
                        }
                    } else {
                        System.out.println("0 rows received");
                        break;
                    }
                }
                printPrices(session, response);
            } else {
                break;
            }
        } while (dtFirst.after(dtEarliest));
    }

    // Print history data from response
    public static void printPrices(O2GSession session, O2GResponse response) {
        System.out.println(String.format("Request with RequestID=%s is completed:", response.getRequestId()));
        O2GResponseReaderFactory factory = session.getResponseReaderFactory();
        if (factory != null) {
            O2GMarketDataSnapshotResponseReader reader = factory.createMarketDataSnapshotReader(response);
            for (int ii = reader.size() - 1; ii >= 0; ii--) {
                if (reader.isBar()) {
                    System.out.println(String.format("DateTime=%s, BidOpen=%s, BidHigh=%s, BidLow=%s, BidClose=%s, AskOpen=%s, AskHigh=%s, AskLow=%s, AskClose=%s, Volume=%s",
                            mDateFormat.format(reader.getDate(ii).getTime()), reader.getBidOpen(ii), reader.getBidHigh(ii), reader.getBidLow(ii), reader.getBidClose(ii),
                            reader.getAskOpen(ii), reader.getAskHigh(ii), reader.getAskLow(ii), reader.getAskClose(ii), reader.getVolume(ii)));
                } else {
                    System.out.println(String.format("DateTime=%s, Bid=%s, Ask=%s", mDateFormat.format(reader.getDate(ii).getTime()), reader.getBidClose(ii), reader.getAskClose(ii)));
                }
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
        
        System.out.println("/dateto | --dateto ");
        System.out.println("Datetime until which you want to receive historical prices. If you leave this argument as it is, it will mean to now. Format is \"m.d.Y H:M:S\". Optional parameter.");
    }
    
    // Check obligatory login parameters and sample parameters
    private static void checkObligatoryParams(LoginParams loginParams, SampleParams sampleParams) throws Exception {
        if(loginParams.getLogin().isEmpty()) {
            throw new Exception(LoginParams.LOGIN_NOT_SPECIFIED);
        }
        if(loginParams.getPassword().isEmpty()) {
            throw new Exception(LoginParams.PASSWORD_NOT_SPECIFIED);
        }
        if(loginParams.getURL().isEmpty()) {
            throw new Exception(LoginParams.URL_NOT_SPECIFIED);
        }
        if(loginParams.getConnection().isEmpty()) {
            throw new Exception(LoginParams.CONNECTION_NOT_SPECIFIED);
        }
        if(sampleParams.getInstrument().isEmpty()) {
            throw new Exception(SampleParams.INSTRUMENT_NOT_SPECIFIED);
        }
        if(sampleParams.getTimeframe().isEmpty()) {
            throw new Exception(SampleParams.TIMEFRAME_NOT_SPECIFIED);
        }

        boolean bIsDateFromNotSpecified = false;
        boolean bIsDateToNotSpecified = false;
        Calendar dtFrom = sampleParams.getDateFrom();
        Calendar dtTo = sampleParams.getDateTo();
        if (dtFrom == null) {
            bIsDateFromNotSpecified = true;
        } else {
            if(!dtFrom.before(Calendar.getInstance(TimeZone.getTimeZone("UTC")))) {
                throw new Exception(String.format("\"DateFrom\" value %s is invalid", dtFrom));
            }
        }
        if (dtTo == null) {
            bIsDateToNotSpecified = true;
        } else {
            if(!bIsDateFromNotSpecified && !dtFrom.before(dtTo)) {
                throw new Exception(String.format("\"DateTo\" value %s is invalid", dtTo));
            }
        }
    }

    // Print process name and sample parameters
    private static void printSampleParams(String procName,
            LoginParams loginPrm, SampleParams prm) {
        System.out.println(String.format("Running %s with arguments:", procName));
        if (loginPrm != null) {
            System.out.println(String.format("%s * %s %s %s %s", loginPrm.getLogin(), loginPrm.getURL(),
                  loginPrm.getConnection(), loginPrm.getSessionID(), loginPrm.getPin()));
        }
        if (prm != null) {
            System.out.println(String.format("Instrument='%s', Timeframe='%s', DateFrom='%s', DateTo='%s'",
                    prm.getInstrument(), prm.getTimeframe(),
                    prm.getDateFrom() == null ? "" : mDateFormat.format(prm.getDateFrom().getTime()),
                    prm.getDateTo() == null ? "" : mDateFormat.format(prm.getDateTo().getTime())));
        }
    }
}
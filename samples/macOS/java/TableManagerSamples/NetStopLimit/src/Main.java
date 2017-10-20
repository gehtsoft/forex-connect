package netstoplimit;

import com.fxcore2.*;
import common.*;

public class Main {
    public static void main(String[] args) throws InterruptedException {
        O2GSession session = null;
        try {
            String sProcName = "NetStopLimit";
            if (args.length == 0) {
                printHelp(sProcName);
                return;
            }

            LoginParams loginParams = new LoginParams(args);
            SampleParams sampleParams = new SampleParams(args);
            printSampleParams(sProcName, loginParams, sampleParams);
            checkObligatoryParams(loginParams, sampleParams);

            session = O2GTransport.createSession();
            session.useTableManager(O2GTableManagerMode.YES, null);
            SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.getSessionID(), loginParams.getPin());
            session.subscribeSessionStatus(statusListener);
            statusListener.reset();
            session.login(loginParams.getLogin(), loginParams.getPassword(), loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                ResponseListener responseListener = new ResponseListener();
                TableListener tableListener = new TableListener(responseListener);
                session.subscribeResponse(responseListener);

                O2GTableManager tableManager = session.getTableManager();
                O2GTableManagerStatus managerStatus = tableManager.getStatus();
                while (managerStatus == O2GTableManagerStatus.TABLES_LOADING) {
                    Thread.sleep(50);
                    managerStatus = tableManager.getStatus();
                }

                if (managerStatus == O2GTableManagerStatus.TABLES_LOAD_FAILED) {
                    throw new Exception("Cannot refresh all tables of table manager");
                }
                O2GAccountRow account = getAccount(tableManager, sampleParams.getAccountID());
                if (account == null) {
                    if (sampleParams.getAccountID().isEmpty()) {
                        throw new Exception("No valid accounts");
                    } else {
                        throw new Exception(String.format("The account '%s' is not valid", sampleParams.getAccountID()));
                    }
                } else {
                    if(!sampleParams.getAccountID().equals(account.getAccountID())) {
                        sampleParams.setAccountID(account.getAccountID());
                        System.out.println(String.format("AccountID='%s'",
                                sampleParams.getAccountID()));
                    }
                }

                O2GOfferRow offer = getOffer(tableManager, sampleParams.getInstrument());
                if (offer == null) {
                    throw new Exception(String.format("The instrument '%s' is not valid", sampleParams.getInstrument()));
                }

                O2GTradeRow trade = getTrade(tableManager, sampleParams.getAccountID(), offer.getOfferID());
                if (trade == null) {
                    throw new Exception(String.format("There are no opened positions for instrument '%s'", sampleParams.getInstrument()));
                }

                O2GRequest request;
                String sBuySell = trade.getBuySell().equals(Constants.Buy) ? Constants.Sell : Constants.Buy;

                tableListener.subscribeEvents(tableManager);

                request = createNetEntryOrderRequest(session, offer.getOfferID(), sampleParams.getAccountID(), sampleParams.getRateStop(), sBuySell, Constants.Orders.StopEntry);
                if (request == null) {
                    throw new Exception("Cannot create request");
                }
                responseListener.setRequestID(request.getRequestId());
                tableListener.setRequestID(request.getRequestId());
                session.sendRequest(request);
                if (!responseListener.waitEvents()) {
                    throw new Exception("Response waiting timeout expired");
                }

                request = createNetEntryOrderRequest(session, offer.getOfferID(), sampleParams.getAccountID(), sampleParams.getRateLimit(), sBuySell, Constants.Orders.LimitEntry);
                if (request == null) {
                    throw new Exception("Cannot create request");
                }
                responseListener.setRequestID(request.getRequestId());
                tableListener.setRequestID(request.getRequestId());
                session.sendRequest(request);
                if (!responseListener.waitEvents()) {
                    throw new Exception("Response waiting timeout expired");
                }

                System.out.println("Done!");

                tableListener.unsubscribeEvents(tableManager);

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

    // Find valid account
    private static O2GAccountRow getAccount(O2GTableManager tableManager, String sAccountID) {
        boolean bHasAccount = false;
        O2GAccountRow account = null;
        O2GAccountsTable accountsTable = (O2GAccountsTable)tableManager.getTable(O2GTableType.ACCOUNTS);
        for (int i = 0; i < accountsTable.size(); i++) {
            account = accountsTable.getRow(i);
            String sAccountKind = account.getAccountKind();
            if (sAccountKind.equals("32") || sAccountKind.equals("36")) {
                if (account.getMarginCallFlag().equals("N")) {
                    if (sAccountID.isEmpty() || sAccountID.equals(account.getAccountID())) {
                        bHasAccount = true;
                        break;
                    }
                }
            }
        }
        if (!bHasAccount) {
            return null;
        } else {
            return account;
        }
    }

    // Find valid offer by instrument name
    private static O2GOfferRow getOffer(O2GTableManager tableManager, String sInstrument) {
        boolean bHasOffer = false;
        O2GOfferRow offer = null;
        O2GOffersTable offersTable = (O2GOffersTable)tableManager.getTable(O2GTableType.OFFERS);
        for (int i = 0; i < offersTable.size(); i++) {
            offer = offersTable.getRow(i);
            if (offer.getInstrument().equals(sInstrument)) {
                if (offer.getSubscriptionStatus().equals("T")) {
                    bHasOffer = true;
                    break;
                }
            }
        }
        if (!bHasOffer) {
            return null;
        } else {
            return offer;
        }
    }

    // Find the first opened position by AccountID and OfferID
    private static O2GTradeRow getTrade(O2GTableManager tableManager, String sAccountID, String sOfferID) {
        boolean bHasTrade = false;
        O2GTradeRow trade = null;
        O2GTradesTable tradesTable = (O2GTradesTable)tableManager.getTable(O2GTableType.TRADES);
        for (int i = 0; i < tradesTable.size(); i++) {
            trade = tradesTable.getRow(i);
            if (trade.getAccountID().equals(sAccountID) && trade.getOfferID().equals(sOfferID)) {
                bHasTrade = true;
                break;
            }
        }
        if (!bHasTrade) {
            return null;
        } else {
            return trade;
        }
    }

    // Create entry order request
    private static O2GRequest createNetEntryOrderRequest(O2GSession session, String sOfferID, String sAccountID, double dRate, String sBuySell, String sOrderType) throws Exception {
        O2GRequest request = null;
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GValueMap valuemap = requestFactory.createValueMap();
        valuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
        valuemap.setString(O2GRequestParamsEnum.ORDER_TYPE, sOrderType);
        valuemap.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
        valuemap.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
        valuemap.setString(O2GRequestParamsEnum.BUY_SELL, sBuySell);
        valuemap.setString(O2GRequestParamsEnum.NET_QUANTITY, "Y");
        valuemap.setDouble(O2GRequestParamsEnum.RATE, dRate);
        valuemap.setString(O2GRequestParamsEnum.CUSTOM_ID, "EntryOrder");
        request = requestFactory.createOrderRequest(valuemap);
        if (request == null) {
            System.out.println(requestFactory.getLastError());
        }
        return request;
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
        
        System.out.println("/account | --account ");
        System.out.println("An account which you want to use in sample. Optional parameter.\n");
        
        System.out.println("/ratestop | --ratestop ");
        System.out.println("Rate of the net stop order.\n");
        
        System.out.println("/ratelimit | --ratelimit ");
        System.out.println("Rate of the net limit order.\n");
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
        if (Double.isNaN(sampleParams.getRateStop())) {
            throw new Exception(SampleParams.RATESTOP_NOT_SPECIFIED);
        }
        if (Double.isNaN(sampleParams.getRateLimit())) {
            throw new Exception(SampleParams.RATELIMIT_NOT_SPECIFIED);
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
            System.out.println(String.format("Instrument='%s', RateStop='%s', RateLimit='%s', AccountID='%s'",
                    prm.getInstrument(), prm.getRateStop(), prm.getRateLimit(), prm.getAccountID()));
        }
    }
}
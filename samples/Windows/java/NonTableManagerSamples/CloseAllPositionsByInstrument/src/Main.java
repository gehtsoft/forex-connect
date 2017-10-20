package closeallpositionsbyinstrument;

import java.util.ArrayList;
import java.util.List;

import com.fxcore2.*;
import common.*;

public class Main {

    public static void main(String[] args) {
        O2GSession session = null;
        try {
            String sProcName = "CloseAllPositionsByInstrument";
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
            session.login(loginParams.getLogin(), loginParams.getPassword(),
                    loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                ResponseListener responseListener = new ResponseListener(session);
                session.subscribeResponse(responseListener);

                O2GAccountRow account = getAccount(session, sampleParams.getAccountID());
                if (account == null) {
                    if (sampleParams.getAccountID().isEmpty()) {
                        throw new Exception("No valid accounts");
                    } else {
                        throw new Exception(String.format("The account '%s' is not valid",
                                sampleParams.getAccountID()));
                    }
                } else {
                    if(!sampleParams.getAccountID().equals(account.getAccountID())) {
                        sampleParams.setAccountID(account.getAccountID());
                        System.out.println(String.format("AccountID='%s'",
                                sampleParams.getAccountID()));
                    }
                }

                O2GOfferRow offer = getOffer(session, sampleParams.getInstrument());
                if (offer == null) {
                    throw new Exception(String.format("The instrument '%s' is not valid",
                            sampleParams.getInstrument()));
                }

                CloseOrdersData closeOrdersData = getCloseOrdersData(session, sampleParams.getAccountID(), offer.getOfferID(), responseListener);
                if (closeOrdersData == null) {
                    throw new Exception("There are no opened positions");
                }

                O2GRequest request = createCloseMarketNettingOrderRequest(session, closeOrdersData);
                if (request == null) {
                    throw new Exception("Cannot create request");
                }
                
                List<String> requestIDs = new ArrayList<String>();
                for (int i = 0; i < request.getChildrenCount(); i++) {
                    requestIDs.add(request.getChildRequest(i).getRequestId());
                }
                responseListener.setRequestIDs(requestIDs);
                session.sendRequest(request);
                if (responseListener.waitEvents()) {
                    Thread.sleep(1000); // Wait for the balance update
                    System.out.println("Done!");
                } else {
                    throw new Exception("Response waiting timeout expired");
                }

                session.unsubscribeResponse(responseListener);
                statusListener.reset();
                session.logout();
                statusListener.waitEvents();
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

    // Find valid account by ID or get the first valid account
    private static O2GAccountRow getAccount(O2GSession session, String sAccountID) throws Exception {
        O2GAccountRow account = null;
        boolean bHasAccount = false;
        O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
        if (readerFactory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GLoginRules loginRules = session.getLoginRules();
        O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.ACCOUNTS);
        O2GAccountsTableResponseReader accountsResponseReader = readerFactory.createAccountsTableReader(response);
        for (int i = 0; i < accountsResponseReader.size(); i++) {
            account = accountsResponseReader.getRow(i);
            String sAccountKind = account.getAccountKind();
            if (!account.getMaintenanceType().equals("0")) // not netting account
            {
                if (sAccountKind.equals("32") || sAccountKind.equals("36")) {
                    if (account.getMarginCallFlag().equals("N")) {
                        if (sAccountID.isEmpty() || sAccountID.equals(account.getAccountID())) {
                            bHasAccount = true;
                            break;
                        }
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
    private static O2GOfferRow getOffer(O2GSession session, String sInstrument) throws Exception {
        O2GOfferRow offer = null;
        boolean bHasOffer = false;
        O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
        if (readerFactory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GLoginRules loginRules = session.getLoginRules();
        O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.OFFERS);
        O2GOffersTableResponseReader offersResponseReader = readerFactory.createOffersTableReader(response);
        for (int i = 0; i < offersResponseReader.size(); i++) {
            offer = offersResponseReader.getRow(i);
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

    private static CloseOrdersData getCloseOrdersData(O2GSession session, String sAccountID, String sOfferID, ResponseListener responseListener) throws Exception {
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.TRADES, sAccountID);
        responseListener.setRequestID(request.getRequestId());
        session.sendRequest(request);
        if (!responseListener.waitEvents()) {
            throw new Exception("Response waiting timeout expired");
        }
        O2GResponse response = responseListener.getResponse();
        CloseOrdersData closeOrdersData = null;
        if (response != null) {
            O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
            if (readerFactory != null) {
                O2GTradesTableResponseReader tradesResponseReader = readerFactory.createTradesTableReader(response);
                for (int i = 0; i < tradesResponseReader.size(); i++) {
                    O2GTradeRow trade = tradesResponseReader.getRow(i);
                    if (!trade.getOfferID().equals(sOfferID)) {
                        continue;
                    }
                    if (closeOrdersData == null) {
                        closeOrdersData = new CloseOrdersData();
                    }
                    String sBuySell = trade.getBuySell();
                    // Set opposite side
                    OrderSide side = (sBuySell.equals(Constants.Buy) ? OrderSide.Sell : OrderSide.Buy);
                    if (closeOrdersData.OfferID.equals(sOfferID)) {
                        OrderSide currentSide = closeOrdersData.Side;
                        if (currentSide != OrderSide.Both && currentSide != side) {
                            closeOrdersData.Side = OrderSide.Both;
                        }
                    } else {
                        closeOrdersData.OfferID = sOfferID;
                        closeOrdersData.AccountID = sAccountID;
                        closeOrdersData.Side = side;
                    }
                }
            }
        }
        return closeOrdersData;
    }

    // Create close all positions by instrument request
    private static O2GRequest createCloseMarketNettingOrderRequest(O2GSession session, CloseOrdersData closeOrdersData) throws Exception {
        O2GRequest request = null;
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }

        O2GValueMap batchValuemap = requestFactory.createValueMap();
        batchValuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);

        String sOfferID = closeOrdersData.OfferID;
        String sAccountID = closeOrdersData.AccountID;
        OrderSide side = closeOrdersData.Side;
        O2GValueMap childValuemap;
        switch (side) {
            case Buy:
                childValuemap = requestFactory.createValueMap();
                childValuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
                childValuemap.setString(O2GRequestParamsEnum.NET_QUANTITY, "Y");
                childValuemap.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.TrueMarketClose);
                childValuemap.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
                childValuemap.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
                childValuemap.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Buy);
                batchValuemap.appendChild(childValuemap);
                break;
            case Sell:
                childValuemap = requestFactory.createValueMap();
                childValuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
                childValuemap.setString(O2GRequestParamsEnum.NET_QUANTITY, "Y");
                childValuemap.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.TrueMarketClose);
                childValuemap.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
                childValuemap.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
                childValuemap.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Sell);
                batchValuemap.appendChild(childValuemap);
                break;
            case Both:
                childValuemap = requestFactory.createValueMap();
                childValuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
                childValuemap.setString(O2GRequestParamsEnum.NET_QUANTITY, "Y");
                childValuemap.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.TrueMarketClose);
                childValuemap.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
                childValuemap.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
                childValuemap.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Buy);
                batchValuemap.appendChild(childValuemap);

                childValuemap = requestFactory.createValueMap();
                childValuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
                childValuemap.setString(O2GRequestParamsEnum.NET_QUANTITY, "Y");
                childValuemap.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.TrueMarketClose);
                childValuemap.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
                childValuemap.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
                childValuemap.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Sell);
                batchValuemap.appendChild(childValuemap);
                break;
        }

        request = requestFactory.createOrderRequest(batchValuemap);
        if (request == null) {
            System.out.println(requestFactory.getLastError());
        }
        return request;
    }

    static class CloseOrdersData {
        public String AccountID;
        public String OfferID;
        public OrderSide Side;
        public CloseOrdersData() {
            AccountID = "";
            OfferID = "";
        }
    }

    enum OrderSide {
        Buy, Sell, Both
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
            System.out.println(String.format("Instrument='%s', AccountID='%s'",
                    prm.getInstrument(), prm.getAccountID()));
        }
    }
}
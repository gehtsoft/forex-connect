package closeallpositions;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;

import com.fxcore2.*;
import common.*;

public class Main {

    public static void main(String[] args) {
        O2GSession session = null;

        try {
            String sProcName = "CloseAllPositions";
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
            session.login(loginParams.getLogin(), loginParams.getPassword(),
                    loginParams.getURL(), loginParams.getConnection());
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

                Map<String, CloseOrdersData> closeOrdersData = getCloseOrdersData(tableManager, sampleParams.getAccountID());
                if (closeOrdersData.size() == 0) {
                    throw new Exception("There are no opened positions");
                }

                tableListener.subscribeEvents(tableManager);

                O2GRequest request = createCloseAllRequest(session, closeOrdersData);
                if (request == null) {
                    throw new Exception("Cannot create request");
                }
                List<String> requestIDs = new ArrayList<String>();
                for (int i = 0; i < request.getChildrenCount(); i++) {
                    requestIDs.add(request.getChildRequest(i).getRequestId());
                }
                responseListener.setRequestIDs(requestIDs);
                tableListener.setRequestIDs(requestIDs);
                session.sendRequest(request);
                if (responseListener.waitEvents()) {
                    Thread.sleep(1000); // Wait for the balance update
                    System.out.println("Done!");
                } else {
                    throw new Exception("Response waiting timeout expired");
                }

                tableListener.unsubscribeEvents(tableManager);

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

    // Get orders data for closing all positions
    private static Map<String, CloseOrdersData> getCloseOrdersData(O2GTableManager tableManager, String sAccountID) {
        Map<String, CloseOrdersData> closeOrdersData = new HashMap<String, CloseOrdersData>();
        O2GTradeRow trade = null;
        O2GTradesTable tradesTable = (O2GTradesTable)tableManager.getTable(O2GTableType.TRADES);
        for (int i = 0; i < tradesTable.size(); i++) {
            trade = tradesTable.getRow(i);
            String sOfferID = trade.getOfferID();
            String sBuySell = trade.getBuySell();
            // Set opposite side
            OrderSide side = (sBuySell.equals(Constants.Buy) ? OrderSide.Sell : OrderSide.Buy);

            if (closeOrdersData.containsKey(sOfferID)) {
                OrderSide currentSide = closeOrdersData.get(sOfferID).getSide();
                if (currentSide != OrderSide.Both && currentSide != side) {
                    closeOrdersData.get(sOfferID).setSide(OrderSide.Both);
                }
            } else {
                CloseOrdersData data = new CloseOrdersData(sAccountID, side);
                closeOrdersData.put(sOfferID, data);
            }
        }
        return closeOrdersData;
    }

    // Create close all order request
    private static O2GRequest createCloseAllRequest(O2GSession session, Map<String, CloseOrdersData> closeOrdersData) throws Exception {
        O2GRequest request = null;
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }

        O2GValueMap batchValuemap = requestFactory.createValueMap();
        batchValuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
        Iterator<Entry<String, CloseOrdersData>> iterator = closeOrdersData.entrySet().iterator();
        while (iterator.hasNext()) {
            Entry<String, CloseOrdersData> entry = iterator.next();
            String sOfferID = entry.getKey();
            String sAccountID = entry.getValue().getAccountID();
            OrderSide side = entry.getValue().getSide();
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
        }
        request = requestFactory.createOrderRequest(batchValuemap);
        if (request == null) {
            System.out.println(requestFactory.getLastError());
        }
        return request;
    }

    // Store the data to create netting close order per instrument
    static class CloseOrdersData {
        // ctor
        public CloseOrdersData(String sAccountID, OrderSide side) {
            mAccountID = sAccountID;
            mSide = side;
        }

        public String getAccountID() {
            return mAccountID;
        }
        private String mAccountID;

        public OrderSide getSide() {
            return mSide;
        }
        
        public void setSide(OrderSide side) {
            mSide = side;
        }
        private OrderSide mSide;
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
            System.out.println(String.format("AccountID='%s'",
                    prm.getAccountID()));
        }
    }
}
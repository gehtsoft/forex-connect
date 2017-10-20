package createoco;

import java.util.ArrayList;
import java.util.List;

import com.fxcore2.*;
import common.*;

public class Main {
    public static void main(String[] args) throws InterruptedException {
        O2GSession session = null;
        try {
            String sProcName = "CreateOCO";
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

                O2GLoginRules loginRules = session.getLoginRules();
                if (loginRules == null) {
                    throw new Exception("Cannot get login rules");
                }

                O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(sampleParams.getInstrument(), account);
                int iAmount = iBaseUnitSize * sampleParams.getLots();

                // For the purpose of this example we will place entry orders 30 pips from the current market price
                double dRateUp = offer.getAsk() + 30.0 * offer.getPointSize();
                double dRateDown = offer.getBid() - 30.0 * offer.getPointSize();

                tableListener.subscribeEvents(tableManager);

                O2GRequest request = createOCORequest(session, offer.getOfferID(), account.getAccountID(), iAmount, dRateUp, dRateDown);
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
                    System.out.println("Done!");
                } else {
                    throw new Exception("Response waiting timeout expired");
                }

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

    // Create OCO request
    private static O2GRequest createOCORequest(O2GSession session, String sOfferID, String sAccountID, int iAmount, double dRateUp, double dRateDown) throws Exception {
        O2GRequest request = null;
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GValueMap valuemapMain = requestFactory.createValueMap();
        valuemapMain.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOCO);

        // ValueMap for upswing
        O2GValueMap valuemapUp = requestFactory.createValueMap();
        valuemapUp.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
        valuemapUp.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.StopEntry);
        valuemapUp.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
        valuemapUp.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
        valuemapUp.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Buy);
        valuemapUp.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
        valuemapUp.setDouble(O2GRequestParamsEnum.RATE, dRateUp);
        valuemapMain.appendChild(valuemapUp);

        // ValueMap for downswing
        O2GValueMap valuemapDown = requestFactory.createValueMap();
        valuemapDown.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
        valuemapDown.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.StopEntry);
        valuemapDown.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
        valuemapDown.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
        valuemapDown.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Sell);
        valuemapDown.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
        valuemapDown.setDouble(O2GRequestParamsEnum.RATE, dRateDown);
        valuemapMain.appendChild(valuemapDown);

        request = requestFactory.createOrderRequest(valuemapMain);
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
        
        System.out.println("/lots | --lots ");
        System.out.println("Trade amount in lots. Optional parameter.\n");
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
            throw new Exception("'Instrument' is not specified. (/i|-i|/instrument|--instrument)");
        }
        if (sampleParams.getLots() <= 0) {
            throw new Exception(String.format("'Lots' value %s is invalid",
                    sampleParams.getLots()));
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
            System.out.println(String.format("Instrument='%s', Lots='%s', AccountID='%s'",
                    prm.getInstrument(), prm.getLots(), prm.getAccountID()));
        }
    }
}
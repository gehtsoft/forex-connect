package createotoco;

import java.util.ArrayList;
import java.util.List;

import com.fxcore2.*;
import common.*;

public class Main {
    public static void main(String[] args) {
        O2GSession session = null;
        try {
            String sProcName = "CreateOTOCO";
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
            session.login(loginParams.getLogin(), loginParams.getPassword(), loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                ResponseListener responseListener = new ResponseListener(session);
                session.subscribeResponse(responseListener);

                O2GAccountRow account = getAccount(session, sampleParams.getAccountID());
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

                O2GOfferRow offer = getOffer(session, sampleParams.getInstrument());
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

                double dRatePrimary = offer.getAsk() - 30.0 * offer.getPointSize();
                double dRateOcoFirst = offer.getAsk() + 15.0 * offer.getPointSize();
                double dRateOcoSecond = offer.getAsk() - 15.0 * offer.getPointSize();

                O2GRequest request = createOTOCORequest(session, offer.getOfferID(), account.getAccountID(), iAmount, dRatePrimary, dRateOcoFirst, dRateOcoSecond);                
                
                if (request == null) {
                    throw new Exception("Cannot create request");
                }

                List<String> requestIDs = new ArrayList<String>();
                fillRequestIDs(requestIDs, request);
               
                responseListener.setRequestIDs(requestIDs);
                session.sendRequest(request);
                if (responseListener.waitEvents()) {
                    System.out.println("Done!");
                } else {
                    throw new Exception("Response waiting timeout expired");
                }

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

    // Create OTOCO request  
    private static O2GRequest createOTOCORequest(O2GSession session, String sOfferID, String sAccountID, int iAmount, double dRatePrimary, double dRateOcoFirst, double dRateOcoSecond) throws Exception
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            // Create OTO command
            O2GValueMap valuemapMain = requestFactory.createValueMap();
            valuemapMain.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOTO);

            // Create Entry order
            O2GValueMap valuemapPrimary = requestFactory.createValueMap();
            valuemapPrimary.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
            valuemapPrimary.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.StopEntry);
            valuemapPrimary.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
            valuemapPrimary.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
            valuemapPrimary.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Sell);
            valuemapPrimary.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
            valuemapPrimary.setDouble(O2GRequestParamsEnum.RATE, dRatePrimary);

            // Create OCO group of orders
            O2GValueMap valuemapOCO = requestFactory.createValueMap();
            valuemapOCO.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOCO);

            // Create Entry order to OCO
            O2GValueMap valuemapOCOFirst = requestFactory.createValueMap();
            valuemapOCOFirst.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.StopEntry);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Buy);
            valuemapOCOFirst.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
            valuemapOCOFirst.setDouble(O2GRequestParamsEnum.RATE, dRateOcoFirst);

            O2GValueMap valuemapOCOSecond = requestFactory.createValueMap();
            valuemapOCOSecond.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.StopEntry);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.BUY_SELL, Constants.Sell);
            valuemapOCOSecond.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
            valuemapOCOSecond.setDouble(O2GRequestParamsEnum.RATE, dRateOcoSecond);

            // Fill the created groups. Please note, first you should add an entry order to OTO order and then OCO group of orders
            valuemapMain.appendChild(valuemapPrimary);
            valuemapOCO.appendChild(valuemapOCOFirst);
            valuemapOCO.appendChild(valuemapOCOSecond);
            valuemapMain.appendChild(valuemapOCO);

            request = requestFactory.createOrderRequest(valuemapMain);
            if (request == null)
            {
                System.out.println(requestFactory.getLastError());
            }
            return request;
        }

    private static void fillRequestIDs(List<String> requestIDs, O2GRequest request)
        {
            int childrenCount = request.getChildrenCount();
            if (childrenCount == 0)
            {
                requestIDs.add(request.getRequestId());
                return;
            }

            for (int i = 0; i < childrenCount; i++)
            {
                O2GRequest childRequest = request.getChildRequest(i);
                fillRequestIDs(requestIDs, childRequest);
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
            throw new Exception(SampleParams.BUYSELL_NOT_SPECIFIED);
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
package subscriptionstatus;

import com.fxcore2.*;
import common.*;

public class Main {
    
    public static void main(String[] args) {
        O2GSession session = null;

        try {
            String sProcName = "SubscriptionStatus";
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

                O2GRequest request = createSetSubscriptionStatusRequest(session, offer.getOfferID(), sampleParams.getStatus(), responseListener);
                if (request == null) {
                    throw new Exception("Cannot create request");
                }
                responseListener.setRequestID(request.getRequestId());
                session.sendRequest(request);
                if (!responseListener.waitEvents()) {
                    throw new Exception("Response waiting timeout expired");
                }

                O2GResponse response = responseListener.getResponse();
                if (response != null && response.getType() == O2GResponseType.COMMAND_RESPONSE) {
                    System.out.println(String.format("Subscription status for '%s' is set to '%s'",
                            sampleParams.getInstrument(), sampleParams.getStatus()));
                }

                printMargins(session, account, offer);
                updateMargins(session, responseListener);
                printMargins(session, account, offer);
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
    
    // Print offers and find offer by instrument name
    private static O2GOfferRow getOffer(O2GSession session, String sInstrument) throws Exception {
        O2GOfferRow offer = null;
        O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
        if (readerFactory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GLoginRules loginRules = session.getLoginRules();
        O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.OFFERS);
        O2GOffersTableResponseReader offersResponseReader = readerFactory.createOffersTableReader(response);

        for (int i = 0; i < offersResponseReader.size(); i++) {
            O2GOfferRow offerRow = offersResponseReader.getRow(i);
            if (offerRow.getInstrument().equals(sInstrument)) {
                offer = offerRow;
            }
            if (offerRow.getSubscriptionStatus().equals(Constants.SubscriptionStatuses.ViewOnly)) {
                System.out.println(String.format("%s : [V]iew only", offerRow.getInstrument()));
            } else if (offerRow.getSubscriptionStatus().equals(Constants.SubscriptionStatuses.Disable)) {
                System.out.println(String.format("%s : [D]isabled", offerRow.getInstrument()));
            } else if (offerRow.getSubscriptionStatus().equals(Constants.SubscriptionStatuses.Tradable)) {
                System.out.println(String.format("%s : Available for [T]rade", offerRow.getInstrument()));
            } else {
                System.out.println(String.format("%s : %s", offerRow.getInstrument(), offerRow.getSubscriptionStatus()));
            }
        }
        return offer;
    }

    // Subscribe or unsubscribe an instrument
    private static O2GRequest createSetSubscriptionStatusRequest(O2GSession session, String sOfferID, String sStatus, ResponseListener responseListener) throws Exception {
        O2GRequest request = null;
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GValueMap valueMap = requestFactory.createValueMap();
        valueMap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.SetSubscriptionStatus);
        valueMap.setString(O2GRequestParamsEnum.SUBSCRIPTION_STATUS, sStatus);
        valueMap.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
        request = requestFactory.createOrderRequest(valueMap);
        if (request == null) {
            System.out.println(requestFactory.getLastError());
        }
        return request;
    }

    // Get and print margin requirements
    private static void printMargins(O2GSession session, O2GAccountRow account, O2GOfferRow offer) throws Exception {
        O2GLoginRules loginRules = session.getLoginRules();
        if (loginRules == null) {
            throw new Exception("Cannot get login rules");
        }
        O2GTradingSettingsProvider tradingSettings = loginRules.getTradingSettingsProvider();
        double dMmr = 0D;
        double dEmr = 0D;
        double dLmr = 0D;
        O2GMargin margin = tradingSettings.getMargins(offer.getInstrument(), account);
        if (margin != null) {
            dMmr = margin.getMMR();
            dEmr = margin.getEMR();
            dLmr = margin.getLMR();
            System.out.println(String.format("Margin requirements: mmr=%s, emr=%s, lmr=%s", dMmr, dEmr, dLmr));
        }
    }

    // Update margin requirements
    private static void updateMargins(O2GSession session, ResponseListener responseListener) throws Exception {
        O2GRequest request = null;
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GValueMap valueMap = requestFactory.createValueMap();
        valueMap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.UpdateMarginRequirements);
        request = requestFactory.createOrderRequest(valueMap);
        responseListener.setRequestID(request.getRequestId());
        session.sendRequest(request);
        if (!responseListener.waitEvents()) {
            throw new Exception("Response waiting timeout expired");
        }
        O2GResponse response = responseListener.getResponse();
        if (response != null && response.getType() == O2GResponseType.MARGIN_REQUIREMENTS_RESPONSE) {
            O2GResponseReaderFactory responseFactory = session.getResponseReaderFactory();
            if (responseFactory != null) {
                responseFactory.processMarginRequirementsResponse(response);
                System.out.println("Margin requirements have been updated");
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
        
        System.out.println("/status | --status ");
        System.out.println("Desired subscription status of the instrument. Possible values: T, D, V.\n");
        
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
        if(sampleParams.getStatus().isEmpty()) {
            throw new Exception(SampleParams.STATUS_NOT_SPECIFIED);
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
            System.out.println(String.format("Instrument='%s', Status='%s', AccountID='%s'",
                    prm.getInstrument(), prm.getStatus(), prm.getAccountID()));
        }
    }
}
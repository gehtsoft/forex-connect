package ShowCommissionDetails;

import com.fxcore2.*;
import common.*;

public class Main {
    
    public static void main(String[] args) {
        O2GSession session = null;

        try {
            String sProcName = "ShowCommissionDetails";
            if (args.length == 0) {
                printHelp(sProcName);
                return;
            }

            LoginParams loginParams = new LoginParams(args);
            SampleParams sampleParams = new SampleParams(args);
            printSampleParams(sProcName, loginParams);
            checkObligatoryParams(loginParams);

            session = O2GTransport.createSession();
            SessionStatusListener statusListener = new SessionStatusListener(session);
            session.subscribeSessionStatus(statusListener);
            statusListener.reset();
            session.login(loginParams.getLogin(), loginParams.getPassword(), loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                PrintCommissionDetails(session, sampleParams);
                System.out.println("Done!");
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

    // Print accounts table
    private static void PrintCommissionDetails(O2GSession session, SampleParams sampleParams) throws Exception
    {
        //wait until commissions related information will be loaded
        O2GCommissionsProvider commissionProvider = session.getCommissionsProvider();
        while (commissionProvider.getStatus() == O2GCommissionStatusCode.LOADING) {
                    Thread.sleep(50);
                }

        if (commissionProvider.getStatus() != O2GCommissionStatusCode.READY)
                throw new Exception("Commissions are not supported or fail to load.");

        O2GAccountRow account = getAccount(session, sampleParams.getAccountID());
        if (account == null) 
             throw new Exception(sampleParams.getAccountID().isEmpty() ? "No valid accounts" : String.format("The account '%s' is not valid",
                                sampleParams.getAccountID()));

        O2GOfferRow offer = getOffer(session, sampleParams.getInstrument());
        if (offer == null) 
           throw new Exception(String.format("The instrument '%s' is not valid", sampleParams.getInstrument()));

        O2GCommissionDescriptionsCollection commList = commissionProvider.getCommissionDescriptions(offer.getOfferID(), account.getATPID());
        if (commList.size() == 0)
        {
            System.out.println("No commissions.");
            return;
        }
        System.out.println("Commissions:\nStage, Type, Value");            
        for (O2GCommissionDescription comm : commList) {
                System.out.println(String.format("'%s' '%s' '%s'", comm.getStage(), comm.getUnitType(), comm.getCommissionValue()));
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
            if (!account.getMaintenanceType().equals("0")) { // not netting account
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

        System.out.println("/instrument | --instrument | /i | -i");
        System.out.println("An instrument which you want to use in sample. For example, \"EUR/USD\".\n");
        
        System.out.println("/buysell | --buysell | /d | -d");
        System.out.println("The order direction. Possible values are: B - buy, S - sell.\n");
        
        System.out.println("/lots | --lots ");
        System.out.println("Trade amount in lots.\n");

        System.out.println("/account | --account ");
        System.out.println("An account which you want to use in sample.\n");
    }
    
    // Check obligatory login parameters and sample parameters
    private static void checkObligatoryParams(LoginParams loginParams) throws Exception {
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
            LoginParams loginPrm) {
        System.out.println(String.format("Running %s with arguments:", procName));
        if (loginPrm != null) {
            System.out.println(String.format("%s * %s %s", loginPrm.getLogin(), loginPrm.getURL(),
                  loginPrm.getConnection()));
        }
    }
}
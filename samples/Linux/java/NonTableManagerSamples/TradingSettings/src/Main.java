package tradingsettings;

import com.fxcore2.*;
import common.*;

public class Main {
    
    public static void main(String[] args) {
        O2GSession session = null;

        try {
            String sProcName = "TradingSettings";
            if (args.length == 0) {
                printHelp(sProcName);
                return;
            }

            LoginParams loginParams = new LoginParams(args);
            printSampleParams(sProcName, loginParams);
            checkObligatoryParams(loginParams);

            session = O2GTransport.createSession();
            SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.getSessionID(), loginParams.getPin());
            session.subscribeSessionStatus(statusListener);
            statusListener.reset();
            session.login(loginParams.getLogin(), loginParams.getPassword(), loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                PrintTradingSettings(session);
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

    // Print trading settings of the first account
    private static void PrintTradingSettings(O2GSession session) throws Exception {
        O2GLoginRules loginRules = session.getLoginRules();
        if (loginRules == null) {
            throw new Exception("Cannot get login rules");
        }
        O2GResponse accountsResponse = loginRules.getTableRefreshResponse(O2GTableType.ACCOUNTS);
        if (accountsResponse == null) {
            throw new Exception("Cannot get response");
        }
        O2GResponse offersResponse = loginRules.getTableRefreshResponse(O2GTableType.OFFERS);
        if (offersResponse == null) {
            throw new Exception("Cannot get response");
        }
        O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
        O2GResponseReaderFactory factory = session.getResponseReaderFactory();
        if (factory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GAccountsTableResponseReader accountsReader = factory.createAccountsTableReader(accountsResponse);
        O2GOffersTableResponseReader instrumentsReader = factory.createOffersTableReader(offersResponse);
        O2GAccountRow account = accountsReader.getRow(0);
        for (int i = 0; i < instrumentsReader.size(); i++) {
            O2GOfferRow instrumentRow = instrumentsReader.getRow(i);
            String instrument = instrumentRow.getInstrument();
            int condDistStopForTrade = tradingSettingsProvider.getCondDistStopForTrade(instrument);
            int condDistLimitForTrade = tradingSettingsProvider.getCondDistLimitForTrade(instrument);
            int condDistEntryStop = tradingSettingsProvider.getCondDistEntryStop(instrument);
            int condDistEntryLimit = tradingSettingsProvider.getCondDistEntryLimit(instrument);
            int minQuantity = tradingSettingsProvider.getMinQuantity(instrument, account);
            int maxQuantity = tradingSettingsProvider.getMaxQuantity(instrument, account);
            int baseUnitSize = tradingSettingsProvider.getBaseUnitSize(instrument, account);
            O2GMarketStatus marketStatus = tradingSettingsProvider.getMarketStatus(instrument);
            int minTrailingStep = tradingSettingsProvider.getMinTrailingStep();
            int maxTrailingStep = tradingSettingsProvider.getMaxTrailingStep();
            double mmr = tradingSettingsProvider.getMMR(instrument, account);
            double mmr2=0, emr=0, lmr=0;
            O2GMargin margin = tradingSettingsProvider.getMargins(instrument, account); 
            String sMarketStatus = "unknown";
            switch (marketStatus) {
            case MARKET_STATUS_OPEN:
                sMarketStatus = "Market Open";
                break;
            case MARKET_STATUS_CLOSED:
                sMarketStatus = "Market Close";
                break;
            }
            System.out.println(String.format("Instrument: %s, Status: %s", instrument, sMarketStatus));
            System.out.println(String.format("Cond.Dist: ST=%s; LT=%s", condDistStopForTrade, condDistLimitForTrade));
            System.out.println(String.format("Cond.Dist entry stop=%s; entry limit=%s", condDistEntryStop,
                    condDistEntryLimit));
            System.out.println(String.format("Quantity: Min=%s; Max=%s. Base unit size=%s; MMR=%s", minQuantity,
                    maxQuantity, baseUnitSize, mmr));
            if (margin != null ) {
                mmr2 = margin.getMMR();
                emr = margin.getEMR();
                lmr = margin.getLMR();
                if(margin.is3LevelMargin()) {
                    System.out.println(String.format("Three level margin: MMR=%s; EMR=%s; LMR=%s", mmr2, emr, lmr));
                }
                else
                {
                    System.out.println(String.format("Single level margin: MMR=%s; EMR=%s; LMR=%s", mmr2, emr, lmr));
                }
            }
            System.out.println(String.format("Trailing step: %s-%s", minTrailingStep, maxTrailingStep));
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
            System.out.println(String.format("%s * %s %s %s %s", loginPrm.getLogin(), loginPrm.getURL(),
                  loginPrm.getConnection(), loginPrm.getSessionID(), loginPrm.getPin()));
        }
    }
}
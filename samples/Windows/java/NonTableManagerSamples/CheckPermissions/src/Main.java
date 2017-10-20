package checkpermissions;

import com.fxcore2.*;
import common.*;

public class Main {
    public static void main(String[] args) {
        O2GSession session = null;
        try {
            String sProcName = "CheckPermissions";
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
                checkPermissions(session, sampleParams.getInstrument());
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

    // Show permissions for the particular instrument
    public static void checkPermissions(O2GSession session, String sInstrument) {
        O2GLoginRules loginRules = session.getLoginRules();
        O2GPermissionChecker permissionChecker = loginRules.getPermissionChecker();
        System.out.println("canCreateMarketOpenOrder = "
                + permissionChecker.canCreateMarketOpenOrder(sInstrument));
        System.out.println("canChangeMarketOpenOrder = "
                + permissionChecker.canChangeMarketOpenOrder(sInstrument));
        System.out.println("canDeleteMarketOpenOrder = "
                + permissionChecker.canDeleteMarketOpenOrder(sInstrument));
        System.out.println("canCreateMarketCloseOrder = "
                + permissionChecker.canCreateMarketCloseOrder(sInstrument));
        System.out.println("canChangeMarketCloseOrder = "
                + permissionChecker.canChangeMarketCloseOrder(sInstrument));
        System.out.println("canDeleteMarketCloseOrder = "
                + permissionChecker.canDeleteMarketCloseOrder(sInstrument));
        System.out.println("canCreateEntryOrder = "
                + permissionChecker.canCreateEntryOrder(sInstrument));
        System.out.println("canChangeEntryOrder = "
                + permissionChecker.canChangeEntryOrder(sInstrument));
        System.out.println("canDeleteEntryOrder = "
                + permissionChecker.canDeleteEntryOrder(sInstrument));
        System.out.println("canCreateStopLimitOrder = "
                + permissionChecker.canCreateStopLimitOrder(sInstrument));
        System.out.println("canChangeStopLimitOrder = "
                + permissionChecker.canChangeStopLimitOrder(sInstrument));
        System.out.println("canDeleteStopLimitOrder = "
                + permissionChecker.canDeleteStopLimitOrder(sInstrument));
        System.out.println("canRequestQuote = "
                + permissionChecker.canRequestQuote(sInstrument));
        System.out.println("canAcceptQuote = "
                + permissionChecker.canAcceptQuote(sInstrument));
        System.out.println("canDeleteQuote = "
                + permissionChecker.canDeleteQuote(sInstrument));
        System.out.println("canJoinToNewContingencyGroup = "
                + permissionChecker.canJoinToNewContingencyGroup(sInstrument));
        System.out.println("canJoinToExistingContingencyGroup = "
                + permissionChecker.canJoinToExistingContingencyGroup(sInstrument));
        System.out.println("canRemoveFromContingencyGroup = "
                + permissionChecker.canRemoveFromContingencyGroup(sInstrument));
        System.out.println("canChangeOfferSubscription = "
                + permissionChecker.canChangeOfferSubscription(sInstrument));
        System.out.println("canCreateNetCloseOrder = "
                + permissionChecker.canCreateNetCloseOrder(sInstrument));
        System.out.println("canChangeNetCloseOrder = "
                + permissionChecker.canChangeNetCloseOrder(sInstrument));
        System.out.println("canDeleteNetCloseOrder = "
                + permissionChecker.canDeleteNetCloseOrder(sInstrument));
        System.out.println("canCreateNetStopLimitOrder = "
                + permissionChecker.canCreateNetStopLimitOrder(sInstrument));
        System.out.println("canChangeNetStopLimitOrder = "
                + permissionChecker.canChangeNetStopLimitOrder(sInstrument));
        System.out.println("canDeleteNetStopLimitOrder = "
                + permissionChecker.canDeleteNetStopLimitOrder(sInstrument));
        System.out.println("canUseDynamicTrailingForStop = "
                + permissionChecker.canUseDynamicTrailingForStop());
        System.out.println("canUseDynamicTrailingForLimit = "
                + permissionChecker.canUseDynamicTrailingForLimit());
        System.out.println("canUseDynamicTrailingForEntryStop = "
                + permissionChecker.canUseDynamicTrailingForEntryStop());
        System.out.println("canUseDynamicTrailingForEntryLimit = "
                + permissionChecker.canUseDynamicTrailingForEntryLimit());
        System.out.println("canUseFluctuateTrailingForStop = "
                + permissionChecker.canUseFluctuateTrailingForStop());
        System.out.println("canUseFluctuateTrailingForLimit = "
                + permissionChecker.canUseFluctuateTrailingForLimit());
        System.out.println("canUseFluctuateTrailingForEntryStop = "
                + permissionChecker.canUseFluctuateTrailingForEntryStop());
        System.out.println("canUseFluctuateTrailingForEntryLimit = "
                + permissionChecker.canUseFluctuateTrailingForEntryLimit());
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
    private static void printSampleParams(String procName, LoginParams loginPrm, SampleParams prm) {
        System.out.println(String.format("Running %s with arguments:", procName));
        if (loginPrm != null) {
            System.out.println(String.format("%s * %s %s %s %s", loginPrm.getLogin(), loginPrm.getURL(),
                  loginPrm.getConnection(), loginPrm.getSessionID(), loginPrm.getPin()));
        }
        if (prm != null) {
            System.out.println(String.format("Instrument='%s'", prm.getInstrument()));
        }
    }
}
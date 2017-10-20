package twoconnections;

import com.fxcore2.*;

public class Main {

    public static void main(String[] args) {
        O2GSession session1 = null;
        O2GSession session2 = null;

        try {
            String sProcName = "TwoConnections";
            if (args.length == 0) {
                printHelp(sProcName);
                return;
            }

            LoginParams loginParams = new LoginParams(args);
            SampleParams sampleParams = new SampleParams(args);
            printSampleParams(sProcName, loginParams, sampleParams);
            checkObligatoryParams(loginParams, sampleParams);

            session1 = O2GTransport.createSession();
            session2 = O2GTransport.createSession();
            
            Thread connection1 = new Thread(new Connection(session1, loginParams, sampleParams, true));
            Thread connection2 = new Thread(new Connection(session2, loginParams, sampleParams, false));

            connection1.start();
            connection2.start();
            
            connection1.join();
            connection2.join();
            
        } catch (Exception e) {
            System.out.println("Exception: " + e.toString());
        } finally {
            if (session1 != null) {
                session1.dispose();
            }
            if (session2 != null) {
                session2.dispose();
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
        
        System.out.println("/account | --account ");
        System.out.println("An account which you want to use in sample. Optional parameter.\n");
        
        System.out.println("/buysell | --buysell | /d | -d");
        System.out.println("The order direction. Possible values are: B - buy, S - sell.\n");
        
        System.out.println("/lots | --lots ");
        System.out.println("Trade amount in lots. Optional parameter.\n");
        
        System.out.println("/login2 | --login2 | /l | -l");
        System.out.println("Your user name for second session.\n");
        
        System.out.println("/password2 | --password2 | /p | -p");
        System.out.println("Your password for second session.\n");
        
        System.out.println("/url2 | --url2 | /u | -u");
        System.out.println("The server URL for second session. For example, http://www.fxcorporate.com/Hosts.jsp.\n");
        
        System.out.println("/connection2 | --connection2 | /c | -c");
        System.out.println("The connection name for second session. For example, \"Demo\" or \"Real\".\n");
        
        System.out.println("/sessionid2 | --sessionid2 ");
        System.out.println("The database name for second session. Required only for users who have accounts in more than one database. Optional parameter.\n");
        
        System.out.println("/pin2 | --pin2 ");
        System.out.println("Your pin code for second session. Optional argument. Required only for users who have a pin. Optional parameter.\n");
        
        System.out.println("/account2 | --account2 ");
        System.out.println("An account for second session which you want to use in sample. Optional parameter.\n");
    }
    
    // Check obligatory login parameters and sample parameters
    private static void checkObligatoryParams(LoginParams loginParams, SampleParams sampleParams) throws Exception {
        if(loginParams.getLogin().isEmpty()) {
            throw new Exception(LoginParams.LOGIN_NOT_SPECIFIED);
        }
        if(loginParams.getLogin2().isEmpty()) {
            throw new Exception(LoginParams.LOGIN2_NOT_SPECIFIED);
        }
        if(loginParams.getPassword().isEmpty()) {
            throw new Exception(LoginParams.PASSWORD_NOT_SPECIFIED);
        }
        if(loginParams.getPassword2().isEmpty()) {
            throw new Exception(LoginParams.PASSWORD2_NOT_SPECIFIED);
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
        if(sampleParams.getBuySell().isEmpty()) {
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
            System.out.println(String.format("Instrument='%s', BuySell='%s', Lots='%s', AccountID='%s', AccountID2='%s'",
                    prm.getInstrument(),
                    prm.getBuySell(), prm.getLots(),
                    prm.getAccountID(), prm.getAccountID2()));
        }
    }
}
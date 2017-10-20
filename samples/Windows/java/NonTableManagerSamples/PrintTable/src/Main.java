package printtable;

import com.fxcore2.*;
import common.*;

public class Main {
    
    public static void main(String[] args) {
        O2GSession session = null;

        try {
            String sProcName = "PrintTable";
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
                ResponseListener responseListener = new ResponseListener();
                session.subscribeResponse(responseListener);

                O2GAccountRow account = getAccount(session);
                if (account == null)
                    throw new Exception("No valid accounts");

                printOrders(session, account.getAccountID(), responseListener);
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
    private static O2GAccountRow getAccount(O2GSession session) throws Exception {
        O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
        if (readerFactory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GLoginRules loginRules = session.getLoginRules();
        O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.ACCOUNTS);
        O2GAccountsTableResponseReader accountsResponseReader = readerFactory.createAccountsTableReader(response);
        for (int i = 0; i < accountsResponseReader.size(); i++) {
            O2GAccountRow accountRow = accountsResponseReader.getRow(i);
            System.out.println(String.format("AccountID: %s, Balance: %.2f", accountRow.getAccountID(), accountRow.getBalance()));
        }
        return accountsResponseReader.getRow(0);
    }

    // Print orders table for account
    private static void printOrders(O2GSession session, String sAccountID, ResponseListener responseListener) throws Exception {
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.ORDERS, sAccountID);
        if (request != null) {
            System.out.println("Orders table for account " + sAccountID);
            responseListener.setRequestID(request.getRequestId());
            session.sendRequest(request);
            if (!responseListener.waitEvents()) {
                throw new Exception("Response waiting timeout expired");
            }
            O2GResponse response = responseListener.getResponse();
            if (response != null) {
                O2GResponseReaderFactory responseReaderFactory = session.getResponseReaderFactory();
                O2GOrdersTableResponseReader responseReader = responseReaderFactory.createOrdersTableReader(response);
                for (int i = 0; i < responseReader.size(); i++) {
                    O2GOrderRow orderRow = responseReader.getRow(i);
                    System.out.println(String.format("OrderID: %s, Status: %s, Amount: %s", orderRow.getOrderID(), orderRow.getStatus(), orderRow.getAmount()));
                }
            } else {
                throw new Exception("Cannot get response");
            }
        } else {
            throw new Exception("Cannot create request");
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
        
        System.out.println("/account | --account ");
        System.out.println("An account which you want to use in sample. Optional parameter.\n");
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
package printtablelistener;

import com.fxcore2.*;
import common.*;

public class Main {

    public static void main(String[] args) {
        O2GSession session = null;

        try {
            String sProcName = "PrintTableListener";
            if (args.length == 0) {
                printHelp(sProcName);
                return;
            }

            LoginParams loginParams = new LoginParams(args);
            printSampleParams(sProcName, loginParams);
            checkObligatoryParams(loginParams);

            session = O2GTransport.createSession();
            TableManagerListener managerListener = new TableManagerListener();
            session.useTableManager(O2GTableManagerMode.YES, managerListener);
            SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.getSessionID(), loginParams.getPin());
            session.subscribeSessionStatus(statusListener);
            statusListener.reset();
            session.login(loginParams.getLogin(), loginParams.getPassword(), loginParams.getURL(), loginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                O2GTableManager tableManager = session.getTableManager();
                if(managerListener.waitEvents() && managerListener.isLoaded()) {
                    System.out.println("All tables are loaded");
                } else {
                    throw new Exception ("Cannot refresh all tables of table manager");
                }
                
                O2GAccountRow account = getAccount(tableManager);
                if (account == null)
                    throw new Exception("No valid accounts");
                
                managerListener.reset();
                O2GTableManager tableManagerByAccount = session.getTableManagerByAccount(account.getAccountID());
                if (managerListener.waitEvents() && managerListener.isLoaded()) {
                    System.out.println(String.format("All tables are loaded (account %s)", account.getAccountID()));
                } else {
                    throw new Exception(String.format("Cannot refresh all tables of table manager (account %s)", account.getAccountID()));
                }

                printOrders(tableManagerByAccount, account.getAccountID());
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

    // Get the first account
    private static O2GAccountRow getAccount(O2GTableManager tableManager) throws Exception {
        O2GAccountsTable accountsTable = (O2GAccountsTable)tableManager.getTable(O2GTableType.ACCOUNTS);
        O2GTableIterator accountsIterator = new O2GTableIterator();
        O2GAccountTableRow accountRow = accountsTable.getNextRow(accountsIterator);
        while(accountRow!= null) {
            System.out.println(String.format("AccountID: %s, Balance: %.2f",
                    accountRow.getAccountID(), accountRow.getBalance()));
            accountRow = accountsTable.getNextRow(accountsIterator);
        }
        return accountsTable.getRow(0);
    }

    // Print orders table using IO2GEachRowListener
    public static void printOrders(O2GTableManager tableManager, String sAccountID) {
        O2GOrdersTable ordersTable = (O2GOrdersTable)tableManager.getTable(O2GTableType.ORDERS);
        if (ordersTable.size() == 0) {
            System.out.println("Table is empty!");
        } else {
            ordersTable.forEachRow(new EachRowListener(sAccountID));
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
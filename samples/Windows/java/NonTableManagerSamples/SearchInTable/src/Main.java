package searchintable;

import com.fxcore2.*;
import common.*;

public class Main {
    
    public static void main(String[] args) {
        O2GSession session = null;

        try {
            String sProcName = "SearchInTable";
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

                findOrder(session, sampleParams.getAccountID(), sampleParams.getOrderID(), responseListener);
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
                    }
                }
            }
            System.out.println(String.format("AccountID: %s, Balance: %.2f", account.getAccountID(), account.getBalance()));
        }
        if (!bHasAccount) {
            return null;
        } else {
            return account;
        }
    }

    // Find order by ID and print it
    private static void findOrder(O2GSession session, String sAccountID, String sOrderID, ResponseListener responseListener) throws Exception {
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.ORDERS, sAccountID);
        if (request != null) {
            responseListener.setRequestID(request.getRequestId());
            session.sendRequest(request);
            if (!responseListener.waitEvents()) {
                throw new Exception("Response waiting timeout expired");
            }
            O2GResponse orderResponse = responseListener.getResponse();
            if (orderResponse != null) {
                if (orderResponse.getType() == O2GResponseType.GET_ORDERS) {
                    O2GResponseReaderFactory responseReaderFactory = session.getResponseReaderFactory();
                    boolean bFound = false;
                    O2GOrdersTableResponseReader responseReader = responseReaderFactory.createOrdersTableReader(orderResponse);
                    for (int i = 0; i < responseReader.size(); i++) {
                        O2GOrderRow orderRow = responseReader.getRow(i);
                        if (sOrderID.equals(orderRow.getOrderID())) {
                            System.out.println(String.format("OrderID=%s; AccountID=%s; Type=%s; Status=%s; OfferID=%s; Amount=%s; BuySell=%s; Rate=%s",
                                    orderRow.getOrderID(), orderRow.getAccountID(), orderRow.getType(), orderRow.getStatus(), orderRow.getOfferID(),
                                    orderRow.getAmount(), orderRow.getBuySell(), orderRow.getRate()));
                            bFound = true;
                            break;
                        }
                    }
                    if (!bFound) {
                        System.out.println(String.format("Order '%s' is not found", sOrderID));
                    }
                }
            }
        } else {
            System.out.println("Cannot create request");
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
        
        System.out.println("/orderid | --orderid ");
        System.out.println("Order, for which you want to display information. Mandatory argument.\n");
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
        if(sampleParams.getOrderID().isEmpty()) {
            throw new Exception(SampleParams.ORDERID_NOT_SPECIFIED);
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
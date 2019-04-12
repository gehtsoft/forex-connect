package getreport;

import java.net.*;
import java.util.Calendar;
import java.io.*;
import com.fxcore2.*;
import common.*;

public class Main {
    
    public static void main(String[] args) {
        O2GSession session = null;
        try {
            String sProcName = "GetReport";
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
                getReports(session);
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
    
    // Get reports for all accounts
    public static void getReports(O2GSession session) throws Exception {
        O2GLoginRules loginRules = session.getLoginRules();
        if (loginRules == null) {
            throw new Exception("Cannot get login rules");
        }
        O2GResponseReaderFactory responseFactory = session.getResponseReaderFactory();
        O2GResponse accountsResponse = loginRules.getTableRefreshResponse(O2GTableType.ACCOUNTS);
        O2GAccountsTableResponseReader accountsReader = responseFactory.createAccountsTableReader(accountsResponse);
        for (int i = 0; i < accountsReader.size(); i++) {
            O2GAccountRow account = accountsReader.getRow(i);
            Calendar dtFrom = Calendar.getInstance();
            dtFrom.add(Calendar.MONTH,-1);
            String sUrl = session.getReportURL(account.getAccountID(), dtFrom, Calendar.getInstance(), "html", null);
            String sFilename = account.getAccountID() + ".html";
            String sOutputDir = System.getProperty("getreport.output.dir");
            if (sOutputDir != null) {
                sFilename = sOutputDir + File.separator + sFilename;
            }

            System.out.println(String.format("AccountID=%s; Balance=%.2f; Report URL=%s",
                    account.getAccountID(), account.getBalance(), sUrl));

            URL url = new URL(sUrl);
            String content = getContent(url);
            String prefix = String.format("%s://%s/", url.getProtocol(), url.getHost()); 
            String report = O2GHtmlContentUtils.replaceRelativePathWithAbsolute(content, prefix);

            try (PrintWriter out = new PrintWriter(sFilename)) {
                out.println(report);
            }

            System.out.println("Report is saved to " + sFilename);
        }
    }

    public static String getContent(URL url) throws Exception {

        URLConnection connection = url.openConnection();
        BufferedReader in = new BufferedReader(
                                new InputStreamReader(
                                    connection.getInputStream()));

        StringBuilder response = new StringBuilder();
        String inputLine;

        while ((inputLine = in.readLine()) != null) 
            response.append(inputLine).append(System.getProperty("line.separator"));

        in.close();

        return response.toString();
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
package twoconnections;
public class LoginParams {
    public static final String LOGIN_NOT_SPECIFIED = "'Login' is not specified (/l|-l|/login|--login)";
    public static final String PASSWORD_NOT_SPECIFIED = "'Password' is not specified (/p|-p|/password|--password)";
    public static final String LOGIN2_NOT_SPECIFIED = "'Login2' is not specified (/login2|--login2)";
    public static final String PASSWORD2_NOT_SPECIFIED = "'Password2' is not specified (/password2|--password2)";
    public static final String URL_NOT_SPECIFIED = "'URL' is not specified (/u|-u|/url|--url)";
    public static final String CONNECTION_NOT_SPECIFIED = "'Connection' is not specified (/c|-c|/connection|--connection)";

    public String getLogin() {
        return mLogin;
    }
    private String mLogin;

    public String getLogin2() {
        return mLogin2;
    }
    private String mLogin2;

    public String getPassword() {
        return mPassword;
    }
    private String mPassword;

    public String getPassword2() {
        return mPassword2;
    }
    private String mPassword2;

    public String getURL() {
        return mURL;
    }
    private String mURL;

    public String getConnection() {
        return mConnection;
    }
    private String mConnection;

    public String getSessionID() {
        return mSessionID;
    }
    private String mSessionID;

    public String getSessionID2() {
        return mSessionID2;
    }
    private String mSessionID2;

    public String getPin() {
        return mPin;
    }
    private String mPin;

    public String getPin2() {
        return mPin2;
    }
    private String mPin2;

    public LoginParams(String[] args) {
        // Get parameters with short keys
        mLogin = getArgument(args, "l");
        mPassword = getArgument(args, "p");
        mURL = getArgument(args, "u");
        mConnection = getArgument(args, "c");

        // If parameters with short keys are not specified, get parameters with long keys
        if (mLogin.isEmpty())
            mLogin = getArgument(args, "login");
        if (mPassword.isEmpty())
            mPassword = getArgument(args, "password");
        if (mURL.isEmpty())
            mURL = getArgument(args, "url");
        if (mConnection.isEmpty())
            mConnection = getArgument(args, "connection");
        mLogin2 = getArgument(args, "login2");
        mPassword2 = getArgument(args, "password2");

        // Get optional parameters
        mSessionID = getArgument(args, "sessionid");
        mPin = getArgument(args, "pin");
        mSessionID2 = getArgument(args, "sessionid2");
        mPin2 = getArgument(args, "pin2");
    }

    private String getArgument(String[] args, String sKey) {
        for (int i = 0; i < args.length; i++) {
            int iDelimOffset = 0;
            if (args[i].startsWith("--")) {
                iDelimOffset = 2;
            } else if (args[i].startsWith("-") || args[i].startsWith("/")) {
                iDelimOffset = 1;
            }

            if (args[i].substring(iDelimOffset).equals(sKey) && (args.length > i+1)) {
                return args[i+1];
            }
        }
        return "";
    }
}

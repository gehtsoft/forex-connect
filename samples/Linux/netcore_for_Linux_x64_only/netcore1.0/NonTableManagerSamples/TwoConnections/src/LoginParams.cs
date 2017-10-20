
using System;
using System.Collections.Generic;

namespace ArgParser
{
    class LoginParams
    {
        public static readonly String LOGIN_HELP_STRING = "/login | --login | /l | -l\nYour user name.\n";
        public static readonly String PASSWORD_HELP_STRING = "/password | --password | /p | -p\nYour password.\n";
        public static readonly String LOGIN2_HELP_STRING = "/login2 | --login2\nYour user name for second connection.\n";
        public static readonly String PASSWORD2_HELP_STRING = "/password2 | --password2\nYour password for second connection.\n";
        public static readonly String URL_HELP_STRING = "/url | --url | /u | -u\nThe server URL. For example, http://www.fxcorporate.com/Hosts.jsp\n";
        public static readonly String CONNECTION_HELP_STRING = "/connection | --connection | /c | -c\nThe connection name. For example, \"Demo\" or \"Real\".\n";
        public static readonly String URL2_HELP_STRING = "/url2 | --url2\nThe server URL for second connection. For example, http://www.fxcorporate.com/Hosts.jsp\n";
        public static readonly String CONNECTION2_HELP_STRING = "/connection2 | --connection2\nThe second connection name. For example, \"Demo\" or \"Real\".\n";
        public static readonly String SESSIONID_HELP_STRING = "/sessionid | --sessionid\nThe database name. Required only for users who have accounts in more than one database. Optional parameter.\n";
        public static readonly String PIN_HELP_STRING = "/pin | --pin\nYour pin code. Required only for users who have a pin. Optional parameter\n";
        public static readonly String SESSIONID2_HELP_STRING = "/sessionid2 | --sessionid2\nThe database name for second connection. Required only for users who have accounts in more than one database. Optional parameter.\n";
        public static readonly String PIN2_HELP_STRING = "/pin2 | --pin2\nYour pin code for second connection. Required only for users who have a pin. Optional parameter\n";

        public static readonly String LOGIN_NOT_SPECIFIED = "'Login' is not specified (/l|-l|/login|--login)\n";
        public static readonly String PASSWORD_NOT_SPECIFIED = "'Password' is not specified (/p|-p|/password|--password)\n";
        public static readonly String LOGIN2_NOT_SPECIFIED = "'Login2' is not specified (/login2|--login2)\n";
        public static readonly String PASSWORD2_NOT_SPECIFIED = "'Password2' is not specified (/password2|--password2)\n";
        public static readonly String URL_NOT_SPECIFIED = "'URL' is not specified (/u|-u|/url|--url)\n";
        public static readonly String CONNECTION_NOT_SPECIFIED = "'Connection' is not specified (/c|-c|/connection|--connection)\n";
        public static readonly String URL2_NOT_SPECIFIED = "'URL2' is not specified (/url2|--url2)\n";
        public static readonly String CONNECTION2_NOT_SPECIFIED = "'Connection2' is not specified (/connection2|--connection2)\n";

        public string Login
        {
            get
            {
                return mLogin;
            }
        }
        private string mLogin;

        public string Password
        {
            get
            {
                return mPassword;
            }
        }
        private string mPassword;

        public string URL
        {
            get
            {
                return mURL;
            }
        }
        private string mURL;

        public string Connection
        {
            get
            {
                return mConnection;
            }
        }
        private string mConnection;

        public string URL2
        {
            get
            {
                return mURL2;
            }
        }
        private string mURL2;

        public string Connection2
        {
            get
            {
                return mConnection2;
            }
        }
        private string mConnection2;

        public string SessionID
        {
            get
            {
                return mSessionID;
            }
        }
        private string mSessionID;

        public string Pin
        {
            get
            {
                return mPin;
            }
        }

        private string mPin;

        public string Login2
        {
            get
            {
                return mLogin2;
            }
        }
        private string mLogin2;

        public string Password2
        {
            get
            {
                return mPassword2;
            }
        }
        private string mPassword2;

        public string SessionID2
        {
            get
            {
                return mSessionID2;
            }
        }
        private string mSessionID2;

        public string Pin2
        {
            get
            {
                return mPin2;
            }
        }
        private string mPin2;

        public LoginParams(String[] args)
        {
            // Get parameters with short keys
            mLogin = GetArgument(args, "l");
            mPassword = GetArgument(args, "p");
            mURL = GetArgument(args, "u");
            mConnection = GetArgument(args, "c");

            // If parameters with short keys are not specified, get parameters with long keys
            if (string.IsNullOrEmpty(mLogin))
                mLogin = GetArgument(args, "login");

            if (string.IsNullOrEmpty(mPassword))
                mPassword = GetArgument(args, "password");

            mLogin2 = GetArgument(args, "login2");
            mPassword2 = GetArgument(args, "password2");

            if (string.IsNullOrEmpty(mURL))
                mURL = GetArgument(args, "url");

            if (!string.IsNullOrEmpty(mURL))
            {
                if (!mURL.EndsWith("Hosts.jsp", StringComparison.OrdinalIgnoreCase))
                {
                    mURL += "/Hosts.jsp";
                }
            }

            mURL2 = GetArgument(args, "url2");

            if (!string.IsNullOrEmpty(mURL2))
            {
                if (!mURL2.EndsWith("Hosts.jsp", StringComparison.OrdinalIgnoreCase))
                {
                    mURL2 += "/Hosts.jsp";
                }
            }

            if (string.IsNullOrEmpty(mConnection))
                mConnection = GetArgument(args, "connection");

            mConnection2 = GetArgument(args, "connection2");

            // Get optional parameters
            mSessionID = GetArgument(args, "sessionid");
            mPin = GetArgument(args, "pin");

            mSessionID2 = GetArgument(args, "sessionid2");
            mPin2 = GetArgument(args, "pin2");
        }

        private String GetArgument(String[] args, String sKey)
        {
            for (int i = 0; i < args.Length; i++)
            {
                int iDelimOffset = 0;
                if (args[i].StartsWith("--"))
                {
                    iDelimOffset = 2;
                }
                else if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                {
                    iDelimOffset = 1;
                }

                if (args[i].Substring(iDelimOffset) == sKey && (args.Length > i + 1))
                {
                    return args[i + 1];
                }
            }
            return "";
        }
    }
}

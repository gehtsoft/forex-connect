using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Configuration;
using fxcore2;

namespace GetReport
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;

            try
            {
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);

                PrintSampleParams("GetReport", loginParams);

                session = O2GTransport.createSession();
                SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    GetReports(session);
                    Console.WriteLine("Done!");
                    statusListener.Reset();
                    session.logout();
                    statusListener.WaitEvents();
                }
                session.unsubscribeSessionStatus(statusListener);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                if (session != null)
                {
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Get reports for all accounts
        /// </summary>
        /// <param name="session"></param>
        public static void GetReports(O2GSession session)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            if (loginRules == null)
            {
                throw new Exception("Cannot get login rules");
            }
            O2GResponseReaderFactory responseFactory = session.getResponseReaderFactory();
            O2GResponse accountsResponse = loginRules.getTableRefreshResponse(O2GTableType.Accounts);
            O2GAccountsTableResponseReader accountsReader = responseFactory.createAccountsTableReader(accountsResponse);
            System.Net.WebClient webClient = new System.Net.WebClient();
            for (int i = 0; i < accountsReader.Count; i++)
            {
                O2GAccountRow account = accountsReader.getRow(i);
                Uri url = new Uri(session.getReportURL(account.AccountID, DateTime.Now.AddMonths(-1), DateTime.Now, "html", null));
                
                Console.WriteLine("AccountID={0}; Balance={1}; Report URL={2}",
                        account.AccountID, account.Balance, url);

                string content = webClient.DownloadString(url);

                string prefix = url.Scheme + Uri.SchemeDelimiter + url.Host;
                string report = O2GHtmlContentUtils.ReplaceRelativePathWithAbsolute(content, prefix);

                string filename = account.AccountID + ".html";
                System.IO.File.WriteAllText(filename, report);
                Console.WriteLine("Report is saved to {0}", filename);
            }
        }

        /// <summary>
        /// Print process name and sample parameters
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="loginPrm"></param>
        private static void PrintSampleParams(string procName, LoginParams loginPrm)
        {
            Console.WriteLine("{0}", procName);
        }

        class LoginParams
        {
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

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="args"></param>
            public LoginParams(NameValueCollection args)
            {
                mLogin = GetRequiredArgument(args, "Login");
                mPassword = GetRequiredArgument(args, "Password");
                mURL = GetRequiredArgument(args, "URL");
                if (!string.IsNullOrEmpty(mURL))
                {
                    if (!mURL.EndsWith("Hosts.jsp", StringComparison.OrdinalIgnoreCase))
                    {
                        mURL += "/Hosts.jsp";
                    }
                }
                mConnection = GetRequiredArgument(args, "Connection");
                mSessionID = args["SessionID"];
                mPin = args["Pin"];
            }

            /// <summary>
            /// Get required argument from configuration file
            /// </summary>
            /// <param name="args">Configuration file key-value collection</param>
            /// <param name="sArgumentName">Argument name (key) from configuration file</param>
            /// <returns>Argument value</returns>
            private string GetRequiredArgument(NameValueCollection args, string sArgumentName)
            {
                string sArgument = args[sArgumentName];
                if (!string.IsNullOrEmpty(sArgument))
                {
                    sArgument = sArgument.Trim();
                }
                if (string.IsNullOrEmpty(sArgument))
                {
                    throw new Exception(string.Format("Please provide {0} in configuration file", sArgumentName));
                }
                return sArgument;
            }
        }
    }
}

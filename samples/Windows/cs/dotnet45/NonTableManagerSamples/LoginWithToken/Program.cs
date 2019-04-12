using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Configuration;
using fxcore2;

namespace Login
{
    class Program
    {
        static SessionStatusListener statusListener = null;
        static SessionStatusListener secondStatusListener = null;

        static void Main(string[] args)
        {
            O2GSession session = null;
            O2GSession secondSession = null;
            try
            {
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);

                PrintSampleParams("Login", loginParams);

                session = O2GTransport.createSession();
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin, "PrimarySessionStatus");
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    string token = session.getToken();
                    Console.WriteLine("Token obtained: {0}", token);
                    secondSession = O2GTransport.createSession();
                    secondStatusListener = new SessionStatusListener(secondSession, loginParams.SessionID, loginParams.Pin, "SecondarySessionStatus");
                    secondSession.subscribeSessionStatus(secondStatusListener);
                    secondStatusListener.Reset();
                    secondSession.loginWithToken(loginParams.Login, token, loginParams.URL, loginParams.Connection);
                    if (secondStatusListener.WaitEvents() && secondStatusListener.Connected)
                    {
                        Console.WriteLine("Done!");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                if (secondSession != null)
                {
                    if (secondStatusListener.Connected)
                    {
                        secondStatusListener.Reset();
                        secondSession.logout();
                        secondStatusListener.WaitEvents();
                    }
                    secondSession.unsubscribeSessionStatus(secondStatusListener);
                    secondSession.Dispose();
                }
                if (session != null)
                {
                    if (statusListener.Connected)
                    {
                        statusListener.Reset();
                        session.logout();
                        statusListener.WaitEvents();
                    }
                    session.unsubscribeSessionStatus(statusListener);
                    session.Dispose();
                }
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

using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Configuration;
using fxcore2;

namespace CheckPermissions
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;

            try
            {
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);
                SampleParams sampleParams = new SampleParams(ConfigurationManager.AppSettings);

                PrintSampleParams("CheckPermissions", loginParams, sampleParams);

                session = O2GTransport.createSession();
                SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    CheckPermissions(session, sampleParams.Instrument);
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
        /// Show permissions for the particular instrument
        /// </summary>
        public static void CheckPermissions(O2GSession session, string sInstrument)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            O2GPermissionChecker permissionChecker = loginRules.getPermissionChecker();
            Console.WriteLine("canCreateMarketOpenOrder = {0}", permissionChecker.canCreateMarketOpenOrder(sInstrument));
            Console.WriteLine("canChangeMarketOpenOrder = {0}", permissionChecker.canChangeMarketOpenOrder(sInstrument));
            Console.WriteLine("canDeleteMarketOpenOrder = {0}", permissionChecker.canDeleteMarketOpenOrder(sInstrument));
            Console.WriteLine("canCreateMarketCloseOrder = {0}", permissionChecker.canCreateMarketCloseOrder(sInstrument));
            Console.WriteLine("canChangeMarketCloseOrder = {0}", permissionChecker.canChangeMarketCloseOrder(sInstrument));
            Console.WriteLine("canDeleteMarketCloseOrder = {0}", permissionChecker.canDeleteMarketCloseOrder(sInstrument));
            Console.WriteLine("canCreateEntryOrder = {0}", permissionChecker.canCreateEntryOrder(sInstrument));
            Console.WriteLine("canChangeEntryOrder = {0}", permissionChecker.canChangeEntryOrder(sInstrument));
            Console.WriteLine("canDeleteEntryOrder = {0}", permissionChecker.canDeleteEntryOrder(sInstrument));
            Console.WriteLine("canCreateStopLimitOrder = {0}", permissionChecker.canCreateStopLimitOrder(sInstrument));
            Console.WriteLine("canChangeStopLimitOrder = {0}", permissionChecker.canChangeStopLimitOrder(sInstrument));
            Console.WriteLine("canDeleteStopLimitOrder = {0}", permissionChecker.canDeleteStopLimitOrder(sInstrument));
            Console.WriteLine("canRequestQuote = {0}", permissionChecker.canRequestQuote(sInstrument));
            Console.WriteLine("canAcceptQuote = {0}", permissionChecker.canAcceptQuote(sInstrument));
            Console.WriteLine("canDeleteQuote = {0}", permissionChecker.canDeleteQuote(sInstrument));
            Console.WriteLine("canJoinToNewContingencyGroup = {0}", permissionChecker.canJoinToNewContingencyGroup(sInstrument));
            Console.WriteLine("canJoinToExistingContingencyGroup = {0}", permissionChecker.canJoinToExistingContingencyGroup(sInstrument));
            Console.WriteLine("canRemoveFromContingencyGroup = {0}", permissionChecker.canRemoveFromContingencyGroup(sInstrument));
            Console.WriteLine("canChangeOfferSubscription = {0}", permissionChecker.canChangeOfferSubscription(sInstrument));
            Console.WriteLine("canCreateNetCloseOrder = {0}", permissionChecker.canCreateNetCloseOrder(sInstrument));
            Console.WriteLine("canChangeNetCloseOrder = {0}", permissionChecker.canChangeNetCloseOrder(sInstrument));
            Console.WriteLine("canDeleteNetCloseOrder = {0}", permissionChecker.canDeleteNetCloseOrder(sInstrument));
            Console.WriteLine("canCreateNetStopLimitOrder = {0}", permissionChecker.canCreateNetStopLimitOrder(sInstrument));
            Console.WriteLine("canChangeNetStopLimitOrder = {0}", permissionChecker.canChangeNetStopLimitOrder(sInstrument));
            Console.WriteLine("canDeleteNetStopLimitOrder = {0}", permissionChecker.canDeleteNetStopLimitOrder(sInstrument));
            Console.WriteLine("canUseDynamicTrailingForStop = {0}", permissionChecker.canUseDynamicTrailingForStop());
            Console.WriteLine("canUseDynamicTrailingForLimit = {0}", permissionChecker.canUseDynamicTrailingForLimit());
            Console.WriteLine("canUseDynamicTrailingForEntryStop = {0}", permissionChecker.canUseDynamicTrailingForEntryStop());
            Console.WriteLine("canUseDynamicTrailingForEntryLimit = {0}", permissionChecker.canUseDynamicTrailingForEntryLimit());
            Console.WriteLine("canUseFluctuateTrailingForStop = {0}", permissionChecker.canUseFluctuateTrailingForStop());
            Console.WriteLine("canUseFluctuateTrailingForLimit = {0}", permissionChecker.canUseFluctuateTrailingForLimit());
            Console.WriteLine("canUseFluctuateTrailingForEntryStop = {0}", permissionChecker.canUseFluctuateTrailingForEntryStop());
            Console.WriteLine("canUseFluctuateTrailingForEntryLimit = {0}", permissionChecker.canUseFluctuateTrailingForEntryLimit());
        }

        /// <summary>
        /// Print process name and sample parameters
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="loginPrm"></param>
        /// <param name="prm"></param>
        private static void PrintSampleParams(string procName, LoginParams loginPrm, SampleParams prm)
        {
            Console.WriteLine("{0}: Instrument='{1}'", procName, prm.Instrument);
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
        
        class SampleParams
        {
            public string Instrument
            {
                get
                {
                    return mInstrument;
                }
            }
            private string mInstrument;

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="args"></param>
            public SampleParams(NameValueCollection args)
            {
                mInstrument = GetRequiredArgument(args, "Instrument");
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

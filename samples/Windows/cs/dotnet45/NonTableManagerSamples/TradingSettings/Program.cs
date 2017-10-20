using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Configuration;
using fxcore2;

namespace TradingSettings
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;

            try
            {
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);

                PrintSampleParams("TradingSettings", loginParams);

                session = O2GTransport.createSession();
                SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    PrintTradingSettings(session);
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

        // Print trading settings of the first account
        private static void PrintTradingSettings(O2GSession session)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            if (loginRules == null)
            {
                throw new Exception("Cannot get login rules");
            }
            O2GResponse accountsResponse = loginRules.getTableRefreshResponse(O2GTableType.Accounts);
            if (accountsResponse == null)
            {
                throw new Exception("Cannot get response");
            }
            O2GResponse offersResponse = loginRules.getTableRefreshResponse(O2GTableType.Offers);
            if (offersResponse == null)
            {
                throw new Exception("Cannot get response");
            }
            O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
            O2GResponseReaderFactory factory = session.getResponseReaderFactory();
            if (factory == null)
            {
                throw new Exception("Cannot create response reader factory");
            }
            O2GAccountsTableResponseReader accountsReader = factory.createAccountsTableReader(accountsResponse);
            O2GOffersTableResponseReader instrumentsReader = factory.createOffersTableReader(offersResponse);
            O2GAccountRow account = accountsReader.getRow(0);
            for (int i = 0; i < instrumentsReader.Count; i++)
            {
                O2GOfferRow instrumentRow = instrumentsReader.getRow(i);
                string instrument = instrumentRow.Instrument;
                int condDistStopForTrade = tradingSettingsProvider.getCondDistStopForTrade(instrument);
                int condDistLimitForTrade = tradingSettingsProvider.getCondDistLimitForTrade(instrument);
                int condDistEntryStop = tradingSettingsProvider.getCondDistEntryStop(instrument);
                int condDistEntryLimit = tradingSettingsProvider.getCondDistEntryLimit(instrument);
                int minQuantity = tradingSettingsProvider.getMinQuantity(instrument, account);
                int maxQuantity = tradingSettingsProvider.getMaxQuantity(instrument, account);
                int baseUnitSize = tradingSettingsProvider.getBaseUnitSize(instrument, account);
                O2GMarketStatus marketStatus = tradingSettingsProvider.getMarketStatus(instrument);
                int minTrailingStep = tradingSettingsProvider.getMinTrailingStep();
                int maxTrailingStep = tradingSettingsProvider.getMaxTrailingStep();
                double mmr = tradingSettingsProvider.getMMR(instrument, account);
                double mmr2=0, emr=0, lmr=0;
                bool threeLevelMargin = tradingSettingsProvider.getMargins(instrument, account, ref mmr2, ref emr, ref lmr);
                string sMarketStatus = "unknown";
                switch (marketStatus)
                {
                    case O2GMarketStatus.MarketStatusOpen:
                        sMarketStatus = "Market Open";
                        break;
                    case O2GMarketStatus.MarketStatusClosed:
                        sMarketStatus = "Market Close";
                        break;
                }
                Console.WriteLine("Instrument: {0}, Status: {1}", instrument, sMarketStatus);
                Console.WriteLine("Cond.Dist: ST={0}; LT={1}", condDistStopForTrade, condDistLimitForTrade);
                Console.WriteLine("Cond.Dist entry stop={0}; entry limit={1}", condDistEntryStop,
                        condDistEntryLimit);
                Console.WriteLine("Quantity: Min={0}; Max={1}. Base unit size={2}; MMR={3}", minQuantity,
                        maxQuantity, baseUnitSize, mmr);
                if (threeLevelMargin)
                {
                    Console.WriteLine("Three level margin: MMR={0}; EMR={1}; LMR={2}", mmr2, emr, lmr);
                }
                else
                {
                    Console.WriteLine("Single level margin: MMR={0}; EMR={1}; LMR={2}", mmr2, emr, lmr);
                }
                Console.WriteLine("Trailing step: {0}-{1}", minTrailingStep, maxTrailingStep);
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

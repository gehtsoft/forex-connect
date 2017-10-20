using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Configuration;
using System.Threading;
using fxcore2;

namespace FindRowByColumnValue
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            string sOrderType = Constants.Orders.LimitEntry;

            try
            {
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);
                SampleParams sampleParams = new SampleParams(ConfigurationManager.AppSettings);

                PrintSampleParams("FindRowByColumnValue", loginParams, sampleParams);

                session = O2GTransport.createSession();
                session.useTableManager(O2GTableManagerMode.Yes, null);
                SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    ResponseListener responseListener = new ResponseListener();
                    TableListener tableListener = new TableListener(responseListener);
                    session.subscribeResponse(responseListener);

                    O2GTableManager tableManager = session.getTableManager();
                    O2GTableManagerStatus managerStatus = tableManager.getStatus();
                    while (managerStatus == O2GTableManagerStatus.TablesLoading)
                    {
                        Thread.Sleep(50);
                        managerStatus = tableManager.getStatus();
                    }

                    if (managerStatus == O2GTableManagerStatus.TablesLoadFailed)
                    {
                        throw new Exception("Cannot refresh all tables of table manager");
                    }
                    O2GAccountRow account = GetAccount(tableManager, sampleParams.AccountID);
                    if (account == null)
                    {
                        if (string.IsNullOrEmpty(sampleParams.AccountID))
                        {
                            throw new Exception("No valid accounts");
                        }
                        else
                        {
                            throw new Exception(string.Format("The account '{0}' is not valid", sampleParams.AccountID));
                        }
                    }
                    else
                    {
                        if(!account.AccountID.Equals(sampleParams.AccountID))
                        {
                            sampleParams.AccountID = account.AccountID;
                            Console.WriteLine("AccountID='{0}'",
                                    sampleParams.AccountID);
                        }
                    }

                    O2GOfferRow offer = GetOffer(tableManager, sampleParams.Instrument);
                    if (offer == null)
                    {
                        throw new Exception(string.Format("The instrument '{0}' is not valid", sampleParams.Instrument));
                    }

                    O2GLoginRules loginRules = session.getLoginRules();
                    if (loginRules == null)
                    {
                        throw new Exception("Cannot get login rules");
                    }
                    O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(sampleParams.Instrument, account);
                    int iAmount = iBaseUnitSize * sampleParams.Lots;

                    double dRate;
                    double dRateStop;
                    double dRateLimit;
                    double dBid = offer.Bid;
                    double dAsk = offer.Ask;
                    double dPointSize = offer.PointSize;

                    // For the purpose of this example we will place entry order 8 pips from the current market price
                    // and attach stop and limit orders 10 pips from an entry order price
                    if (sOrderType.Equals(Constants.Orders.LimitEntry))
                    {
                        if (sampleParams.BuySell.Equals(Constants.Buy))
                        {
                            dRate = dAsk - 8 * dPointSize;
                            dRateLimit = dRate + 10 * dPointSize;
                            dRateStop = dRate - 10 * dPointSize;
                        }
                        else
                        {
                            dRate = dBid + 8 * dPointSize;
                            dRateLimit = dRate - 10 * dPointSize;
                            dRateStop = dRate + 10 * dPointSize;
                        }
                    }
                    else
                    {
                        if (sampleParams.BuySell.Equals(Constants.Buy))
                        {
                            dRate = dAsk + 8 * dPointSize;
                            dRateLimit = dRate + 10 * dPointSize;
                            dRateStop = dRate - 10 * dPointSize;
                        }
                        else
                        {
                            dRate = dBid - 8 * dPointSize;
                            dRateLimit = dRate - 10 * dPointSize;
                            dRateStop = dRate + 10 * dPointSize;
                        }
                    }

                    tableListener.SubscribeEvents(tableManager);

                    O2GRequest request = CreateELSRequest(session, offer.OfferID, sampleParams.AccountID, iAmount, dRate, dRateLimit, dRateStop, sampleParams.BuySell, sOrderType);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }

                    string sRequestID = request.RequestID;

                    responseListener.SetRequestID(sRequestID);
                    tableListener.SetRequestID(sRequestID);
                    session.sendRequest(request);
                    if (!responseListener.WaitEvents())
                    {
                        throw new Exception("Response waiting timeout expired");
                    }

                    Console.WriteLine("Search by RequestID:{0}", sRequestID);
                    FindOrders(tableManager, sRequestID);
                    Console.WriteLine("Search by Type:{0} and BuySell:{1}", sOrderType, sampleParams.BuySell);
                    FindOrdersByTypeAndDirection(tableManager, sOrderType, sampleParams.BuySell);
                    Console.WriteLine("Search conditional orders");
                    FindConditionalOrders(tableManager);

                    Console.WriteLine("Done!");

                    tableListener.UnsubscribeEvents(tableManager);

                    statusListener.Reset();
                    session.logout();
                    statusListener.WaitEvents();
                    session.unsubscribeResponse(responseListener);
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
        /// Find valid account
        /// </summary>
        /// <param name="tableManager"></param>
        /// <returns>account</returns>
        private static O2GAccountRow GetAccount(O2GTableManager tableManager, string sAccountID)
        {
            bool bHasAccount = false;
            O2GAccountRow account = null;
            O2GAccountsTable accountsTable = (O2GAccountsTable)tableManager.getTable(O2GTableType.Accounts);
            for (int i = 0; i < accountsTable.Count; i++)
            {
                account = accountsTable.getRow(i);
                string sAccountKind = account.AccountKind;
                if (sAccountKind.Equals("32") || sAccountKind.Equals("36"))
                {
                    if (account.MarginCallFlag.Equals("N"))
                    {
                        if (string.IsNullOrEmpty(sAccountID) || sAccountID.Equals(account.AccountID))
                        {
                            bHasAccount = true;
                            break;
                        }
                    }
                }
            }
            if (!bHasAccount)
            {
                return null;
            }
            else
            {
                return account;
            }
        }

        /// <summary>
        /// Find valid offer by instrument name
        /// </summary>
        /// <param name="tableManager"></param>
        /// <param name="sInstrument"></param>
        /// <returns>offer</returns>
        private static O2GOfferRow GetOffer(O2GTableManager tableManager, string sInstrument)
        {
            bool bHasOffer = false;
            O2GOfferRow offer = null;
            O2GOffersTable offersTable = (O2GOffersTable)tableManager.getTable(O2GTableType.Offers);
            for (int i = 0; i < offersTable.Count; i++)
            {
                offer = offersTable.getRow(i);
                if (offer.Instrument.Equals(sInstrument))
                {
                    if (offer.SubscriptionStatus.Equals("T"))
                    {
                        bHasOffer = true;
                        break;
                    }
                }
            }
            if (!bHasOffer)
            {
                return null;
            }
            else
            {
                return offer;
            }
        }

        /// <summary>
        /// Create entry order with attached stop and limit orders request
        /// </summary>
        private static O2GRequest CreateELSRequest(O2GSession session, string sOfferID, string sAccountID, int iAmount, double dRate, double dRateLimit, double dRateStop, string sBuySell, string sOrderType)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valuemap = requestFactory.createValueMap();
            valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemap.setString(O2GRequestParamsEnum.OrderType, sOrderType);
            valuemap.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemap.setString(O2GRequestParamsEnum.BuySell, sBuySell);
            valuemap.setInt(O2GRequestParamsEnum.Amount, iAmount);
            valuemap.setDouble(O2GRequestParamsEnum.Rate, dRate);
            valuemap.setDouble(O2GRequestParamsEnum.RateLimit, dRateLimit);
            valuemap.setDouble(O2GRequestParamsEnum.RateStop, dRateStop);
            valuemap.setString(O2GRequestParamsEnum.CustomID, "EntryOrderWithStopLimit");
            request = requestFactory.createOrderRequest(valuemap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }

        // Find orders by request ID and print it
        private static void FindOrders(O2GTableManager tableManager, string sRequestID)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)tableManager.getTable(O2GTableType.Orders);
            O2GTableIterator ordersIterator = new O2GTableIterator();
            O2GOrderTableRow orderRow = null;
            while (ordersTable.getNextRowByColumnValue("RequestID", sRequestID, ordersIterator, out orderRow))
            {
                Console.WriteLine("Order:{0}, OfferID={1}, Type={2}, Rate={3:N4}, BuySell={4}, Status={5}, Limit={6:N4}, Stop={7:N4}, RequestID={8}",
                        orderRow.OrderID, orderRow.OfferID, orderRow.Type, orderRow.Rate, orderRow.BuySell, orderRow.Status,
                        orderRow.Limit, orderRow.Stop, orderRow.RequestID);
            }
        }

        // Find orders by type and buysell and print it
        private static void FindOrdersByTypeAndDirection(O2GTableManager tableManager, String sOrderType, string sBuySell)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)tableManager.getTable(O2GTableType.Orders);
            O2GTableIterator ordersIterator = new O2GTableIterator();
            O2GOrderTableRow orderRow = null;
            O2GRelationalOperators[] ops = { O2GRelationalOperators.EqualTo, O2GRelationalOperators.EqualTo };
            while (ordersTable.getNextRowByMultiColumnValues(new String[] { "Type", "BuySell" }, ops, new Object[] { sOrderType, sBuySell },
                    0, ordersIterator, out orderRow))
            {
                Console.WriteLine("Order:{0}, OfferID={1}, Type={2}, Rate={3:N4}, BuySell={4}, Status={5}, Limit={6:N4}, Stop={7:N4}, RequestID={8}",
                        orderRow.OrderID, orderRow.OfferID, orderRow.Type, orderRow.Rate, orderRow.BuySell, orderRow.Status,
                        orderRow.Limit, orderRow.Stop, orderRow.RequestID);
            }
        }

        // Find conditional orders and print it
        private static void FindConditionalOrders(O2GTableManager tableManager)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)tableManager.getTable(O2GTableType.Orders);
            O2GTableIterator ordersIterator = new O2GTableIterator();
            O2GOrderTableRow orderRow = null;
            Object[] orderTypes = new Object[] { Constants.Orders.LimitEntry, Constants.Orders.StopEntry,
                    Constants.Orders.Limit, Constants.Orders.Stop, Constants.Orders.Entry, "LTE", "STE" };
            while (ordersTable.getNextRowByColumnValues("Type", O2GRelationalOperators.EqualTo, orderTypes, ordersIterator, out orderRow))
            {
                Console.WriteLine("Order:{0}, OfferID={1}, Type={2}, Rate={3:N4}, BuySell={4}, Status={5}, Limit={6:N4}, Stop={7:N4}, RequestID={8}",
                        orderRow.OrderID, orderRow.OfferID, orderRow.Type, orderRow.Rate, orderRow.BuySell, orderRow.Status,
                        orderRow.Limit, orderRow.Stop, orderRow.RequestID);
            }
        }

        /// <summary>
        /// Print process name and sample parameters
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="loginPrm"></param>
        /// <param name="prm"></param>
        private static void PrintSampleParams(string procName, LoginParams loginPrm, SampleParams prm)
        {
            Console.WriteLine("{0}: Instrument='{1}', BuySell='{2}', Lots='{3}', AccountID='{4}'", procName, prm.Instrument, prm.BuySell, prm.Lots, prm.AccountID);
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

            public string BuySell
            {
                get
                {
                    return mBuySell;
                }
            }
            private string mBuySell;

            public int Lots
            {
                get
                {
                    return mLots;
                }
            }
            private int mLots;

            public string AccountID
            {
                get
                {
                    return mAccountID;
                }
                set
                {
                    mAccountID = value;
                }
            }
            private string mAccountID;

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="args"></param>
            public SampleParams(NameValueCollection args)
            {
                mInstrument = GetRequiredArgument(args, "Instrument");
                mBuySell = GetRequiredArgument(args, "BuySell");
                string sLots = args["Lots"];
                if (string.IsNullOrEmpty(sLots))
                {
                    sLots = "1"; // default
                }
                Int32.TryParse(sLots, out mLots);
                if (mLots <= 0)
                {
                    throw new Exception(string.Format("\"Lots\" value {0} is invalid; please fix the value in the configuration file", sLots));
                }
                mAccountID = args["Account"];
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

using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Configuration;
using fxcore2;

namespace PrintTable
{
    class Program
    {
        static SessionStatusListener statusListener = null;
        static ResponseListener responseListener = null;

        static void Main(string[] args)
        {
            O2GSession session = null;

            try
            {
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);
                SampleParams sampleParams = new SampleParams(ConfigurationManager.AppSettings);

                PrintSampleParams("PrintTable", loginParams, sampleParams);

                session = O2GTransport.createSession();
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    O2GResponseType responseType = string.Equals(sampleParams.TableType, SampleParams.OrdersTable) == true ?
                                                    O2GResponseType.GetOrders : O2GResponseType.GetTrades;

                    responseListener = new ResponseListener(responseType);
                    session.subscribeResponse(responseListener);
                    O2GAccountRow account = GetAccount(session);
                    if (account != null)
                    {
                        if (responseType == O2GResponseType.GetOrders)
                        {                            
                            PrintOrders(session, account.AccountID, responseListener);
                        }
                        else
                        {
                            PrintTrades(session, account.AccountID, responseListener);
                        }
                        Console.WriteLine("Done!");
                    }
                    else
                    {
                        throw new Exception("No valid accounts");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                if (session != null)
                {
                    if (statusListener.Connected)
                    {
                        if (responseListener != null)
                            session.unsubscribeResponse(responseListener);
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
        /// Print accounts and get the first account
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static O2GAccountRow GetAccount(O2GSession session)
        {
            O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
            if (readerFactory == null)
            {
                throw new Exception("Cannot create response reader factory");
            }
            O2GLoginRules loginRules = session.getLoginRules();
            O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.Accounts);
            O2GAccountsTableResponseReader accountsResponseReader = readerFactory.createAccountsTableReader(response);
            for (int i = 0; i < accountsResponseReader.Count; i++)
            {
                O2GAccountRow accountRow = accountsResponseReader.getRow(i);
                Console.WriteLine("AccountID: {0}, Balance: {1}", accountRow.AccountID, accountRow.Balance);
            }
            return accountsResponseReader.getRow(0);
        }

        /// <summary>
        /// Print orders table for account
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="responseListener"></param>
        private static void PrintOrders(O2GSession session, string sAccountID, ResponseListener responseListener)
        {
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.Orders, sAccountID);
            if (request != null)
            {
                Console.WriteLine("Orders table for account {0}", sAccountID);
                responseListener.SetRequestID(request.RequestID);
                session.sendRequest(request);
                if (!responseListener.WaitEvents())
                {
                    throw new Exception("Response waiting timeout expired");
                }
                O2GResponse response = responseListener.GetResponse();
                if (response != null)
                {
                    O2GResponseReaderFactory responseReaderFactory = session.getResponseReaderFactory();
                    O2GOrdersTableResponseReader responseReader = responseReaderFactory.createOrdersTableReader(response);
                    for (int i = 0; i < responseReader.Count; i++)
                    {
                        O2GOrderRow orderRow = responseReader.getRow(i);
                        Console.WriteLine("OrderID: {0}, Status: {1}, Amount: {2}", orderRow.OrderID, orderRow.Status, orderRow.Amount);
                    }
                }
                else
                {
                    throw new Exception("Cannot get response");
                }
            }
            else
            {
                throw new Exception("Cannot create request");
            }
        }

        /// <summary>
        /// Print trades table for account
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="responseListener"></param>
        private static void PrintTrades(O2GSession session, string sAccountID, ResponseListener responseListener)
        {
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.Trades, sAccountID);
            if (request != null)
            {
                Console.WriteLine("Trades table for account {0}", sAccountID);
                responseListener.SetRequestID(request.RequestID);
                session.sendRequest(request);
                if (!responseListener.WaitEvents())
                {
                    throw new Exception("Response waiting timeout expired");
                }
                O2GResponse response = responseListener.GetResponse();
                if (response != null)
                {
                    O2GResponseReaderFactory responseReaderFactory = session.getResponseReaderFactory();
                    O2GTradesTableResponseReader responseReader = responseReaderFactory.createTradesTableReader(response);
                    for (int i = 0; i < responseReader.Count; i++)
                    {
                        O2GTradeRow tradeRow = responseReader.getRow(i);
                        Console.WriteLine("TradeID: {0}, Amount: {1}, Dividends: {2}", tradeRow.TradeID, tradeRow.Amount, tradeRow.Dividends);
                    }
                }
                else
                {
                    throw new Exception("Cannot get response");
                }
            }
            else
            {
                throw new Exception("Cannot create request");
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
            Console.WriteLine("{0} : Table='{1}'", procName, prm.TableType);
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
            public static readonly string OrdersTable = "orders";
            public static readonly string TradesTable = "trades";

            public string TableType
            {
                get
                {
                    return mTableType;
                }
            }
            private string mTableType;

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="args"></param>
            public SampleParams(NameValueCollection args)
            {
                mTableType = args["Table"];
                if (string.IsNullOrEmpty(mTableType) ||
                    !mTableType.Equals(OrdersTable) &&
                    !mTableType.Equals(TradesTable))
                {
                    mTableType = TradesTable; // default
                }
            }
        }
    }
}

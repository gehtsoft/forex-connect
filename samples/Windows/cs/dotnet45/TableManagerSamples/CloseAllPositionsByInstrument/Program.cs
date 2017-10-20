using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Configuration;
using fxcore2;

namespace CloseAllPositionsByInstrument
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

                PrintSampleParams("CloseAllPositionsByInstrument", loginParams, sampleParams);

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
                    sampleParams.AccountID = account.AccountID;

                    O2GOfferRow offer = GetOffer(tableManager, sampleParams.Instrument);
                    if (offer == null)
                    {
                        throw new Exception(string.Format("The instrument '{0}' is not valid", sampleParams.Instrument));
                    }

                    CloseOrdersData closeOrdersData;
                    if (!GetCloseOrdersData(tableManager, sampleParams.AccountID, offer.OfferID, out closeOrdersData))
                    {
                        throw new Exception("There are no opened positions");
                    }

                    tableListener.SubscribeEvents(tableManager);

                    O2GRequest request = CreateCloseMarketNettingOrderRequest(session, closeOrdersData);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request; probably some arguments are missing or incorrect");
                    }
                    List<string> requestIDs = new List<string>();
                    for (int i = 0; i < request.ChildrenCount; i++)
                    {
                        requestIDs.Add(request.getChildRequest(i).RequestID);
                    }
                    responseListener.SetRequestIDs(requestIDs);
                    tableListener.SetRequestIDs(requestIDs);
                    session.sendRequest(request);
                    if (responseListener.WaitEvents())
                    {
                        Thread.Sleep(1000); // Wait for the balance update
                        Console.WriteLine("Done!");
                    }
                    else
                    {
                        throw new Exception("Response waiting timeout expired");
                    }

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
        /// Get orders data for closing all positions
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="responseListener"></param>
        /// <returns></returns>
        private static bool GetCloseOrdersData(O2GTableManager tableManager, string sAccountID, string sOfferID, out CloseOrdersData closeOrdersData)
        {
            bool bIsTradesFound = false;
            closeOrdersData = new CloseOrdersData();
            O2GTradesTable tradesTable = (O2GTradesTable)tableManager.getTable(O2GTableType.Trades);
            for (int i = 0; i < tradesTable.Count; i++)
            {
                O2GTradeRow trade = tradesTable.getRow(i);

                if (!trade.OfferID.Equals(sOfferID))
                {
                    continue;
                }
                bIsTradesFound = true;
                string sBuySell = trade.BuySell;
                // Set opposite side
                OrderSide side = (sBuySell.Equals(Constants.Buy) ? OrderSide.Sell : OrderSide.Buy);
                if (closeOrdersData.OfferID.Equals(sOfferID))
                {
                    OrderSide currentSide = closeOrdersData.Side;
                    if (currentSide != OrderSide.Both && currentSide != side)
                    {
                        closeOrdersData.Side = OrderSide.Both;
                    }
                }
                else
                {
                    closeOrdersData.OfferID = sOfferID;
                    closeOrdersData.AccountID = sAccountID;
                    closeOrdersData.Side = side;
                }
            }
            return bIsTradesFound;
        }

        /// <summary>
        /// Create close all order request
        /// </summary>
        private static O2GRequest CreateCloseMarketNettingOrderRequest(O2GSession session, CloseOrdersData closeOrdersData)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            O2GValueMap batchValuemap = requestFactory.createValueMap();
            batchValuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);

            string sOfferID = closeOrdersData.OfferID;
            string sAccountID = closeOrdersData.AccountID;
            OrderSide side = closeOrdersData.Side;
            O2GValueMap childValuemap;
            switch (side)
            {
                case OrderSide.Buy:
                    childValuemap = requestFactory.createValueMap();
                    childValuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
                    childValuemap.setString(O2GRequestParamsEnum.NetQuantity, "Y");
                    childValuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketClose);
                    childValuemap.setString(O2GRequestParamsEnum.AccountID, sAccountID);
                    childValuemap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
                    childValuemap.setString(O2GRequestParamsEnum.BuySell, Constants.Buy);
                    batchValuemap.appendChild(childValuemap);
                    break;
                case OrderSide.Sell:
                    childValuemap = requestFactory.createValueMap();
                    childValuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
                    childValuemap.setString(O2GRequestParamsEnum.NetQuantity, "Y");
                    childValuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketClose);
                    childValuemap.setString(O2GRequestParamsEnum.AccountID, sAccountID);
                    childValuemap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
                    childValuemap.setString(O2GRequestParamsEnum.BuySell, Constants.Sell);
                    batchValuemap.appendChild(childValuemap);
                    break;
                case OrderSide.Both:
                    childValuemap = requestFactory.createValueMap();
                    childValuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
                    childValuemap.setString(O2GRequestParamsEnum.NetQuantity, "Y");
                    childValuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketClose);
                    childValuemap.setString(O2GRequestParamsEnum.AccountID, sAccountID);
                    childValuemap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
                    childValuemap.setString(O2GRequestParamsEnum.BuySell, Constants.Buy);
                    batchValuemap.appendChild(childValuemap);

                    childValuemap = requestFactory.createValueMap();
                    childValuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
                    childValuemap.setString(O2GRequestParamsEnum.NetQuantity, "Y");
                    childValuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketClose);
                    childValuemap.setString(O2GRequestParamsEnum.AccountID, sAccountID);
                    childValuemap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
                    childValuemap.setString(O2GRequestParamsEnum.BuySell, Constants.Sell);
                    batchValuemap.appendChild(childValuemap);
                    break;
            }

            request = requestFactory.createOrderRequest(batchValuemap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }

        /// <summary>
        /// Store the data to create netting close order per instrument
        /// </summary>
        class CloseOrdersData
        {
            public string AccountID;
            public string OfferID;
            public OrderSide Side;
            public CloseOrdersData()
            {
                AccountID = string.Empty;
                OfferID = string.Empty;
            }
        }

        enum OrderSide
        {
            Buy, Sell, Both
        }

        /// <summary>
        /// Print process name and sample parameters
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="loginPrm"></param>
        /// <param name="prm"></param>
        private static void PrintSampleParams(string procName, LoginParams loginPrm, SampleParams prm)
        {
            Console.WriteLine("{0}: Instrument='{1}', AccountID='{2}'", procName, prm.Instrument, prm.AccountID);
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

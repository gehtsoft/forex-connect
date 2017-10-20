using System;
using System.Collections.Generic;
using System.Threading;
using fxcore2;
using ArgParser;

namespace CloseAllPositions
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;
            ResponseListener responseListener = null;

            try
            {
                Console.WriteLine("CloseAllPositions sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "CloseAllPositions");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.AccountID);

                argParser.ParseArguments();

                if (!argParser.AreArgumentsValid)
                {
                    argParser.PrintUsage();
                    return;
                }

                argParser.PrintArguments();

                LoginParams loginParams = argParser.LoginParams;
                SampleParams sampleParams = argParser.SampleParams;

                session = O2GTransport.createSession();
                session.useTableManager(O2GTableManagerMode.Yes, null);
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    responseListener = new ResponseListener(session);
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

                    Dictionary<string, CloseOrdersData> closeOrdersData = GetCloseOrdersData(tableManager, sampleParams.AccountID);
                    if (closeOrdersData.Values.Count == 0)
                    {
                        throw new Exception("There are no opened positions");
                    }

                    tableListener.SubscribeEvents(tableManager);

                    O2GRequest request = CreateCloseAllRequest(session, closeOrdersData);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
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
                        Console.WriteLine("Done!");
                    }
                    else
                    {
                        throw new Exception("Response waiting timeout expired");
                    }

                    tableListener.UnsubscribeEvents(tableManager);
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
                        statusListener.Reset();
                        session.logout();
                        statusListener.WaitEvents();
                        if (responseListener != null)
                            session.unsubscribeResponse(responseListener);
                        session.unsubscribeSessionStatus(statusListener);
                    }
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
        /// <param name="tableManager"></param>
        /// <param name="sAccountID"></param>
        /// <returns></returns>
        private static Dictionary<string, CloseOrdersData> GetCloseOrdersData(O2GTableManager tableManager, string sAccountID)
        {
            Dictionary<string, CloseOrdersData> closeOrdersData = new Dictionary<string, CloseOrdersData>();
            O2GTradeRow trade = null;
            O2GTradesTable tradesTable = (O2GTradesTable)tableManager.getTable(O2GTableType.Trades);
            for (int i = 0; i < tradesTable.Count; i++)
            {
                trade = tradesTable.getRow(i);
                string sOfferID = trade.OfferID;
                string sBuySell = trade.BuySell;
                // Set opposite side
                OrderSide side = (sBuySell.Equals(Constants.Buy) ? OrderSide.Sell : OrderSide.Buy);

                if (closeOrdersData.ContainsKey(sOfferID))
                {
                    OrderSide currentSide = closeOrdersData[sOfferID].Side;
                    if (currentSide != OrderSide.Both && currentSide != side)
                    {
                        closeOrdersData[sOfferID].Side = OrderSide.Both;
                    }
                }
                else
                {
                    CloseOrdersData data = new CloseOrdersData(sAccountID, side);
                    closeOrdersData.Add(sOfferID, data);
                }
            }
            return closeOrdersData;
        }

        /// <summary>
        /// Create close all order request
        /// </summary>
        private static O2GRequest CreateCloseAllRequest(O2GSession session, Dictionary<string, CloseOrdersData> closeOrdersData)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            O2GValueMap batchValuemap = requestFactory.createValueMap();
            batchValuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            Dictionary<string, CloseOrdersData>.Enumerator enumerator = closeOrdersData.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string sOfferID = enumerator.Current.Key;
                string sAccountID = enumerator.Current.Value.AccountID;
                OrderSide side = enumerator.Current.Value.Side;
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
            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="sAccountID"></param>
            /// <param name="side"></param>
            public CloseOrdersData(string sAccountID, OrderSide side)
            {
                mAccountID = sAccountID;
                mSide = side;
            }

            public string AccountID
            {
                get { return mAccountID; }
            }
            private string mAccountID;

            public OrderSide Side
            {
                get { return mSide; }
                set { mSide = value; }
            }
            private OrderSide mSide;
        }

        enum OrderSide
        {
            Buy, Sell, Both
        }
    }
}

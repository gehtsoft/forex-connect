using System;
using System.Collections.Generic;
using System.Threading;
using fxcore2;
using ArgParser;

namespace CloseAllPositionsByInstrument
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
                Console.WriteLine("CloseAllPositionsByInstrument sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "CloseAllPositionsByInstrument");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.Instrument,
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
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    responseListener = new ResponseListener(session);
                    session.subscribeResponse(responseListener);

                    O2GAccountRow account = GetAccount(session, sampleParams.AccountID);
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

                    O2GOfferRow offer = GetOffer(session, sampleParams.Instrument);
                    if (offer == null)
                    {
                        throw new Exception(string.Format("The instrument '{0}' is not valid", sampleParams.Instrument));
                    }

                    CloseOrdersData closeOrdersData;
                    if (!GetCloseOrdersData(session, sampleParams.AccountID, offer.OfferID, responseListener, out closeOrdersData))
                    {
                        throw new Exception("There are no opened positions");
                    }

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
                        session.unsubscribeSessionStatus(statusListener);
                    }
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Find valid account by ID or get the first valid account
        /// </summary>
        /// <param name="session"></param>
        /// <returns>account</returns>
        private static O2GAccountRow GetAccount(O2GSession session, string sAccountID)
        {
            O2GAccountRow account = null;
            bool bHasAccount = false;
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
                account = accountsResponseReader.getRow(i);
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
        /// <param name="session"></param>
        /// <param name="sInstrument"></param>
        /// <returns>offer</returns>
        private static O2GOfferRow GetOffer(O2GSession session, string sInstrument)
        {
            O2GOfferRow offer = null;
            bool bHasOffer = false;
            O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
            if (readerFactory == null)
            {
                throw new Exception("Cannot create response reader factory");
            }
            O2GLoginRules loginRules = session.getLoginRules();
            O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.Offers);
            O2GOffersTableResponseReader offersResponseReader = readerFactory.createOffersTableReader(response);
            for (int i = 0; i < offersResponseReader.Count; i++)
            {
                offer = offersResponseReader.getRow(i);
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
        /// <param name="sOfferID"></param>
        /// <param name="responseListener"></param>
        /// <returns></returns>
        private static bool GetCloseOrdersData(O2GSession session, string sAccountID, string sOfferID, ResponseListener responseListener, out CloseOrdersData closeOrdersData)
        {
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.Trades, sAccountID);
            responseListener.SetRequestID(request.RequestID);
            session.sendRequest(request);
            if (!responseListener.WaitEvents())
            {
                throw new Exception("Response waiting timeout expired");
            }
            O2GResponse response = responseListener.GetResponse();
            bool bIsTradesFound = false;
            closeOrdersData = new CloseOrdersData();
            if (response != null)
            {
                O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
                if (readerFactory != null)
                {
                    O2GTradesTableResponseReader tradesResponseReader = readerFactory.createTradesTableReader(response);
                    for (int i = 0; i < tradesResponseReader.Count; i++)
                    {
                        O2GTradeRow trade = tradesResponseReader.getRow(i);
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
    }
}

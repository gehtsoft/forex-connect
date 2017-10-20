using System;
using ArgParser;
using fxcore2;

namespace CreateAndFindEntry
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
                Console.WriteLine("CreateAndFindEntry sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "CreateAndFindEntry");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.Instrument,
                                       ParserArgument.BuySell,
                                       ParserArgument.OrderType,
                                       ParserArgument.Lots,
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

                    O2GLoginRules loginRules = session.getLoginRules();
                    if (loginRules == null)
                    {
                        throw new Exception("Cannot get login rules");
                    }
                    O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(sampleParams.Instrument, account);
                    int iAmount = iBaseUnitSize * sampleParams.Lots;
                    double dRate = CalculateRate(sampleParams.OrderType, sampleParams.BuySell, offer.Ask, offer.Bid, offer.PointSize);
                    O2GRequest request = CreateEntryOrderRequest(session, offer.OfferID, account.AccountID, iAmount, dRate, sampleParams.BuySell, sampleParams.OrderType);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request; probably some arguments are missing or incorrect");
                    }
                    responseListener.SetRequestID(request.RequestID);
                    session.sendRequest(request);
                    if (responseListener.WaitEvents())
                    {
                        string sOrderID = responseListener.GetOrderID();
                        if (!string.IsNullOrEmpty(sOrderID))
                        {
                            Console.WriteLine("You have successfully created an entry order for instrument {0}", sampleParams.Instrument);
                            Console.WriteLine("Your order ID is {0}", sOrderID);
                            FindOrder(session, sOrderID, sampleParams.AccountID, responseListener);
                            Console.WriteLine("Done!");
                        }
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
        /// For the purpose of this example we will place entry order 10 pips from the current market price
        /// </summary>
        /// <param name="sOrderType"></param>
        /// <param name="sBuySell"></param>
        /// <param name="dAsk"></param>
        /// <param name="dBid"></param>
        /// <param name="dPointSize"></param>
        /// <returns>rate</returns>
        private static double CalculateRate(string sOrderType, string sBuySell, double dAsk, double dBid, double dPointSize)
        {
            double dRate = 0D;
            if (sOrderType.Equals(Constants.Orders.LimitEntry))
            {
                if (sBuySell.Equals(Constants.Buy))
                {
                    dRate = dAsk - 10 * dPointSize;
                }
                else
                {
                    dRate = dBid + 10 * dPointSize;
                }
            }
            else
            {
                if (sBuySell.Equals(Constants.Buy))
                {
                    dRate = dAsk + 10 * dPointSize;
                }
                else
                {
                    dRate = dBid - 10 * dPointSize;
                }
            }
            return dRate;
        }

        /// <summary>
        /// Find valid account
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
        /// Create entry order request
        /// </summary>
        private static O2GRequest CreateEntryOrderRequest(O2GSession session, string sOfferID, string sAccountID, int iAmount, double dRate, string sBuySell, string sOrderType)
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
            valuemap.setString(O2GRequestParamsEnum.CustomID, "EntryOrder");
            request = requestFactory.createOrderRequest(valuemap);
            return request;
        }

        /// <summary>
        /// Find order by ID and print information about it
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sOrderID"></param>
        /// <param name="sAccountID"></param>
        /// <param name="responseListener"></param>
        private static void FindOrder(O2GSession session, string sOrderID, string sAccountID, ResponseListener responseListener)
        {
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.Orders, sAccountID);
            responseListener.SetRequestID(request.RequestID);
            session.sendRequest(request);
            if (!responseListener.WaitEvents())
            {
                throw new Exception("Response waiting timeout expired");
            }
            O2GResponse response = responseListener.GetResponse();
            if (response != null)
            {
                O2GResponseReaderFactory responseFactory = session.getResponseReaderFactory();
                O2GOrdersTableResponseReader ordersReader = responseFactory.createOrdersTableReader(response);
                for (int i = 0; i < ordersReader.Count; i++)
                {
                    O2GOrderRow order = ordersReader.getRow(i);
                    if (sOrderID.Equals(order.OrderID))
                    {
                        Console.WriteLine("Information for OrderID = {0}", sOrderID);
                        Console.WriteLine("Account: {0}", order.AccountID);
                        Console.WriteLine("Amount: {0}", order.Amount);
                        Console.WriteLine("Rate: {0}", order.Rate);
                        Console.WriteLine("Type: {0}", order.Type);
                        Console.WriteLine("Buy/Sell: {0}", order.BuySell);
                        Console.WriteLine("Stage: {0}", order.Stage);
                        Console.WriteLine("Status: {0}", order.Status);
                    }
                }
            }
            else
            {
                throw new Exception("Cannot get response");
            }
        }        
    }
}

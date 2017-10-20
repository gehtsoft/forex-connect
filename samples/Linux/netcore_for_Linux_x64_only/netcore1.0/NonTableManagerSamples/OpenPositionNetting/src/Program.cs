using System;
using ArgParser;
using System.Threading;
using fxcore2;

namespace OpenPositionNetting
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
                Console.WriteLine("OpenPositionNetting sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "OpenPositionNetting");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.Instrument,
                                       ParserArgument.BuySell,
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

                    O2GTradesTableResponseReader tradesTable = GetTradesTable(session, sampleParams.AccountID, responseListener);
                    if (tradesTable == null)
                    {
                        throw new Exception("Cannot get trades table");
                    }
                    responseListener.SetTradesTable(tradesTable);

                    O2GLoginRules loginRules = session.getLoginRules();
                    if (loginRules == null)
                    {
                        throw new Exception("Cannot get login rules");
                    }
                    O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(sampleParams.Instrument, account);
                    int iAmount = iBaseUnitSize * sampleParams.Lots;

                    O2GRequest request = CreateTrueMarketOrderRequest(session, offer.OfferID, sampleParams.AccountID, iAmount, sampleParams.BuySell);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }
                    responseListener.SetRequestID(request.RequestID);
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
                    }
                    session.unsubscribeSessionStatus(statusListener);
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
                if (account.MaintenanceType.Equals("0"))  // netting account
                {
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
        /// Get initial Trades state
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="responseListener"></param>
        /// <returns>TradesTable</returns>
        private static O2GTradesTableResponseReader GetTradesTable(O2GSession session, string sAccountID, ResponseListener responseListener)
        {
            O2GTradesTableResponseReader tradesTable = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GRequest refreshTrades = requestFactory.createRefreshTableRequestByAccount(O2GTableType.Trades, sAccountID);
            responseListener.SetRequestID(refreshTrades.RequestID);
            session.sendRequest(refreshTrades);
            if (!responseListener.WaitEvents())
            {
                throw new Exception("Response waiting timeout expired");
            }
            O2GResponse tradeResponse = responseListener.GetResponse();
            if (tradeResponse != null)
            {
                O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
                if (readerFactory != null)
                {
                    tradesTable = readerFactory.createTradesTableReader(tradeResponse);
                }
            }
            return tradesTable;
        }

        /// <summary>
        /// Create true market order request
        /// </summary>
        private static O2GRequest CreateTrueMarketOrderRequest(O2GSession session, string sOfferID, string sAccountID, int iAmount, string sBuySell)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valuemap = requestFactory.createValueMap();
            valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketOpen);
            valuemap.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemap.setString(O2GRequestParamsEnum.BuySell, sBuySell);
            valuemap.setInt(O2GRequestParamsEnum.Amount, iAmount);
            valuemap.setString(O2GRequestParamsEnum.CustomID, "TrueMarketOrder");
            request = requestFactory.createOrderRequest(valuemap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }

      
    }
}

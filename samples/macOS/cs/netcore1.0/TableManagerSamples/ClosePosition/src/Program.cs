using System;
using System.Threading;
using ArgParser;
using fxcore2;

namespace ClosePosition
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
                Console.WriteLine("ClosePosition sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "ClosePosition");

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
                    responseListener = new ResponseListener();
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

                    O2GTradeRow trade = GetTrade(tableManager, sampleParams.AccountID, offer.OfferID);
                    if (trade == null)
                    {
                        throw new Exception(string.Format("There are no opened positions for instrument '{0}'", sampleParams.Instrument));
                    }

                    tableListener.SubscribeEvents(tableManager);

                    O2GRequest request = CreateCloseMarketOrderRequest(session, sampleParams.Instrument, trade);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }
                    responseListener.SetRequestID(request.RequestID);
                    tableListener.SetRequestID(request.RequestID);
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
        /// Find the first opened position by AccountID and OfferID
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="sOfferID"></param>
        /// <param name="responseListener"></param>
        /// <returns></returns>
        private static O2GTradeRow GetTrade(O2GTableManager tableManager, string sAccountID, string sOfferID)
        {
            bool bHasTrade = false;
            O2GTradeRow trade = null;
            O2GTradesTable tradesTable = (O2GTradesTable)tableManager.getTable(O2GTableType.Trades);
            for (int i = 0; i < tradesTable.Count; i++)
            {
                trade = tradesTable.getRow(i);
                if (trade.AccountID.Equals(sAccountID) && trade.OfferID.Equals(sOfferID))
                {
                    bHasTrade = true;
                    break;
                }
            }
            if (!bHasTrade)
            {
                return null;
            }
            else
            {
                return trade;
            }
        }

        /// <summary>
        /// Create close market order request
        /// </summary>
        private static O2GRequest CreateCloseMarketOrderRequest(O2GSession session, string sInstrument, O2GTradeRow tradeRow)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            O2GLoginRules loginRules = session.getLoginRules();
            O2GPermissionChecker permissionChecker = loginRules.getPermissionChecker();

            O2GValueMap valuemap = requestFactory.createValueMap();
            valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);

            if (permissionChecker.canCreateMarketCloseOrder(sInstrument) != O2GPermissionStatus.PermissionEnabled)
            {
                valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketOpen); // in USA you need to use "OM" to close a position.
            }
            else
            {
                valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketClose);
                valuemap.setString(O2GRequestParamsEnum.TradeID, tradeRow.TradeID);
            }

            valuemap.setString(O2GRequestParamsEnum.AccountID, tradeRow.AccountID);
            valuemap.setString(O2GRequestParamsEnum.OfferID, tradeRow.OfferID);
            valuemap.setString(O2GRequestParamsEnum.BuySell, tradeRow.BuySell.Equals(Constants.Buy) ? Constants.Sell : Constants.Buy);
            valuemap.setInt(O2GRequestParamsEnum.Amount, tradeRow.Amount);
            valuemap.setString(O2GRequestParamsEnum.CustomID, "CloseMarketOrder");
            request = requestFactory.createOrderRequest(valuemap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }
    }
}

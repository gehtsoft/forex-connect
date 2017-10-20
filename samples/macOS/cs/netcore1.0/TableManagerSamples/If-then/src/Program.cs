using System;
using System.Collections.Generic;
using ArgParser;
using System.Threading;
using fxcore2;

namespace IfThen
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
                Console.WriteLine("IfThen sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "IfThen");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.Instrument,
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

                    O2GLoginRules loginRules = session.getLoginRules();
                    if (loginRules == null)
                    {
                        throw new Exception("Cannot get login rules");
                    }

                    O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(sampleParams.Instrument, account);
                    int iAmount = iBaseUnitSize * sampleParams.Lots;

                    // For the purpose of this example we will place IF-THEN order 30 pips below the current market price
                    // and our stop entry order 15 pips below the current market price
                    double dRateIf = offer.Ask - 30.0 * offer.PointSize;
                    double dRateStop = offer.Ask - 15.0 * offer.PointSize;

                    tableListener.SubscribeEvents(tableManager);

                    O2GRequest request = CreateIfThenRequest(session, offer.OfferID, account.AccountID, iAmount, dRateIf, dRateStop);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }

                    List<string> requestIDList = new List<string>();
                    for (int i = 0; i < request.ChildrenCount; i++)
                    {
                        requestIDList.Add(request.getChildRequest(i).RequestID);
                    }
                    responseListener.SetRequestIDs(requestIDList);
                    tableListener.SetRequestIDs(requestIDList);
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
        /// Create If-Then request
        /// </summary>
        private static O2GRequest CreateIfThenRequest(O2GSession session, string sOfferID, string sAccountID, int iAmount, double dRateIf, double dRateStop)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valuemapMain = requestFactory.createValueMap();
            valuemapMain.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOTO);

            // ValueMap for if-then
            O2GValueMap valuemapIf = requestFactory.createValueMap();
            valuemapIf.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemapIf.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.StopEntry);
            valuemapIf.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemapIf.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemapIf.setString(O2GRequestParamsEnum.BuySell, Constants.Sell);
            valuemapIf.setInt(O2GRequestParamsEnum.Amount, 0);
            valuemapIf.setDouble(O2GRequestParamsEnum.Rate, dRateIf);
            valuemapMain.appendChild(valuemapIf);

            // ValueMap for stop entry
            O2GValueMap valuemapStop = requestFactory.createValueMap();
            valuemapStop.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemapStop.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.StopEntry);
            valuemapStop.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemapStop.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemapStop.setString(O2GRequestParamsEnum.BuySell, Constants.Buy);
            valuemapStop.setInt(O2GRequestParamsEnum.Amount, iAmount);
            valuemapStop.setDouble(O2GRequestParamsEnum.Rate, dRateStop);
            valuemapMain.appendChild(valuemapStop);

            request = requestFactory.createOrderRequest(valuemapMain);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }
    }
}

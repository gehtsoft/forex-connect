using System;
using ArgParser;
using fxcore2;

namespace SubscriptionStatus
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
                Console.WriteLine("SubscriptionStatus sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "SubscriptionStatus");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.Instrument,
                                       ParserArgument.Status,
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
                    responseListener = new ResponseListener();
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

                    O2GRequest request = CreateSetSubscriptionStatusRequest(session, offer.OfferID, sampleParams.Status, responseListener);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }
                    responseListener.SetRequestID(request.RequestID);
                    session.sendRequest(request);
                    if (!responseListener.WaitEvents())
                    {
                        throw new Exception("Response waiting timeout expired");
                    }

                    O2GResponse response = responseListener.GetResponse();
                    if (response != null && response.Type == O2GResponseType.CommandResponse)
                    {
                        Console.WriteLine("Subscription status for '{0}' is set to '{1}'", sampleParams.Instrument, sampleParams.Status);
                    }

                    PrintMargins(session, account, offer);
                    UpdateMargins(session, responseListener);
                    PrintMargins(session, account, offer);
                    Console.WriteLine("Done!");
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
                    }
                    session.unsubscribeSessionStatus(statusListener);
                    session.Dispose();
                }
            }
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
        /// Print offers and find offer by instrument name
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sInstrument"></param>
        /// <returns>offer</returns>
        private static O2GOfferRow GetOffer(O2GSession session, string sInstrument)
        {
            O2GOfferRow offer = null;
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
                O2GOfferRow offerRow = offersResponseReader.getRow(i);
                if (offerRow.Instrument.Equals(sInstrument))
                {
                    offer = offerRow;
                }
                switch (offerRow.SubscriptionStatus)
                {
                    case Constants.SubscriptionStatuses.ViewOnly:
                        Console.WriteLine("{0} : [V]iew only", offerRow.Instrument);
                        break;
                    case Constants.SubscriptionStatuses.Disable:
                        Console.WriteLine("{0} : [D]isabled", offerRow.Instrument);
                        break;
                    case Constants.SubscriptionStatuses.Tradable:
                        Console.WriteLine("{0} : Available for [T]rade", offerRow.Instrument);
                        break;
                    default:
                        Console.WriteLine("{0} : {1}", offerRow.Instrument, offerRow.SubscriptionStatus);
                        break;
                }
            }
            return offer;
        }

        /// <summary>
        /// Subscribe or unsubscribe an instrument
        /// </summary>
        private static O2GRequest CreateSetSubscriptionStatusRequest(O2GSession session, string sOfferID, string sStatus, ResponseListener responseListener)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.SetSubscriptionStatus);
            valueMap.setString(O2GRequestParamsEnum.SubscriptionStatus, sStatus);
            valueMap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            request = requestFactory.createOrderRequest(valueMap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }

        /// <summary>
        /// Get and print margin requirements
        /// </summary>
        /// <param name="session"></param>
        /// <param name="account"></param>
        /// <param name="offer"></param>
        private static void PrintMargins(O2GSession session, O2GAccountRow account, O2GOfferRow offer)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            if (loginRules == null)
            {
                throw new Exception("Cannot get login rules");
            }
            O2GTradingSettingsProvider tradingSettings = loginRules.getTradingSettingsProvider();
            double dMmr = 0D;
            double dEmr = 0D;
            double lmr = 0D;
            tradingSettings.getMargins(offer.Instrument, account, ref dMmr, ref dEmr, ref lmr);
            Console.WriteLine("Margin requirements: mmr={0}, emr={1}, lmr={2}", dMmr, dEmr, lmr);
        }

        /// <summary>
        /// Update margin requirements
        /// </summary>
        /// <param name="session"></param>
        /// <param name="responseListener"></param>
        private static void UpdateMargins(O2GSession session, ResponseListener responseListener)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.UpdateMarginRequirements);
            request = requestFactory.createOrderRequest(valueMap);
            responseListener.SetRequestID(request.RequestID);
            session.sendRequest(request);
            if (!responseListener.WaitEvents())
            {
                throw new Exception("Response waiting timeout expired");
            }
            O2GResponse response = responseListener.GetResponse();
            if (response != null && response.Type == O2GResponseType.MarginRequirementsResponse)
            {
                O2GResponseReaderFactory responseFactory = session.getResponseReaderFactory();
                if (responseFactory != null)
                {
                    responseFactory.processMarginRequirementsResponse(response);
                    Console.WriteLine("Margin requirements have been updated");
                }
            }
        }
    }
}

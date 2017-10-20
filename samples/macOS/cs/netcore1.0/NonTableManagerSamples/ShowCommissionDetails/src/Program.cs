using System;
using ArgParser;
using fxcore2;

namespace ShowCommissionDetails
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("ShowCommissionDetails sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "ShowCommissionDetails");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.AccountID,
                                       ParserArgument.BuySell,
                                       ParserArgument.Instrument);
                                      
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
                statusListener = new SessionStatusListener(session, loginParams);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    PrintCommissionDetails(session, sampleParams);
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
                    }
                    session.unsubscribeSessionStatus(statusListener);
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Print accounts table
        /// </summary>
        /// <param name="session"></param>
        private static void PrintCommissionDetails(O2GSession session, SampleParams sampleParams)
        {
            //wait until commissions related information will be loaded
            O2GCommissionsProvider commissionProvider = session.getCommissionsProvider();
            while (commissionProvider.GetStatus() == O2GCommissionStatus.CommissionStatusLoading)
                System.Threading.Thread.Sleep(1000);

            if (commissionProvider.GetStatus() != O2GCommissionStatus.CommissionStatusReady)
                throw new Exception("Could not calculate the estimated commissions."); 

            //retrieve the required parameter values from the sampleParams
            O2GAccountRow account = GetAccount(session, sampleParams.AccountID);
            O2GOfferRow offer = GetOffer(session, sampleParams.Instrument);
             if (offer == null || account == null)
                 throw new Exception("Incorrect input parameters.");

            O2GCommissionDescriptionsCollection commList = commissionProvider.GetCommissionDescriptions(offer.OfferID, account.ATPID);
            foreach (O2GCommissionDescription descr in commList)
            {                
                Console.WriteLine("Commission : {0}, {1}, {2}", descr.Stage, descr.UnitType, descr.CommissionValue);
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
                if (true)  // not netting account
                {
                    if (true)
                    {
                        if (true)
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
    }
}

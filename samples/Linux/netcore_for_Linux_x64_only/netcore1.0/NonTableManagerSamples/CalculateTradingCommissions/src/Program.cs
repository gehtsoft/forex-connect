using System;
using ArgParser;
using System.Threading;
using fxcore2;

namespace CalculateTradingCommissions
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;
            
            try
            {
                Console.WriteLine("CalculateTradingCommissions sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "CalculateTradingCommissions");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.BuySell,
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
                statusListener = new SessionStatusListener(session, loginParams);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
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
                        throw new Exception(string.IsNullOrEmpty(sampleParams.AccountID) ? "No valid accounts" : string.Format("The account '{0}' is not valid", sampleParams.AccountID));

                    O2GOfferRow offer = GetOffer(tableManager, sampleParams.Instrument);
                    if (offer == null)                    
                        throw new Exception(string.Format("The instrument '{0}' is not valid", sampleParams.Instrument));                    

                    O2GLoginRules loginRules = session.getLoginRules();
                    if (loginRules == null)                    
                        throw new Exception("Cannot get login rules");
                    
                    O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(sampleParams.Instrument, account);
                    int iAmount = iBaseUnitSize * sampleParams.Lots;


                    printEstimatedTradingCommissions(session, offer, account, iAmount, sampleParams.BuySell);

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
                        session.unsubscribeSessionStatus(statusListener);
                    }
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Print estimates trading commissions
        /// </summary>      
        private static void printEstimatedTradingCommissions(O2GSession session, O2GOfferRow offer, O2GAccountRow account, int iAmount, string sBuySell)
        {   
            //wait until commissions related information will be loaded
            O2GCommissionsProvider commissionProvider = session.getCommissionsProvider();
            while (commissionProvider.GetStatus() == O2GCommissionStatus.CommissionStatusLoading)
                System.Threading.Thread.Sleep(1000);

            if (commissionProvider.GetStatus() != O2GCommissionStatus.CommissionStatusReady)
                throw new Exception("Could not calculate the estimated commissions.");

            var a = offer.InstrumentType;

            //calculate commissions
            Console.WriteLine("Commission for open the position is {0}.", commissionProvider.CalcOpenCommission(offer, account, iAmount, sBuySell, 0));
            Console.WriteLine("Commission for close the position is {0}.", commissionProvider.CalcCloseCommission(offer, account, iAmount, sBuySell, 0));
            Console.WriteLine("Total commission for open and close the position is {0}.", commissionProvider.CalcTotalCommission(offer, account, iAmount, sBuySell, 0, 0));
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
    }
}

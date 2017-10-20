using System;
using System.Collections.Specialized;
using System.Threading;
using fxcore2;
using System.Collections.Generic;
using ArgParser;

namespace PrintRollover
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("PrintRollover sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "PrintRollover");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Instrument,
                                       ParserArgument.AccountID,
                                       ParserArgument.Pin);

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
                
                O2GRolloverProvider rolloverProvider = session.getRolloverProvider();

                AutoResetEvent autoEvent = new AutoResetEvent(false);
                RolloverProviderListener rolloverProviderListener = new RolloverProviderListener(autoEvent);

                rolloverProvider.subscribe(rolloverProviderListener);
                
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

                    if (autoEvent.WaitOne(10000)) //wait 10s
                    {
                        PrintRollover(rolloverProvider, account, offer);
                    }
                    else
                    {
                        Console.WriteLine("Waiting time expired: Rollover is not avaliavle");
                    }

                    rolloverProvider.unsubscribe(rolloverProviderListener);
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

        static private void PrintRollover(O2GRolloverProvider rolloverProvider, O2GAccountRow account, O2GOfferRow offer)
        {
            double rolloverBuy = rolloverProvider.getRolloverBuy(offer, account);
            double rolloverSell = rolloverProvider.getRolloverSell(offer, account);

            string rolloverInfo = string.Format("Rollover: {0} (buy), {1} (sell)", rolloverBuy, rolloverSell);
            Console.WriteLine(rolloverInfo);
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
                if (!account.MaintenanceType.Equals("0"))  // not netting account
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

        class RolloverProviderListener : IO2GRolloverProviderListener
        {
            AutoResetEvent mAutoEvent;
            public RolloverProviderListener(AutoResetEvent autoEvent)
            {
                mAutoEvent = autoEvent;
            }
            public void onStatusChanged(O2GRolloverStatus status)
            {
                if (status == O2GRolloverStatus.RolloverReady)
                {
                    mAutoEvent.Set();
                }
                else if (status == O2GRolloverStatus.FailToLoad)
                {
                    mAutoEvent.Set();
                }
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

using System;
using ArgParser;
using System.Threading;
using fxcore2;

namespace SearchInTable
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("SearchInTable sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "SearchInTable");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.OrderID,
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
                    else
                    {
                        if(!account.AccountID.Equals(sampleParams.AccountID))
                        {
                            sampleParams.AccountID = account.AccountID;
                            Console.WriteLine("AccountID='{0}'",
                                    sampleParams.AccountID);
                        }
                    }

                    FindOrder(tableManager, sampleParams.AccountID, sampleParams.OrderID);

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
        /// Find order by id and print it
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="sOrderID"></param>
        /// <param name="responseListener"></param>
        private static void FindOrder(O2GTableManager tableManager, string sAccountID, string sOrderID)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)tableManager.getTable(O2GTableType.Orders);
            O2GOrderTableRow orderRow = null;
            ordersTable.findRow(sOrderID, out orderRow);
            if (orderRow != null && sAccountID.Equals(orderRow.AccountID))
            {
                Console.WriteLine("OrderID={0}; AccountID={1}; Type={2}; Status={3}; OfferID={4}; Amount={5}; BuySell={6}; Rate={7}",
                        orderRow.OrderID, orderRow.AccountID, orderRow.Type, orderRow.Status, orderRow.OfferID,
                        orderRow.Amount, orderRow.BuySell, orderRow.Rate);
            }
            else
            {
                Console.WriteLine("Order '{0}' is not found", sOrderID);
            }
        }
    }
}

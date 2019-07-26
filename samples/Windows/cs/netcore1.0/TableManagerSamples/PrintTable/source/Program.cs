using System;
using ArgParser;
using fxcore2;
using System.Threading;

namespace PrintTable
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("PrintTable sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "PrintTable");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.TableType);

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

                    O2GAccountRow account = GetAccount(tableManager);
                    if (account == null)
                        throw new Exception("No valid accounts");


                    O2GResponseType responseType = string.Equals(sampleParams.TableType, SampleParams.OrdersTable) == true ?
                                O2GResponseType.GetOrders : O2GResponseType.GetTrades;

                    if (responseType == O2GResponseType.GetOrders)
                    {
                        PrintOrders(tableManager, account.AccountID);
                    }
                    else
                    {
                        PrintTrades(tableManager, account.AccountID);
                    }


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
        /// Print accounts and get the first account
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static O2GAccountRow GetAccount(O2GTableManager tableManager)
        {
            O2GAccountsTable accountsTable = (O2GAccountsTable)tableManager.getTable(O2GTableType.Accounts);
            O2GTableIterator accountsIterator = new O2GTableIterator();
            O2GAccountTableRow accountRow = null;
            accountsTable.getNextRow(accountsIterator, out accountRow);
            while (accountRow != null)
            {
                Console.WriteLine("AccountID: {0}, Balance: {1}", accountRow.AccountID, accountRow.Balance);
                accountsTable.getNextRow(accountsIterator, out accountRow);
            }
            return accountsTable.getRow(0);
        }

        // Print orders table using IO2GEachRowListener
        public static void PrintOrders(O2GTableManager tableManager, string sAccountID)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)tableManager.getTable(O2GTableType.Orders);
            if (ordersTable.Count == 0)
            {
                Console.WriteLine("Table is empty!");
            }
            else
            {
                ordersTable.forEachRow(new EachRowListener(sAccountID));
            }
        }

        // Print orders table using IO2GEachRowListener
        public static void PrintTrades(O2GTableManager tableManager, string sAccountID)
        {
            O2GTradesTable tradesTable = (O2GTradesTable)tableManager.getTable(O2GTableType.Trades);
            if (tradesTable.Count == 0)
            {
                Console.WriteLine("Table is empty!");
            }
            else
            {
                tradesTable.forEachRow(new EachRowListener(sAccountID));
            }
        }
    }
}

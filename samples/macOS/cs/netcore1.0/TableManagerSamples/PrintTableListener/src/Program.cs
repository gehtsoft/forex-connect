using System;
using ArgParser;
using fxcore2;

namespace PrintTableListener
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                ArgumentParser argParser = new ArgumentParser(args, "PrintTableListener");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin);

                argParser.ParseArguments();

                if (!argParser.AreArgumentsValid)
                {
                    argParser.PrintUsage();
                    return;
                }

                argParser.PrintArguments();

                LoginParams loginParams = argParser.LoginParams;

                session = O2GTransport.createSession();
                TableManagerListener managerListener = new TableManagerListener();
                session.useTableManager(O2GTableManagerMode.Yes, managerListener);
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    O2GTableManager tableManager = session.getTableManager();
                    if (managerListener.WaitEvents() && managerListener.IsLoaded)
                    {
                        Console.WriteLine("All tables are loaded");
                    }
                    else
                    {
                        throw new Exception("Cannot refresh all tables of table manager");
                    }

                    O2GAccountRow account = GetAccount(tableManager);
                    if (account == null)
                        throw new Exception("No valid accounts");

                    managerListener.Reset();
                    O2GTableManager tableManagerByAccount = session.getTableManagerByAccount(account.AccountID);
                    if (managerListener.WaitEvents() && managerListener.IsLoaded)
                    {
                        Console.WriteLine(string.Format("All tables are loaded (account {0})", account.AccountID));
                    }
                    else
                    {
                        throw new Exception(string.Format("Cannot refresh all tables of table manager (account {0})", account.AccountID));
                    }

                    PrintOrders(tableManagerByAccount, account.AccountID);
                    Console.WriteLine("Done!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Exception: {0}", e.ToString()));
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
    }
}

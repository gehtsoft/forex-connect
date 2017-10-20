using System;
using ArgParser;
using fxcore2;

namespace PrintTable
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
                Console.WriteLine("PrintTable sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "PrintTable");

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
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    responseListener = new ResponseListener();
                    session.subscribeResponse(responseListener);
                    O2GAccountRow account = GetAccount(session);
                    if (account != null)
                    {
                        PrintOrders(session, account.AccountID, responseListener);
                        Console.WriteLine("Done!");
                    }
                    else
                    {
                        throw new Exception("No valid accounts");
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
        /// Print accounts and get the first account
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static O2GAccountRow GetAccount(O2GSession session)
        {
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
                O2GAccountRow accountRow = accountsResponseReader.getRow(i);
                Console.WriteLine("AccountID: {0}, Balance: {1}", accountRow.AccountID, accountRow.Balance);
            }
            return accountsResponseReader.getRow(0);
        }

        /// <summary>
        /// Print orders table for account
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="responseListener"></param>
        private static void PrintOrders(O2GSession session, string sAccountID, ResponseListener responseListener)
        {
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.Orders, sAccountID);
            if (request != null)
            {
                Console.WriteLine("Orders table for account {0}", sAccountID);
                responseListener.SetRequestID(request.RequestID);
                session.sendRequest(request);
                if (!responseListener.WaitEvents())
                {
                    throw new Exception("Response waiting timeout expired");
                }
                O2GResponse response = responseListener.GetResponse();
                if (response != null)
                {
                    O2GResponseReaderFactory responseReaderFactory = session.getResponseReaderFactory();
                    O2GOrdersTableResponseReader responseReader = responseReaderFactory.createOrdersTableReader(response);
                    for (int i = 0; i < responseReader.Count; i++)
                    {
                        O2GOrderRow orderRow = responseReader.getRow(i);
                        Console.WriteLine("OrderID: {0}, Status: {1}, Amount: {2}", orderRow.OrderID, orderRow.Status, orderRow.Amount);
                    }
                }
                else
                {
                    throw new Exception("Cannot get response");
                }
            }
            else
            {
                throw new Exception("Cannot create request");
            }
        }
    }
}

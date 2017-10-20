using System;
using System.Collections.Generic;
using ArgParser;
using fxcore2;

namespace JoinNewGroup
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;
            ResponseListener responseListener = null;
            int iContingencyGroupType = 1; // OCO group

            try
            {
                Console.WriteLine("JoinNewGroup sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "JoinNewGroup");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.PrimaryID,
                                       ParserArgument.SecondaryID,
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
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    responseListener = new ResponseListener(session);
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

                    O2GRequest request = JoinToNewGroupRequest(session, sampleParams.AccountID, sampleParams.PrimaryID, sampleParams.SecondaryID, iContingencyGroupType);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }

                    List<string> orderIDList = new List<string>();
                    orderIDList.Add(sampleParams.PrimaryID);
                    orderIDList.Add(sampleParams.SecondaryID);
                    foreach (string sOrderID in orderIDList)
                    {
                        if (!IsOrderExists(session, sampleParams.AccountID, sOrderID, responseListener))
                        {
                            throw new Exception(string.Format("Order '{0}' does not exist", sOrderID));
                        }
                    }
                    responseListener.SetOrderIDs(orderIDList);
                    session.sendRequest(request);
                    if (responseListener.WaitEvents())
                    {
                        Console.WriteLine("Done!");
                    }
                    else
                    {
                        throw new Exception("Response waiting timeout expired");
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
        /// Check if order exists
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sAccountID"></param>
        /// <param name="sOrderID"></param>
        /// <param name="responseListener"></param>
        /// <returns></returns>
        private static bool IsOrderExists(O2GSession session, string sAccountID, string sOrderID, ResponseListener responseListener)
        {
            bool bHasOrder = false;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GRequest request = requestFactory.createRefreshTableRequestByAccount(O2GTableType.Orders, sAccountID);
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
            O2GResponseReaderFactory responseReaderFactory = session.getResponseReaderFactory();
            O2GOrdersTableResponseReader responseReader = responseReaderFactory.createOrdersTableReader(response);
            for (int i = 0; i < responseReader.Count; i++)
            {
                O2GOrderRow orderRow = responseReader.getRow(i);
                if (sOrderID.Equals(orderRow.OrderID))
                {
                    bHasOrder = true;
                    break;
                }
            }
            return bHasOrder;
        }

        /// <summary>
        /// Create request for join two existing entry orders into a new contingency group
        /// </summary>
        private static O2GRequest JoinToNewGroupRequest(O2GSession session, string sAccountID, string sPrimaryID, string sSecondaryID, int iContingencyType)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valuemapMain = requestFactory.createValueMap();
            valuemapMain.setString(O2GRequestParamsEnum.Command, Constants.Commands.JoinToNewContingencyGroup);
            valuemapMain.setInt(O2GRequestParamsEnum.ContingencyGroupType, iContingencyType);

            O2GValueMap valuemapChild;

            valuemapChild = requestFactory.createValueMap();
            valuemapChild.setString(O2GRequestParamsEnum.OrderID, sPrimaryID);
            valuemapChild.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemapMain.appendChild(valuemapChild);

            valuemapChild = requestFactory.createValueMap();
            valuemapChild.setString(O2GRequestParamsEnum.OrderID, sSecondaryID);
            valuemapChild.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemapMain.appendChild(valuemapChild);

            request = requestFactory.createOrderRequest(valuemapMain);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }
    }
}

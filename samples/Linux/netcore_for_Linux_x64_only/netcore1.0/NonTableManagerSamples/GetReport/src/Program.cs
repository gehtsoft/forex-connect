using System;
using ArgParser;
using fxcore2;

namespace GetReport
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("GetReport sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "GetReport");

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
                    GetReports(session);
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
        /// Get reports for all accounts
        /// </summary>
        /// <param name="session"></param>
        public static void GetReports(O2GSession session)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            if (loginRules == null)
            {
                throw new Exception("Cannot get login rules");
            }
            O2GResponseReaderFactory responseFactory = session.getResponseReaderFactory();
            O2GResponse accountsResponse = loginRules.getTableRefreshResponse(O2GTableType.Accounts);
            O2GAccountsTableResponseReader accountsReader = responseFactory.createAccountsTableReader(accountsResponse);
            using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
            {
                for (int i = 0; i < accountsReader.Count; i++)
                {
                    O2GAccountRow account = accountsReader.getRow(i);
                    Uri url = new Uri(session.getReportURL(account.AccountID, DateTime.Now.AddMonths(-1), DateTime.Now, "html", null));

                    Console.WriteLine("AccountID={0}; Balance={1}; BaseUnitSize={2}; Report URL={3}",
                            account.AccountID, account.Balance, account.BaseUnitSize, url);

                    var response = httpClient.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content;
                        // by calling .Result you are synchronously reading the result
                        string content = responseContent.ReadAsStringAsync().Result;
                        string filename = account.AccountID + ".html";
                        string prefix = url.Scheme + "://" + url.Host + "/";
                        string report = O2GHtmlContentUtils.ReplaceRelativePathWithAbsolute(content, prefix);
                        System.IO.File.WriteAllText(filename, report);
                        Console.WriteLine("Report is saved to {0}", filename);
                    }
                    else
                    {
                        throw new Exception("Report is not received.");
                    }
                }
            }
        }
    }
}

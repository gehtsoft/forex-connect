using System;
using ArgParser;
using System.Threading;
using fxcore2;

namespace GetOffers
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
                Console.WriteLine("GetOffers sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "GetOffers");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
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
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    responseListener = new ResponseListener(session);
                    responseListener.SetInstrument(sampleParams.Instrument);
                    session.subscribeResponse(responseListener);

                    O2GLoginRules loginRules = session.getLoginRules();
                    if (loginRules == null)
                    {
                        throw new Exception("Cannot get login rules");
                    }

                    O2GResponse response;
                    if (loginRules.isTableLoadedByDefault(O2GTableType.Offers))
                    {
                        response = loginRules.getTableRefreshResponse(O2GTableType.Offers);
                        if (response != null)
                        {
                            responseListener.PrintOffers(session, response, null);
                        }
                    }
                    else
                    {
                        O2GRequestFactory requestFactory = session.getRequestFactory();
                        if (requestFactory != null)
                        {
                            O2GRequest offersRequest = requestFactory.createRefreshTableRequest(O2GTableType.Offers);
                            responseListener.SetRequestID(offersRequest.RequestID);
                            session.sendRequest(offersRequest);
                            if (!responseListener.WaitEvents())
                            {
                                throw new Exception("Response waiting timeout expired");
                            }
                            response = responseListener.GetResponse();
                            if (response != null)
                            {
                                responseListener.PrintOffers(session, response, null);
                            }
                        }
                    }

                    // Do nothing 10 seconds, let offers print
                    Thread.Sleep(10000);
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
                        if (responseListener != null)
                            session.unsubscribeResponse(responseListener);
                        statusListener.Reset();
                        session.logout();
                        statusListener.WaitEvents();
                    }
                    session.unsubscribeSessionStatus(statusListener);
                    session.Dispose();
                }
            }
        }
    }
}

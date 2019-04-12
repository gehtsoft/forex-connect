using System;
using ArgParser;
using fxcore2;

namespace Login
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;
            O2GSession secondSession = null;
            SessionStatusListener secondStatusListener = null;

            try
            {
                Console.WriteLine("Login sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "Login");

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
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin, "PrimarySessionStatus");
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    string token = session.getToken();
                    Console.WriteLine("Token obtained: {0}", token);
                    secondSession = O2GTransport.createSession();
                    secondStatusListener = new SessionStatusListener(secondSession, loginParams.SessionID, loginParams.Pin, "SecondarySessionStatus");
                    secondSession.subscribeSessionStatus(secondStatusListener);
                    secondStatusListener.Reset();
                    secondSession.loginWithToken(loginParams.Login, token, loginParams.URL, loginParams.Connection);
                    if (secondStatusListener.WaitEvents() && secondStatusListener.Connected)
                    {
                        Console.WriteLine("Done!");
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                if (secondSession != null)
                {
                    if (secondStatusListener.Connected)
                    {
                        secondStatusListener.Reset();
                        secondSession.logout();
                        secondStatusListener.WaitEvents();
                    }
                    secondSession.unsubscribeSessionStatus(secondStatusListener);
                    secondSession.Dispose();
                }
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
    }
}

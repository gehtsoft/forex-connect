using System;
using fxcore2;
using ArgParser;
using System.Threading;

namespace TwoConnections
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session1 = null;
            O2GSession session2 = null;

            try
            {

                Console.WriteLine("TwoConnections sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "TwoConnections");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.AccountID,
                                       ParserArgument.Login2,
                                       ParserArgument.Password2,
                                       ParserArgument.SessionID2,
                                       ParserArgument.Pin2,
                                       ParserArgument.AccountID2,
                                       ParserArgument.Instrument,
                                       ParserArgument.BuySell);

                argParser.ParseArguments();

                if (!argParser.AreArgumentsValid)
                {
                    argParser.PrintUsage();
                    return;
                }

                argParser.PrintArguments();

                LoginParams loginParams = argParser.LoginParams;
                SampleParams sampleParams = argParser.SampleParams;

                session1 = O2GTransport.createSession();
                session2 = O2GTransport.createSession();

                Connection connection1 = new Connection(session1, loginParams, sampleParams, true);
                Connection connection2 = new Connection(session2, loginParams, sampleParams, false);

                Thread thread1 = new Thread(connection1.Run);
                Thread thread2 = new Thread(connection2.Run);

                thread1.Start();
                thread2.Start();

                thread1.Join();
                thread2.Join();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
        }
    }
}

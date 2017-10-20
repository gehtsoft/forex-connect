using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Configuration;
using Common;
using fxcore2;

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
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);
                SampleParams sampleParams = new SampleParams(ConfigurationManager.AppSettings);

                PrintSampleParams("TwoConnections", loginParams, sampleParams);

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
            finally
            {
                if (session1 != null)
                {
                    session1.Dispose();
                }
                if (session2 != null)
                {
                    session2.Dispose();
                }
            }
        }

        /// <summary>
        /// Print process name and sample parameters
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="loginPrm"></param>
        /// <param name="prm"></param>
        private static void PrintSampleParams(string procName, LoginParams loginPrm, SampleParams prm)
        {
            Console.WriteLine("{0}: Instrument='{1}', BuySell='{2}', Lots='{3}', AccountID='{4}', AccountID2='{5}'",
                procName, prm.Instrument, prm.BuySell, prm.Lots, prm.AccountID, prm.AccountID2);
        }
    }
}

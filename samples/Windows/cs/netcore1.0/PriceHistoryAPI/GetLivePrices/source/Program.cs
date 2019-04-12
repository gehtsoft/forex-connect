/* Copyright 2019 FXCM Global Services, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use these files except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
using System;
using System.Collections.Specialized;
using System.Text;
using System.Globalization;

using fxcore2;
using Candleworks.PriceHistoryMgr;
using ArgParser;

namespace GetLivePrices
{
    // NOTE: the example doesn't handle the session reconnecting event.
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            IPriceHistoryCommunicator communicator = null;
            SessionStatusListener statusListener = null;
            bool loggedIn = false;

            try
            {
                Console.WriteLine("GetLivePrices sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "GetLivePrices");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.Instrument,
                                       ParserArgument.TimeFrame,
                                       ParserArgument.DateFrom,
                                       ParserArgument.DateTo,
                                       ParserArgument.QuotesCount,
                                       ParserArgument.OpenPriceCandlesMode);

                argParser.ParseArguments();

                if (!argParser.AreArgumentsValid)
                {
                    argParser.PrintUsage();
                    return;
                }

                argParser.PrintArguments();

                LoginParams loginParams = argParser.LoginParams;
                SampleParams sampleParams = argParser.SampleParams;

                // use the application module path as a base path for quotes storage
                string storagePath = System.IO.Path.Combine(AppContext.BaseDirectory, "History");

                // create the ForexConnect trading session
                session = O2GTransport.createSession();
                statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                // subscribe IO2GSessionStatus interface implementation for the status events
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();

                // create an instance of IPriceHistoryCommunicator
                communicator = PriceHistoryCommunicatorFactory.createCommunicator(session, storagePath);

                // log in to ForexConnect
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    loggedIn = true;

                    CommunicatorStatusListener communicatorStatusListener = new CommunicatorStatusListener();
                    communicator.addStatusListener(communicatorStatusListener);

                    // wait until the communicator signals that it is ready
                    if (communicator.isReady() ||
                        (communicatorStatusListener.WaitEvents() && communicatorStatusListener.Ready))
                    {
                        // attach the instance of the class that implements the IPriceHistoryCommunicatorListener
                        // interface to the communicator
                        ResponseListener responseListener = new ResponseListener();
                        communicator.addListener(responseListener);

                        GetLivePrices(communicator, sampleParams.Instrument, sampleParams.Timeframe, 
                            sampleParams.DateFrom, sampleParams.DateTo, sampleParams.QuotesCount, responseListener, 
                            session, statusListener);
                        Console.WriteLine("Done!");

                        communicator.removeListener(responseListener);
                    }

                    communicator.removeStatusListener(communicatorStatusListener);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                if (communicator != null)
                {
                    communicator.Dispose();
                }
                if (session != null)
                {
                    if (loggedIn)
                    {
                        try
                        {
                            statusListener.Reset();
                            session.logout();
                            statusListener.WaitEvents();
                        }
                        catch (Exception)
                        {
                        }
                    }

                    session.unsubscribeSessionStatus(statusListener);
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Request historical prices for the specified timeframe of the specified period
        /// and then show live prices.
        /// </summary>
        /// <param name="communicator">The price history communicator.</param>
        /// <param name="instrument">The instrument.</param>
        /// <param name="timeframe">The timeframe.</param>
        /// <param name="from">From-date.</param>
        /// <param name="to">To-date</param>
        /// <param name="quotesCount">The quotes count.</param>
        /// <param name="responseListener">The response listener.</param>
        /// <param name="session">The trading session.</param>
        /// <param name="sessionListener">The trading session listener.</param>
        public static void GetLivePrices(IPriceHistoryCommunicator communicator, string instrument, string timeframe, 
                                         DateTime from, DateTime to, int quotesCount, ResponseListener responseListener, 
                                         O2GSession session, SessionStatusListener sessionListener)
        {
            if (!communicator.isReady())
            {
                Console.WriteLine("History communicator is not ready.");
                return;
            }

            // create timeframe entity
            ITimeframeFactory timeframeFactory = communicator.TimeframeFactory;
            O2GTimeframe timeframeObj = timeframeFactory.create(timeframe);

            // check timeframe for ticks
            if (O2GTimeframeUnit.Tick == timeframeObj.Unit)
                throw new Exception("Application works only for bars. Don't use tick as timeframe.");

            // load Offers table and start ticks listening
            PriceUpdateController priceUpdateController = new PriceUpdateController(session, instrument);
            if (!priceUpdateController.Wait())
                return;

            // create period collection
            bool alive = true;
            PeriodCollection periods = new PeriodCollection(instrument, timeframe, alive, priceUpdateController);

            PeriodCollectionUpdateObserver livePriceViewer = new PeriodCollectionUpdateObserver(periods);

            // create and send a history request
            IPriceHistoryCommunicatorRequest request = communicator.createRequest(instrument, timeframeObj, from, to, quotesCount);
            responseListener.SetRequest(request);
            communicator.sendRequest(request);

            // wait results
            responseListener.Wait();

            IPriceHistoryCommunicatorResponse response = responseListener.GetResponse();
            O2GMarketDataSnapshotResponseReader reader = communicator.createResponseReader(response);

            if (response != null)
                ProcessHistoricalPrices(communicator, response, ref periods);

            // finally notify the collection that all bars are added, so it can
            // add all ticks collected while the request was being executed
            // and start update the data by forthcoming ticks
            periods.Finish(reader.getLastBarTime(), reader.getLastBarVolume());

            // continue update the data until cancelled by a user
            Console.WriteLine("\nPress ENTER to cancel.\n\n");
            Console.ReadKey();

            livePriceViewer.Unsubscribe();
            priceUpdateController.Unsubscribe();
        }

        /// <summary>
        /// Print history data from response and fills periods collection.
        /// </summary>
        /// <param name="communicator">The price history communicator.</param>
        /// <param name="response">The response. Cannot be null.</param>
        /// <param name="periods">The periods collection.</param>
        public static void ProcessHistoricalPrices(IPriceHistoryCommunicator communicator, IPriceHistoryCommunicatorResponse response, ref PeriodCollection periods)
        {
            // use O2GMarketDataSnapshotResponseReader to extract price data from the response object 
            O2GMarketDataSnapshotResponseReader reader = communicator.createResponseReader(response);
            for (int i = 0; i < reader.Count; i++)
            {
                if (reader.isBar)
                {
                    periods.Add(reader.getDate(i), reader.getBidOpen(i), reader.getBidHigh(i), reader.getBidLow(i), reader.getBidClose(i),
                        reader.getAskOpen(i), reader.getAskHigh(i), reader.getAskLow(i), reader.getAskClose(i), reader.getVolume(i));

                    Console.WriteLine("DateTime={0}, BidOpen={1}, BidHigh={2}, BidLow={3}, BidClose={4}, AskOpen={5}, AskHigh={6}, AskLow={7}, AskClose={8}, Volume={9}",
                        reader.getDate(i), reader.getBidOpen(i), reader.getBidHigh(i), reader.getBidLow(i), reader.getBidClose(i),
                        reader.getAskOpen(i), reader.getAskHigh(i), reader.getAskLow(i), reader.getAskClose(i), reader.getVolume(i));
                }
            }
        }

        private static void PrintSampleParams(string procName, LoginParams loginPrm, SampleParams prm)
        {
            Console.WriteLine("{0}: Instrument='{1}', Timeframe='{2}', DateFrom='{3}', QuotesCount='{4}'",
                procName, prm.Instrument, prm.Timeframe, prm.DateFrom.ToString("MM.dd.yyyy HH:mm:ss"), prm.QuotesCount);
        }
    }
}

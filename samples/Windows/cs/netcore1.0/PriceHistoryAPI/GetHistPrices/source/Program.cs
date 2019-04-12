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
using Candleworks.QuotesMgr;
using ArgParser;

namespace GetHistPrices
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            IPriceHistoryCommunicator communicator = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("GetHistPrices sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "GetHistPrices");

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
                    CommunicatorStatusListener communicatorStatusListener = new CommunicatorStatusListener();
                    communicator.addStatusListener(communicatorStatusListener);

                    // wait until the communicator signals that it is ready
                    if (communicator.isReady() ||
                        (communicatorStatusListener.WaitEvents() && communicatorStatusListener.Ready))
                    {
                        // set open price candles mode, it must be called after login
                        QuotesManager quotesManager = communicator.getQuotesManager();
                        quotesManager.openPriceCandlesMode = sampleParams.OpenPriceCandlesMode;

                        // attach the instance of the class that implements the IPriceHistoryCommunicatorListener
                        // interface to the communicator
                        ResponseListener responseListener = new ResponseListener();
                        communicator.addListener(responseListener);

                        GetHistoryPrices(communicator, sampleParams.Instrument, sampleParams.Timeframe, 
                            sampleParams.DateFrom, sampleParams.DateTo, sampleParams.QuotesCount, responseListener);
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
                    try
                    {
                        statusListener.Reset();
                        session.logout();
                        statusListener.WaitEvents();
                    }
                    catch (Exception)
                    {
                    }

                    session.unsubscribeSessionStatus(statusListener);
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Request historical prices for the specified timeframe of the specified period.
        /// </summary>
        /// <param name="communicator">The price history communicator.</param>
        /// <param name="instrument">The instrument.</param>
        /// <param name="timeframe">The timeframe.</param>
        /// <param name="from">From-date.</param>
        /// <param name="to">To-date</param>
        /// <param name="quotesCount">The quotes count.</param>
        /// <param name="responseListener">The response listener.</param>
        public static void GetHistoryPrices(IPriceHistoryCommunicator communicator, string instrument, string timeframe, 
                                            DateTime from, DateTime to, int quotesCount, ResponseListener responseListener)
        {
            if (!communicator.isReady())
            {
                Console.WriteLine("History communicator is not ready.");
                return;
            }

            // create timeframe entity
            ITimeframeFactory timeframeFactory = communicator.TimeframeFactory;
            O2GTimeframe timeframeObj = timeframeFactory.create(timeframe);

            // create and send a history request
            IPriceHistoryCommunicatorRequest request = communicator.createRequest(instrument, timeframeObj, from, to, quotesCount);
            responseListener.SetRequest(request);
            communicator.sendRequest(request);

            // wait results
            responseListener.Wait();

            // print results if any
            IPriceHistoryCommunicatorResponse response = responseListener.GetResponse();
            if (response != null)
                PrintPrices(communicator, response);
        }

        /// <summary>
        /// Print history data from response.
        /// </summary>
        /// <param name="communicator">The price history communicator.</param>
        /// <param name="response">The response. Cannot be null.</param>
        public static void PrintPrices(IPriceHistoryCommunicator communicator, IPriceHistoryCommunicatorResponse response)
        {
            // use O2GMarketDataSnapshotResponseReader to extract price data from the response object 
            O2GMarketDataSnapshotResponseReader reader = communicator.createResponseReader(response);
            for (int i = 0; i < reader.Count; i++)
            {
                if (reader.isBar)
                {
                    Console.WriteLine("DateTime={0}, BidOpen={1}, BidHigh={2}, BidLow={3}, BidClose={4}, AskOpen={5}, AskHigh={6}, AskLow={7}, AskClose={8}, Volume={9}",
                        reader.getDate(i), reader.getBidOpen(i), reader.getBidHigh(i), reader.getBidLow(i), reader.getBidClose(i),
                        reader.getAskOpen(i), reader.getAskHigh(i), reader.getAskLow(i), reader.getAskClose(i), reader.getVolume(i));
                }
                else
                {
                    Console.WriteLine("DateTime={0}, Bid={1}, Ask={2}", reader.getDate(i), reader.getBid(i), reader.getAsk(i));
                }
            }
        }
    }
}

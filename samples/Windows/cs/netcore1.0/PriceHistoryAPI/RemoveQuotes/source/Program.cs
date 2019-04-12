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
using System.Text;
using System.Collections.Generic;
using System.IO;

using fxcore2;
using Candleworks.QuotesMgr;
using System.Collections.Specialized;
using Candleworks.PriceHistoryMgr;
using ArgParser;

namespace RemoveQuotes
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
                Console.WriteLine("RemoveQuotes sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "RemoveQuotes");

                argParser.AddArguments(ParserArgument.Login,
                                       ParserArgument.Password,
                                       ParserArgument.Url,
                                       ParserArgument.Connection,
                                       ParserArgument.SessionID,
                                       ParserArgument.Pin,
                                       ParserArgument.Instrument,
                                       ParserArgument.Year);

                argParser.ParseArguments();

                if (!argParser.AreArgumentsValid)
                {
                    argParser.PrintUsage();
                    return;
                }

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

                        RemoveQuotes(quotesManager, sampleParams);
                        Console.WriteLine();
                        ShowLocalQuotes(quotesManager);
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
        /// Remove quotes.
        /// </summary>
        /// <param name="quotesManager">The QuotesManager instance</param>
        /// <param name="arguments">Parameters of remove command</param>
        static void RemoveQuotes(QuotesManager quotesManager, SampleParams sampleParams)
        {
            if (sampleParams.Year <= 0)
            {
                Console.WriteLine("Error : year must be positive integer value");
                return;
            }

            RemoveLocalQuotes(quotesManager, sampleParams.Instrument, sampleParams.Year);
        }

        /// <summary>
        /// Removes quotes from local cache.
        /// </summary>
        /// <param name="quotesManager">The QuotesManager instance</param>
        /// <param name="instrument">Instrument name</param>
        /// <param name="year">Year</param>
        static void RemoveLocalQuotes(QuotesManager quotesManager, string instrument, int year)
        {
            if (instrument == null)
            {
                throw new ArgumentNullException("instrument");
            }

            if (quotesManager == null)
            {
                Console.WriteLine("Failed to remove local quotes");
                return;
            }

            ICollection<IQMData> quotes = null;
            try
            {
                quotes = GetQuotes(quotesManager, instrument, year);
            }
            catch (Exception error)
            {
                Console.WriteLine("Failed to remove local quotes : {0}", error.Message);
                return;
            }

            if (quotes == null || quotes.Count == 0)
            {
                Console.WriteLine("There are no quotes to remove");
                return;
            }

            RemoveData(quotesManager, quotes);
        }

        /// <summary>
        /// Fills QuotesManager data for the specified instrument and year.
        /// </summary>
        /// <param name="quotesManager">QuotesManager instance</param>
        /// <param name="instrument">Instrument name</param>
        /// <param name="year">Year</param>
        /// <returns>Collection of quotes manager storage data</returns>
        static ICollection<IQMData> GetQuotes(QuotesManager quotesManager, string instrument, int year)
        {
            BaseTimeframes baseTimeframes = quotesManager.getBaseTimeframes();
            int timeframesCount = baseTimeframes.size();
            List<IQMData> quotes = new List<IQMData>(timeframesCount);

            for (int i = 0; i < timeframesCount; ++i)
            {
                string timeframe = baseTimeframes.get(i);
                long size = quotesManager.getDataSize(instrument, timeframe, year);
                if (size > 0)
                {
                    quotes.Add(new QMData(instrument, timeframe, year, size));
                }
            }

            return quotes;
        }

        /// <summary>
        /// Shows quotes that are available in local cache.
        /// </summary>
        /// <param name="quotesManager">QuotesManager instance</param>
        static void ShowLocalQuotes(QuotesManager quotesManager)
        {
            if (quotesManager == null)
            {
                Console.WriteLine("Failed to get local quotes");
                return;
            }

            // if the instruments list is not updated, update it now
            if (!quotesManager.areInstrumentsUpdated())
            {
                UpdateInstrumentsListener instrumentsCallback = new UpdateInstrumentsListener();
                UpdateInstrumentsTask task = quotesManager.createUpdateInstrumentsTask(instrumentsCallback);
                quotesManager.executeTask(task);
                instrumentsCallback.WaitEvents();
            }

            IQMDataCollection collection = PrepareListOfQMData(quotesManager);
            ShowPreparedQMDataList(collection);
        }

        /// <summary>
        /// Prepares the collection of the quotes stored in the Quotes Manager cache.
        /// </summary>
        ///<param name="quotesManager">QuotesManager instance</param>
        public static IQMDataCollection PrepareListOfQMData(QuotesManager quotesManager)
        {
            QMDataCollection collection = new QMDataCollection();
            BaseTimeframes timeframes = quotesManager.getBaseTimeframes();
            Instruments instruments = quotesManager.getInstruments();

            int instrumentCount = instruments.size();
            for (int i = 0; i < instrumentCount; i++)
            {
                Instrument instrument = instruments.get(i);
                int timeframeCount = timeframes.size();
                for (int j = 0; j < timeframeCount; j++)
                {
                    string timeframe = timeframes.get(j);
                    int y1 = instrument.getOldestQuoteDate(timeframe).Year;
                    int y2 = instrument.getLatestQuoteDate(timeframe).Year;
                    if (y2 >= y1)
                    {
                        for (int y = y1; y <= y2; y++)
                        {
                            long size = quotesManager.getDataSize(instrument.getName(), timeframe, y);
                            if (size > 0)
                                collection.Add(new QMData(instrument.getName(), timeframe, y, size));
                        }
                    }
                }
            }

            return collection;
        }

        /// <summary>
        /// Show list of available quotes.
        /// </summary>
        /// <param name="collection">Collection of quotes manager storage data</param>
        static void ShowPreparedQMDataList(IQMDataCollection collection)
        {
            IDictionary<string, IDictionary<int, long>> instrumentsInfo = SummariseInstrumentDataSize(collection);
            if (instrumentsInfo == null || instrumentsInfo.Count == 0)
            {
                Console.WriteLine("There are no quotes in the local storage.");
            }
            else
            {
                Console.WriteLine("Quotes in the local storage");

                foreach (string instrument in instrumentsInfo.Keys)
                {
                    IDictionary<int, long> sizeByYear = instrumentsInfo[instrument];
                    foreach (int year in sizeByYear.Keys)
                    {
                        Console.WriteLine("    {0} {1} {2}", instrument, year, ReadableSize(sizeByYear[year]));
                    }
                }
            }
        }

        /// <summary>
        /// Get string suffix according to number size (KB, MB, GB).
        /// </summary>
        /// <param name="bytes">Number</param>
        /// <returns>String suffix</returns>
        static string ReadableSize(long bytes)
        {
            const double byteConversion = 1024;
            int range = (int)Math.Truncate(Math.Log(bytes, byteConversion));

            switch (range)
            {
                case 3:
                    return Math.Round(bytes / Math.Pow(byteConversion, 3), 2) + " GB";
                case 2:
                    return Math.Round(bytes / Math.Pow(byteConversion, 2), 2) + " MB";
                case 1:
                    return Math.Round(bytes / byteConversion, 2) + " KB";
                default:
                    return bytes + " Bytes";
            }
        }

        /// <summary>
        /// Get full size of available instrument's quotes.
        /// </summary>
        /// <param name="collection">Collection of quotes manager storage data</param>
        /// <returns>Collection of info about quotes in starage</returns>
        static IDictionary<string, IDictionary<int, long>> SummariseInstrumentDataSize(IQMDataCollection collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return null;
            }

            IDictionary<string, IDictionary<int, long>> instrumentsInfo = new Dictionary<string, IDictionary<int, long>>();
            foreach (IQMData quote in collection)
            {
                string instrument = quote.Instrument;
                if (!instrumentsInfo.ContainsKey(instrument))
                {
                    instrumentsInfo[instrument] = new Dictionary<int, long>();
                }

                int year = quote.Year;
                if (!instrumentsInfo[instrument].ContainsKey(year))
                {
                    instrumentsInfo[instrument][year] = 0;
                }
                instrumentsInfo[instrument][year] += quote.Size;
            }

            return instrumentsInfo;
        }

        /// <summary>
        /// Removes the data from cache.
        /// </summary>
        /// <param name="quotesManager">The QuotesManager instance</param>
        /// <param name="list">Collection of quotes manager storage data.</param>
        public static void RemoveData(QuotesManager quotesManager, IEnumerable<IQMData> list)
        {
            Queue<RemoveQuotesTask> removeTasks = new Queue<RemoveQuotesTask>();
            RemoveQuotesListener removeQuotesListener = new RemoveQuotesListener();
            foreach (IQMData data in list)
            {
                RemoveQuotesTask task = quotesManager.createRemoveQuotesTask(data.Instrument, 
                    data.Timeframe, removeQuotesListener);
                task.addYear(data.Year);
                removeTasks.Enqueue(task);
            }

            while (removeTasks.Count > 0)
            {
                StartNextRemoveTask(quotesManager, removeTasks, removeQuotesListener);
            }
        }

        /// <summary>
        /// Tries to start next remove task.
        /// </summary>
        /// <param name="quotesManager">The QuotesManager instance</param>
        /// <param name="removeTasks">Collection of remove tasks</param>
        /// <param name="removeQuoteListener">Listener of remove quotes task</param>
        private static void StartNextRemoveTask(QuotesManager quotesManager, Queue<RemoveQuotesTask> removeTasks, RemoveQuotesListener removeQuoteListener)
        {
            RemoveQuotesTask removeTask = removeTasks.Dequeue();
            quotesManager.executeTask(removeTask);
            removeQuoteListener.WaitEvents();
        }
    }
}
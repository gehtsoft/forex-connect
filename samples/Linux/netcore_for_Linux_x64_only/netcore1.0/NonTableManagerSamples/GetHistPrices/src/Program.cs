using System;
using ArgParser;
using fxcore2;
using System.Globalization;

namespace GetHistPrices
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;
            ResponseListener responseListener  = null;

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
                                       ParserArgument.DateTo);

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
                    session.subscribeResponse(responseListener);
                    GetHistoryPrices(session, sampleParams.Instrument, sampleParams.Timeframe, sampleParams.DateFrom, sampleParams.DateTo, responseListener);
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
                        if (responseListener != null)
                            session.unsubscribeResponse(responseListener);
                    }
                    session.unsubscribeSessionStatus(statusListener);
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Request historical prices for the specified timeframe of the specified period
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sInstrument"></param>
        /// <param name="sTimeframe"></param>
        /// <param name="dtFrom"></param>
        /// <param name="dtTo"></param>
        /// <param name="responseListener"></param>
        public static void GetHistoryPrices(O2GSession session, string sInstrument, string sTimeframe, DateTime dtFrom, DateTime dtTo, ResponseListener responseListener)
        {
            O2GRequestFactory factory = session.getRequestFactory();
            O2GTimeframe timeframe = factory.Timeframes[sTimeframe];
            if (timeframe == null)
            {
                throw new Exception(string.Format("Timeframe '{0}' is incorrect!", sTimeframe));
            }
            O2GRequest request = factory.createMarketDataSnapshotRequestInstrument(sInstrument, timeframe, 300);
            DateTime dtFirst = dtTo;
            do // cause there is limit for returned candles amount
            {
                factory.fillMarketDataSnapshotRequestTime(request, dtFrom, dtFirst, false, O2GCandleOpenPriceMode.FirstTick);
                responseListener.SetRequestID(request.RequestID);
                session.sendRequest(request);
                if (!responseListener.WaitEvents())
                {
                    throw new Exception("Response waiting timeout expired");
                }
                // shift "to" bound to oldest datetime of returned data
                O2GResponse response = responseListener.GetResponse();
                if (response != null && response.Type == O2GResponseType.MarketDataSnapshot)
                {
                    O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
                    if (readerFactory != null)
                    {
                        O2GMarketDataSnapshotResponseReader reader = readerFactory.createMarketDataSnapshotReader(response);
                        if (reader.Count > 0)
                        {
                            if (DateTime.Compare(dtFirst, reader.getDate(0)) != 0)
                            {
                                dtFirst = reader.getDate(0); // earliest datetime of returned data
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("0 rows received");
                            break;
                        }
                    }
                    PrintPrices(session, response);
                }
                else
                {
                    break;
                }
            } while (dtFirst > dtFrom);
        }

        /// <summary>
        /// Print history data from response
        /// </summary>
        /// <param name="session"></param>
        /// <param name="response"></param>
        public static void PrintPrices(O2GSession session, O2GResponse response)
        {
            Console.WriteLine("Request with RequestID={0} is completed:", response.RequestID);
            O2GResponseReaderFactory factory = session.getResponseReaderFactory();
            if (factory != null)
            {
                O2GMarketDataSnapshotResponseReader reader = factory.createMarketDataSnapshotReader(response);
                for (int ii = reader.Count - 1; ii >= 0; ii--)
                {
                    if (reader.isBar)
                    {
                        Console.WriteLine("DateTime={0}, BidOpen={1}, BidHigh={2}, BidLow={3}, BidClose={4}, AskOpen={5}, AskHigh={6}, AskLow={7}, AskClose={8}, Volume={9}",
                                reader.getDate(ii), reader.getBidOpen(ii), reader.getBidHigh(ii), reader.getBidLow(ii), reader.getBidClose(ii),
                                reader.getAskOpen(ii), reader.getAskHigh(ii), reader.getAskLow(ii), reader.getAskClose(ii), reader.getVolume(ii));
                    }
                    else
                    {
                        Console.WriteLine("DateTime={0}, Bid={1}, Ask={2}", reader.getDate(ii), reader.getBidClose(ii), reader.getAskClose(ii));
                    }
                }
            }
        }
    }
}

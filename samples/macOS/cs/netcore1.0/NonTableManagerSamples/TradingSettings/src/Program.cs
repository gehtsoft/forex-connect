using System;
using ArgParser;
using fxcore2;

namespace TradingSettings
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("TradingSettings sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "TradingSettings");

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
                    PrintTradingSettings(session);
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

        // Print trading settings of the first account
        private static void PrintTradingSettings(O2GSession session)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            if (loginRules == null)
            {
                throw new Exception("Cannot get login rules");
            }
            O2GResponse accountsResponse = loginRules.getTableRefreshResponse(O2GTableType.Accounts);
            if (accountsResponse == null)
            {
                throw new Exception("Cannot get response");
            }
            O2GResponse offersResponse = loginRules.getTableRefreshResponse(O2GTableType.Offers);
            if (offersResponse == null)
            {
                throw new Exception("Cannot get response");
            }
            O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
            O2GResponseReaderFactory factory = session.getResponseReaderFactory();
            if (factory == null)
            {
                throw new Exception("Cannot create response reader factory");
            }
            O2GAccountsTableResponseReader accountsReader = factory.createAccountsTableReader(accountsResponse);
            O2GOffersTableResponseReader instrumentsReader = factory.createOffersTableReader(offersResponse);
            O2GAccountRow account = accountsReader.getRow(0);
            for (int i = 0; i < instrumentsReader.Count; i++)
            {
                O2GOfferRow instrumentRow = instrumentsReader.getRow(i);
                string instrument = instrumentRow.Instrument;
                int condDistStopForTrade = tradingSettingsProvider.getCondDistStopForTrade(instrument);
                int condDistLimitForTrade = tradingSettingsProvider.getCondDistLimitForTrade(instrument);
                int condDistEntryStop = tradingSettingsProvider.getCondDistEntryStop(instrument);
                int condDistEntryLimit = tradingSettingsProvider.getCondDistEntryLimit(instrument);
                int minQuantity = tradingSettingsProvider.getMinQuantity(instrument, account);
                int maxQuantity = tradingSettingsProvider.getMaxQuantity(instrument, account);
                int baseUnitSize = tradingSettingsProvider.getBaseUnitSize(instrument, account);
                O2GMarketStatus marketStatus = tradingSettingsProvider.getMarketStatus(instrument);
                int minTrailingStep = tradingSettingsProvider.getMinTrailingStep();
                int maxTrailingStep = tradingSettingsProvider.getMaxTrailingStep();
                double mmr = tradingSettingsProvider.getMMR(instrument, account);
                double mmr2=0, emr=0, lmr=0;
                bool threeLevelMargin = tradingSettingsProvider.getMargins(instrument, account, ref mmr2, ref emr, ref lmr);
                string sMarketStatus = "unknown";
                switch (marketStatus)
                {
                    case O2GMarketStatus.MarketStatusOpen:
                        sMarketStatus = "Market Open";
                        break;
                    case O2GMarketStatus.MarketStatusClosed:
                        sMarketStatus = "Market Close";
                        break;
                }
                Console.WriteLine("Instrument: {0}, Status: {1}", instrument, sMarketStatus);
                Console.WriteLine("Cond.Dist: ST={0}; LT={1}", condDistStopForTrade, condDistLimitForTrade);
                Console.WriteLine("Cond.Dist entry stop={0}; entry limit={1}", condDistEntryStop,
                        condDistEntryLimit);
                Console.WriteLine("Quantity: Min={0}; Max={1}. Base unit size={2}; MMR={3}", minQuantity,
                        maxQuantity, baseUnitSize, mmr);
                if (threeLevelMargin)
                {
                    Console.WriteLine("Three level margin: MMR={0}; EMR={1}; LMR={2}", mmr2, emr, lmr);
                }
                else
                {
                    Console.WriteLine("Single level margin: MMR={0}; EMR={1}; LMR={2}", mmr2, emr, lmr);
                }
                Console.WriteLine("Trailing step: {0}-{1}", minTrailingStep, maxTrailingStep);
            }
        }
    }
}

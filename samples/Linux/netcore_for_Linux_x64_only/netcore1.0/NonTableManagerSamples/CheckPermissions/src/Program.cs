using System;
using fxcore2;
using ArgParser;

namespace CheckPermissions
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;
            SessionStatusListener statusListener = null;

            try
            {
                Console.WriteLine("CheckPermissions sample\n");

                ArgumentParser argParser = new ArgumentParser(args, "CheckPermissions");

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
                    CheckPermissions(session, sampleParams.Instrument);
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
                        session.unsubscribeSessionStatus(statusListener);
                    }
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Show permissions for the particular instrument
        /// </summary>
        public static void CheckPermissions(O2GSession session, string sInstrument)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            O2GPermissionChecker permissionChecker = loginRules.getPermissionChecker();
            Console.WriteLine("canCreateMarketOpenOrder = {0}", permissionChecker.canCreateMarketOpenOrder(sInstrument));
            Console.WriteLine("canChangeMarketOpenOrder = {0}", permissionChecker.canChangeMarketOpenOrder(sInstrument));
            Console.WriteLine("canDeleteMarketOpenOrder = {0}", permissionChecker.canDeleteMarketOpenOrder(sInstrument));
            Console.WriteLine("canCreateMarketCloseOrder = {0}", permissionChecker.canCreateMarketCloseOrder(sInstrument));
            Console.WriteLine("canChangeMarketCloseOrder = {0}", permissionChecker.canChangeMarketCloseOrder(sInstrument));
            Console.WriteLine("canDeleteMarketCloseOrder = {0}", permissionChecker.canDeleteMarketCloseOrder(sInstrument));
            Console.WriteLine("canCreateEntryOrder = {0}", permissionChecker.canCreateEntryOrder(sInstrument));
            Console.WriteLine("canChangeEntryOrder = {0}", permissionChecker.canChangeEntryOrder(sInstrument));
            Console.WriteLine("canDeleteEntryOrder = {0}", permissionChecker.canDeleteEntryOrder(sInstrument));
            Console.WriteLine("canCreateStopLimitOrder = {0}", permissionChecker.canCreateStopLimitOrder(sInstrument));
            Console.WriteLine("canChangeStopLimitOrder = {0}", permissionChecker.canChangeStopLimitOrder(sInstrument));
            Console.WriteLine("canDeleteStopLimitOrder = {0}", permissionChecker.canDeleteStopLimitOrder(sInstrument));
            Console.WriteLine("canRequestQuote = {0}", permissionChecker.canRequestQuote(sInstrument));
            Console.WriteLine("canAcceptQuote = {0}", permissionChecker.canAcceptQuote(sInstrument));
            Console.WriteLine("canDeleteQuote = {0}", permissionChecker.canDeleteQuote(sInstrument));
            Console.WriteLine("canJoinToNewContingencyGroup = {0}", permissionChecker.canJoinToNewContingencyGroup(sInstrument));
            Console.WriteLine("canJoinToExistingContingencyGroup = {0}", permissionChecker.canJoinToExistingContingencyGroup(sInstrument));
            Console.WriteLine("canRemoveFromContingencyGroup = {0}", permissionChecker.canRemoveFromContingencyGroup(sInstrument));
            Console.WriteLine("canChangeOfferSubscription = {0}", permissionChecker.canChangeOfferSubscription(sInstrument));
            Console.WriteLine("canCreateNetCloseOrder = {0}", permissionChecker.canCreateNetCloseOrder(sInstrument));
            Console.WriteLine("canChangeNetCloseOrder = {0}", permissionChecker.canChangeNetCloseOrder(sInstrument));
            Console.WriteLine("canDeleteNetCloseOrder = {0}", permissionChecker.canDeleteNetCloseOrder(sInstrument));
            Console.WriteLine("canCreateNetStopLimitOrder = {0}", permissionChecker.canCreateNetStopLimitOrder(sInstrument));
            Console.WriteLine("canChangeNetStopLimitOrder = {0}", permissionChecker.canChangeNetStopLimitOrder(sInstrument));
            Console.WriteLine("canDeleteNetStopLimitOrder = {0}", permissionChecker.canDeleteNetStopLimitOrder(sInstrument));
            Console.WriteLine("canUseDynamicTrailingForStop = {0}", permissionChecker.canUseDynamicTrailingForStop());
            Console.WriteLine("canUseDynamicTrailingForLimit = {0}", permissionChecker.canUseDynamicTrailingForLimit());
            Console.WriteLine("canUseDynamicTrailingForEntryStop = {0}", permissionChecker.canUseDynamicTrailingForEntryStop());
            Console.WriteLine("canUseDynamicTrailingForEntryLimit = {0}", permissionChecker.canUseDynamicTrailingForEntryLimit());
            Console.WriteLine("canUseFluctuateTrailingForStop = {0}", permissionChecker.canUseFluctuateTrailingForStop());
            Console.WriteLine("canUseFluctuateTrailingForLimit = {0}", permissionChecker.canUseFluctuateTrailingForLimit());
            Console.WriteLine("canUseFluctuateTrailingForEntryStop = {0}", permissionChecker.canUseFluctuateTrailingForEntryStop());
            Console.WriteLine("canUseFluctuateTrailingForEntryLimit = {0}", permissionChecker.canUseFluctuateTrailingForEntryLimit());
        }
    }
}

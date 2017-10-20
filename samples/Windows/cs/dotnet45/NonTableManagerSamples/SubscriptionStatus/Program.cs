using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Configuration;
using fxcore2;

namespace SubscriptionStatus
{
    class Program
    {
        static void Main(string[] args)
        {
            O2GSession session = null;

            try
            {
                LoginParams loginParams = new LoginParams(ConfigurationManager.AppSettings);
                SampleParams sampleParams = new SampleParams(ConfigurationManager.AppSettings);

                PrintSampleParams("SubscriptionStatus", loginParams, sampleParams);

                session = O2GTransport.createSession();
                SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    ResponseListener responseListener = new ResponseListener();
                    session.subscribeResponse(responseListener);

                    O2GAccountRow account = GetAccount(session, sampleParams.AccountID);
                    if (account == null)
                    {
                        if (string.IsNullOrEmpty(sampleParams.AccountID))
                        {
                            throw new Exception("No valid accounts");
                        }
                        else
                        {
                            throw new Exception(string.Format("The account '{0}' is not valid", sampleParams.AccountID));
                        }
                    }
                    sampleParams.AccountID = account.AccountID;

                    O2GOfferRow offer = GetOffer(session, sampleParams.Instrument);
                    if (offer == null)
                    {
                        throw new Exception(string.Format("The instrument '{0}' is not valid", sampleParams.Instrument));
                    }

                    O2GRequest request = CreateSetSubscriptionStatusRequest(session, offer.OfferID, sampleParams.Status, responseListener);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }
                    responseListener.SetRequestID(request.RequestID);
                    session.sendRequest(request);
                    if (!responseListener.WaitEvents())
                    {
                        throw new Exception("Response waiting timeout expired");
                    }

                    O2GResponse response = responseListener.GetResponse();
                    if (response != null && response.Type == O2GResponseType.CommandResponse)
                    {
                        Console.WriteLine("Subscription status for '{0}' is set to '{1}'", sampleParams.Instrument, sampleParams.Status);
                    }

                    PrintMargins(session, account, offer);
                    UpdateMargins(session, responseListener);
                    PrintMargins(session, account, offer);
                    Console.WriteLine("Done!");

                    statusListener.Reset();
                    session.logout();
                    statusListener.WaitEvents();
                    session.unsubscribeResponse(responseListener);
                }
                session.unsubscribeSessionStatus(statusListener);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                if (session != null)
                {
                    session.Dispose();
                }
            }
        }

        /// <summary>
        /// Find valid account
        /// </summary>
        /// <param name="session"></param>
        /// <returns>account</returns>
        private static O2GAccountRow GetAccount(O2GSession session, string sAccountID)
        {
            O2GAccountRow account = null;
            bool bHasAccount = false;
            O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
            if (readerFactory == null)
            {
                throw new Exception("Cannot create response reader factory");
            }
            O2GLoginRules loginRules = session.getLoginRules();
            O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.Accounts);
            O2GAccountsTableResponseReader accountsResponseReader = readerFactory.createAccountsTableReader(response);
            for (int i = 0; i < accountsResponseReader.Count; i++)
            {
                account = accountsResponseReader.getRow(i);
                string sAccountKind = account.AccountKind;
                if (sAccountKind.Equals("32") || sAccountKind.Equals("36"))
                {
                    if (account.MarginCallFlag.Equals("N"))
                    {
                        if (string.IsNullOrEmpty(sAccountID) || sAccountID.Equals(account.AccountID))
                        {
                            bHasAccount = true;
                            break;
                        }
                    }
                }
            }
            if (!bHasAccount)
            {
                return null;
            }
            else
            {
                return account;
            }
        }

        /// <summary>
        /// Print offers and find offer by instrument name
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sInstrument"></param>
        /// <returns>offer</returns>
        private static O2GOfferRow GetOffer(O2GSession session, string sInstrument)
        {
            O2GOfferRow offer = null;
            O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
            if (readerFactory == null)
            {
                throw new Exception("Cannot create response reader factory");
            }
            O2GLoginRules loginRules = session.getLoginRules();
            O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.Offers);
            O2GOffersTableResponseReader offersResponseReader = readerFactory.createOffersTableReader(response);

            for (int i = 0; i < offersResponseReader.Count; i++)
            {
                O2GOfferRow offerRow = offersResponseReader.getRow(i);
                if (offerRow.Instrument.Equals(sInstrument))
                {
                    offer = offerRow;
                }
                switch (offerRow.SubscriptionStatus)
                {
                    case Constants.SubscriptionStatuses.ViewOnly:
                        Console.WriteLine("{0} : [V]iew only", offerRow.Instrument);
                        break;
                    case Constants.SubscriptionStatuses.Disable:
                        Console.WriteLine("{0} : [D]isabled", offerRow.Instrument);
                        break;
                    case Constants.SubscriptionStatuses.Tradable:
                        Console.WriteLine("{0} : Available for [T]rade", offerRow.Instrument);
                        break;
                    default:
                        Console.WriteLine("{0} : {1}", offerRow.Instrument, offerRow.SubscriptionStatus);
                        break;
                }
            }
            return offer;
        }

        /// <summary>
        /// Subscribe or unsubscribe an instrument
        /// </summary>
        private static O2GRequest CreateSetSubscriptionStatusRequest(O2GSession session, string sOfferID, string sStatus, ResponseListener responseListener)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.SetSubscriptionStatus);
            valueMap.setString(O2GRequestParamsEnum.SubscriptionStatus, sStatus);
            valueMap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            request = requestFactory.createOrderRequest(valueMap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }

        /// <summary>
        /// Get and print margin requirements
        /// </summary>
        /// <param name="session"></param>
        /// <param name="account"></param>
        /// <param name="offer"></param>
        private static void PrintMargins(O2GSession session, O2GAccountRow account, O2GOfferRow offer)
        {
            O2GLoginRules loginRules = session.getLoginRules();
            if (loginRules == null)
            {
                throw new Exception("Cannot get login rules");
            }
            O2GTradingSettingsProvider tradingSettings = loginRules.getTradingSettingsProvider();
            double dMmr = 0D;
            double dEmr = 0D;
            double lmr = 0D;
            tradingSettings.getMargins(offer.Instrument, account, ref dMmr, ref dEmr, ref lmr);
            Console.WriteLine("Margin requirements: mmr={0}, emr={1}, lmr={2}", dMmr, dEmr, lmr);
        }

        /// <summary>
        /// Update margin requirements
        /// </summary>
        /// <param name="session"></param>
        /// <param name="responseListener"></param>
        private static void UpdateMargins(O2GSession session, ResponseListener responseListener)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valueMap = requestFactory.createValueMap();
            valueMap.setString(O2GRequestParamsEnum.Command, Constants.Commands.UpdateMarginRequirements);
            request = requestFactory.createOrderRequest(valueMap);
            responseListener.SetRequestID(request.RequestID);
            session.sendRequest(request);
            if (!responseListener.WaitEvents())
            {
                throw new Exception("Response waiting timeout expired");
            }
            O2GResponse response = responseListener.GetResponse();
            if (response != null && response.Type == O2GResponseType.MarginRequirementsResponse)
            {
                O2GResponseReaderFactory responseFactory = session.getResponseReaderFactory();
                if (responseFactory != null)
                {
                    responseFactory.processMarginRequirementsResponse(response);
                    Console.WriteLine("Margin requirements have been updated");
                }
            }
        }

        /// <summary>
        /// Print process name and sample parameters
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="loginPrm"></param>
        private static void PrintSampleParams(string procName, LoginParams loginPrm, SampleParams prm)
        {
            Console.WriteLine("{0}: Instrument='{1}', Status='{2}', AccountID='{3}'", procName, prm.Instrument, prm.Status, prm.AccountID);
        }

        class LoginParams
        {
            public string Login
            {
                get
                {
                    return mLogin;
                }
            }
            private string mLogin;

            public string Password
            {
                get
                {
                    return mPassword;
                }
            }
            private string mPassword;

            public string URL
            {
                get
                {
                    return mURL;
                }
            }
            private string mURL;

            public string Connection
            {
                get
                {
                    return mConnection;
                }
            }
            private string mConnection;

            public string SessionID
            {
                get
                {
                    return mSessionID;
                }
            }
            private string mSessionID;

            public string Pin
            {
                get
                {
                    return mPin;
                }
            }
            private string mPin;

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="args"></param>
            public LoginParams(NameValueCollection args)
            {
                mLogin = GetRequiredArgument(args, "Login");
                mPassword = GetRequiredArgument(args, "Password");
                mURL = GetRequiredArgument(args, "URL");
                if (!string.IsNullOrEmpty(mURL))
                {
                    if (!mURL.EndsWith("Hosts.jsp", StringComparison.OrdinalIgnoreCase))
                    {
                        mURL += "/Hosts.jsp";
                    }
                }
                mConnection = GetRequiredArgument(args, "Connection");
                mSessionID = args["SessionID"];
                mPin = args["Pin"];
            }

            /// <summary>
            /// Get required argument from configuration file
            /// </summary>
            /// <param name="args">Configuration file key-value collection</param>
            /// <param name="sArgumentName">Argument name (key) from configuration file</param>
            /// <returns>Argument value</returns>
            private string GetRequiredArgument(NameValueCollection args, string sArgumentName)
            {
                string sArgument = args[sArgumentName];
                if (!string.IsNullOrEmpty(sArgument))
                {
                    sArgument = sArgument.Trim();
                }
                if (string.IsNullOrEmpty(sArgument))
                {
                    throw new Exception(string.Format("Please provide {0} in configuration file", sArgumentName));
                }
                return sArgument;
            }
        }

        class SampleParams
        {
            public string Instrument
            {
                get
                {
                    return mInstrument;
                }
            }
            private string mInstrument;

            public string Status
            {
                get
                {
                    return mStatus;
                }
            }
            private string mStatus;

            public string AccountID
            {
                get
                {
                    return mAccountID;
                }
                set
                {
                    mAccountID = value;
                }
            }
            private string mAccountID;

            /// <summary>
            /// ctor
            /// </summary>
            /// <param name="args"></param>
            public SampleParams(NameValueCollection args)
            {
                mInstrument = GetRequiredArgument(args, "Instrument");
                mStatus = args["Status"];
                if (string.IsNullOrEmpty(mStatus))
                {
                    mStatus = Constants.SubscriptionStatuses.Tradable; // default is "T"
                }
                else
                {
                    if (!mStatus.Equals(Constants.SubscriptionStatuses.Disable) &&
                        !mStatus.Equals(Constants.SubscriptionStatuses.Tradable) &&
                        !mStatus.Equals(Constants.SubscriptionStatuses.ViewOnly))
                    {
                        mStatus = Constants.SubscriptionStatuses.Tradable; // default
                    }
                }
                mAccountID = args["Account"];
            }

            /// <summary>
            /// Get required argument from configuration file
            /// </summary>
            /// <param name="args">Configuration file key-value collection</param>
            /// <param name="sArgumentName">Argument name (key) from configuration file</param>
            /// <returns>Argument value</returns>
            private string GetRequiredArgument(NameValueCollection args, string sArgumentName)
            {
                string sArgument = args[sArgumentName];
                if (!string.IsNullOrEmpty(sArgument))
                {
                    sArgument = sArgument.Trim();
                }
                if (string.IsNullOrEmpty(sArgument))
                {
                    throw new Exception(string.Format("Please provide {0} in configuration file", sArgumentName));
                }
                return sArgument;
            }
        }
    }
}

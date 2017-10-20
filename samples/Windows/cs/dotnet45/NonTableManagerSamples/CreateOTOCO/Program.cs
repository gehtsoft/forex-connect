using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Configuration;
using System.Threading;
using fxcore2;

namespace CreateOTO
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

                PrintSampleParams("CreateOTOCO", loginParams, sampleParams);

                session = O2GTransport.createSession();
                SessionStatusListener statusListener = new SessionStatusListener(session, loginParams.SessionID, loginParams.Pin);
                session.subscribeSessionStatus(statusListener);
                session.login(loginParams.Login, loginParams.Password, loginParams.URL, loginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    ResponseListener responseListener = new ResponseListener(session);
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

                    O2GLoginRules loginRules = session.getLoginRules();
                    if (loginRules == null)
                    {
                        throw new Exception("Cannot get login rules");
                    }

                    O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(sampleParams.Instrument, account);
                    int iAmount = iBaseUnitSize * sampleParams.Lots;

                    double dRatePrimary = offer.Ask - 30.0 * offer.PointSize;
                    double dRateOcoFirst = offer.Ask + 15.0 * offer.PointSize;
                    double dRateOcoSecond = offer.Ask - 15.0 * offer.PointSize;

                    O2GRequest request = CreateOTOCORequest(session, offer.OfferID, account.AccountID, iAmount, dRatePrimary, dRateOcoFirst, dRateOcoSecond);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }

                    List<string> requestIDList = new List<string>();
                    FillRequestIDs(requestIDList, request); 
                    
                    responseListener.SetRequestIDs(requestIDList);
                    session.sendRequest(request);
                    if (responseListener.WaitEvents())
                    {
                        Console.WriteLine("Done!");
                    }
                    else
                    {
                        throw new Exception("Response waiting timeout expired");
                    }

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
        /// Find valid offer by instrument name
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sInstrument"></param>
        /// <returns>offer</returns>
        private static O2GOfferRow GetOffer(O2GSession session, string sInstrument)
        {
            O2GOfferRow offer = null;
            bool bHasOffer = false;
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
                offer = offersResponseReader.getRow(i);
                if (offer.Instrument.Equals(sInstrument))
                {
                    if (offer.SubscriptionStatus.Equals("T"))
                    {
                        bHasOffer = true;
                        break;
                    }
                }
            }
            if (!bHasOffer)
            {
                return null;
            }
            else
            {
                return offer;
            }
        }

        /// <summary>
        /// Create OTO request
        /// </summary>
        private static O2GRequest CreateOTOCORequest(O2GSession session, string sOfferID, string sAccountID, int iAmount, double dRatePrimary, double dRateOcoFirst, double dRateOcoSecond)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }

            // Create OTO command
            O2GValueMap valuemapMain = requestFactory.createValueMap();
            valuemapMain.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOTO);

            // Create Entry order
            O2GValueMap valuemapPrimary = requestFactory.createValueMap();
            valuemapPrimary.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemapPrimary.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.StopEntry);
            valuemapPrimary.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemapPrimary.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemapPrimary.setString(O2GRequestParamsEnum.BuySell, Constants.Sell);
            valuemapPrimary.setInt(O2GRequestParamsEnum.Amount, iAmount);
            valuemapPrimary.setDouble(O2GRequestParamsEnum.Rate, dRatePrimary);

            // Create OCO group of orders
            O2GValueMap valuemapOCO = requestFactory.createValueMap();
            valuemapOCO.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOCO);

            // Create Entry order to OCO
            O2GValueMap valuemapOCOFirst = requestFactory.createValueMap();
            valuemapOCOFirst.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.StopEntry);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemapOCOFirst.setString(O2GRequestParamsEnum.BuySell, Constants.Buy);
            valuemapOCOFirst.setInt(O2GRequestParamsEnum.Amount, iAmount);
            valuemapOCOFirst.setDouble(O2GRequestParamsEnum.Rate, dRateOcoFirst);

            O2GValueMap valuemapOCOSecond = requestFactory.createValueMap();
            valuemapOCOSecond.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.StopEntry);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemapOCOSecond.setString(O2GRequestParamsEnum.BuySell, Constants.Sell);
            valuemapOCOSecond.setInt(O2GRequestParamsEnum.Amount, iAmount);
            valuemapOCOSecond.setDouble(O2GRequestParamsEnum.Rate, dRateOcoSecond);

            // Fill the created groups. Please note, first you should add an entry order to OTO order and then OCO group of orders
            valuemapMain.appendChild(valuemapPrimary);
            valuemapOCO.appendChild(valuemapOCOFirst);
            valuemapOCO.appendChild(valuemapOCOSecond);
            valuemapMain.appendChild(valuemapOCO);

            request = requestFactory.createOrderRequest(valuemapMain);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }
        
        /// <summary>
        /// Fill request IDs
        /// </summary>
        /// <param name="requestIDs">List of request IDs</param>
        /// <param name="request">request</param>
        private static void FillRequestIDs(List<string> requestIDs, O2GRequest request)
        {
            int childrenCount = request.ChildrenCount;
            if (childrenCount == 0)
            {
                requestIDs.Add(request.RequestID);
                return;
            }

            for (int i = 0; i < childrenCount; i++)
            {
                O2GRequest childRequest = request.getChildRequest(i);
                FillRequestIDs(requestIDs, childRequest);
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
            Console.WriteLine("{0}: Instrument='{1}', Lots='{2}', AccountID='{3}'", procName, prm.Instrument, prm.Lots, prm.AccountID);
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

            public int Lots
            {
                get
                {
                    return mLots;
                }
            }
            private int mLots;

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
                string sLots = args["Lots"];
                if (string.IsNullOrEmpty(sLots))
                {
                    sLots = "1"; // default
                }
                Int32.TryParse(sLots, out mLots);
                if (mLots <= 0)
                {
                    throw new Exception(string.Format("\"Lots\" value {0} is invalid; please fix the value in the configuration file", sLots));
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

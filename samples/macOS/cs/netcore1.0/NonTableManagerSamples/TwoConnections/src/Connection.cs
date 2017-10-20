using System;
using ArgParser;
using fxcore2;

namespace TwoConnections
{
    class Connection
    {
        O2GSession mSession;
        LoginParams mLoginParams;
        SampleParams mSampleParams;
        bool mIsFirstAccount;
        SessionStatusListener statusListener = null;
        ResponseListener responseListener = null;

        public Connection(O2GSession session, LoginParams loginParams, SampleParams sampleParams, bool bIsFirstAccount)
        {
            mSession = session;
            mLoginParams = loginParams;
            mSampleParams = sampleParams;
            mIsFirstAccount = bIsFirstAccount;
        }

        public void Run()
        {
            try
            {
                string sLogin;
                string sPassword;
                string sSessionID;
                string sPin;
                if (mIsFirstAccount)
                {
                    sLogin = mLoginParams.Login;
                    sPassword = mLoginParams.Password;
                    sSessionID = mLoginParams.SessionID;
                    sPin = mLoginParams.Pin;
                }
                else
                {
                    sLogin = mLoginParams.Login2;
                    sPassword = mLoginParams.Password2;
                    sSessionID = mLoginParams.SessionID2;
                    sPin = mLoginParams.Pin2;
                }

                statusListener = new SessionStatusListener(mSession, sSessionID, sPin);
                mSession.subscribeSessionStatus(statusListener);
                statusListener.Reset();
                mSession.login(sLogin, sPassword, mLoginParams.URL, mLoginParams.Connection);
                if (statusListener.WaitEvents() && statusListener.Connected)
                {
                    if (!mIsFirstAccount) // Disable receiving price updates for the second account
                    {
                        mSession.setPriceUpdateMode(O2GPriceUpdateMode.NoPrice);
                    }
                    responseListener = new ResponseListener(mSession);
                    mSession.subscribeResponse(responseListener);
                    O2GAccountRow account = null;
                    if (mIsFirstAccount)
                    {
                        bool bIsAccountEmpty = String.IsNullOrEmpty(mSampleParams.AccountID);
                        account = GetAccount(mSession, mSampleParams.AccountID);
                        if (account != null)
                        {
                            if (bIsAccountEmpty)
                            {
                                mSampleParams.AccountID = account.AccountID;
                                Console.WriteLine("Account: " + mSampleParams.AccountID);
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("The account '{0}' is not valid",
                                    mSampleParams.AccountID));
                        }
                    }
                    else
                    {
                        bool bIsAccountEmpty = String.IsNullOrEmpty(mSampleParams.AccountID2);
                        account = GetAccount(mSession, mSampleParams.AccountID2);
                        if (account != null)
                        {
                            if (bIsAccountEmpty)
                            {
                                mSampleParams.AccountID2 = account.AccountID;
                                Console.WriteLine("Account2: " + mSampleParams.AccountID2);
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("The account2 '{0}' is not valid",
                                    mSampleParams.AccountID2));
                        }
                    }
                    O2GOfferRow offer = GetOffer(mSession, mSampleParams.Instrument);
                    if (offer == null)
                    {
                        throw new Exception(string.Format("The instrument '{0}' is not valid",
                                mSampleParams.Instrument));
                    }

                    O2GLoginRules loginRules = mSession.getLoginRules();
                    if (loginRules == null)
                    {
                        throw new Exception("Cannot get login rules");
                    }
                    O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(mSampleParams.Instrument, account);
                    int iAmount = iBaseUnitSize * mSampleParams.Lots;

                    O2GRequest request;
                    request = CreateTrueMarketOrderRequest(mSession, offer.OfferID, account.AccountID, iAmount, mSampleParams.BuySell);
                    if (request == null)
                    {
                        throw new Exception("Cannot create request");
                    }
                    responseListener.SetRequestID(request.RequestID);
                    mSession.sendRequest(request);
                    if (!responseListener.WaitEvents())
                    {
                        throw new Exception("Response waiting timeout expired");
                    }
                    Console.WriteLine("Done!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                if (statusListener.Connected)
                {
                    statusListener.Reset();
                    mSession.logout();
                    statusListener.WaitEvents();
                    mSession.unsubscribeResponse(responseListener);
                }
                mSession.subscribeSessionStatus(statusListener);
                mSession.Dispose();
            }
        }

        /// <summary>
        /// Find valid account by ID or get the first valid account
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

                if (string.IsNullOrEmpty(sAccountID) || sAccountID.Equals(account.AccountID))
                {
                    bHasAccount = true;
                    break;
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
        /// Create true market order request
        /// </summary>
        private static O2GRequest CreateTrueMarketOrderRequest(O2GSession session, string sOfferID, string sAccountID, int iAmount, string sBuySell)
        {
            O2GRequest request = null;
            O2GRequestFactory requestFactory = session.getRequestFactory();
            if (requestFactory == null)
            {
                throw new Exception("Cannot create request factory");
            }
            O2GValueMap valuemap = requestFactory.createValueMap();
            valuemap.setString(O2GRequestParamsEnum.Command, Constants.Commands.CreateOrder);
            valuemap.setString(O2GRequestParamsEnum.OrderType, Constants.Orders.TrueMarketOpen);
            valuemap.setString(O2GRequestParamsEnum.AccountID, sAccountID);
            valuemap.setString(O2GRequestParamsEnum.OfferID, sOfferID);
            valuemap.setString(O2GRequestParamsEnum.BuySell, sBuySell);
            valuemap.setInt(O2GRequestParamsEnum.Amount, iAmount);
            valuemap.setString(O2GRequestParamsEnum.CustomID, "TrueMarketOrder");
            request = requestFactory.createOrderRequest(valuemap);
            if (request == null)
            {
                Console.WriteLine(requestFactory.getLastError());
            }
            return request;
        }
    }
}

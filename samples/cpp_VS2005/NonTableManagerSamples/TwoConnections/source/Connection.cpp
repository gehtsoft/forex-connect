#include "stdafx.h"
#include "Connection.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

Connection::Connection(IO2GSession *session, LoginParams *loginParams, SampleParams *sampleParams, bool bIsFirstAccount)
{
    mSession = session;
    mLoginParams = loginParams;
    mSampleParams = sampleParams;
    mIsFirstAccount = bIsFirstAccount;
}

Connection::~Connection()
{
}

int Connection::run()
{
    const char *sLogin;
    const char *sPassword;
    const char *sSessionID;
    const char *sPin;
    bool bWasError = false;
    if (mIsFirstAccount)
    {
        sLogin = mLoginParams->getLogin();
        sPassword = mLoginParams->getPassword();
        sSessionID = mLoginParams->getSessionID();
        sPin = mLoginParams->getPin();
    }
    else
    {
        sLogin = mLoginParams->getLogin2();
        sPassword = mLoginParams->getPassword2();
        sSessionID = mLoginParams->getSessionID2();
        sPin = mLoginParams->getPin2();
    }

    SessionStatusListener *sessionListener = new SessionStatusListener(mSession, false, sSessionID, sPin);
    mSession->subscribeSessionStatus(sessionListener);
    bool bConnected = login(mSession, sessionListener, sLogin, sPassword, mLoginParams->getURL(), mLoginParams->getConnection());

    if (bConnected)
    {
        // Disable receiving price updates for the second account
        if (!mIsFirstAccount)
        {
            mSession->setPriceUpdateMode(NoPrice);
        }
        ResponseListener *responseListener = new ResponseListener(mSession);
        mSession->subscribeResponse(responseListener);

        O2G2Ptr<IO2GAccountRow> account = NULL;
        if (mIsFirstAccount)
        {
            bool bIsAccountEmpty = !mSampleParams->getAccount() || strlen(mSampleParams->getAccount()) == 0;
            account = getAccount(mSession, mSampleParams->getAccount());
            if (account)
            {
                if (bIsAccountEmpty)
                {
                    mSampleParams->setAccount(account->getAccountID());
                    std::cout << "Account: " << mSampleParams->getAccount() << std::endl;
                }
            }
            else
            {
                std::cout << "The account '" << mSampleParams->getAccount() << "' is not valid" << std::endl;
                bWasError = true;
            }
        }
        else
        {
            bool bIsAccountEmpty = !mSampleParams->getAccount2() || strlen(mSampleParams->getAccount2()) == 0;
            account = getAccount(mSession, mSampleParams->getAccount2());
            if (account)
            {
                if (bIsAccountEmpty)
                {
                    mSampleParams->setAccount2(account->getAccountID());
                    std::cout << "Account: " << mSampleParams->getAccount2() << std::endl;
                }
            }
            else
            {
                std::cout << "The account2 '" << mSampleParams->getAccount2() << "' is not valid" << std::endl;
                bWasError = true;
            }
        }

        if (!bWasError)
        {
            O2G2Ptr<IO2GOfferRow> offer = getOffer(mSession, mSampleParams->getInstrument());
            if (offer)
            {
                O2G2Ptr<IO2GLoginRules> loginRules = mSession->getLoginRules();
                if (loginRules)
                {
                    O2G2Ptr<IO2GTradingSettingsProvider> tradingSettingsProvider = loginRules->getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider->getBaseUnitSize(mSampleParams->getInstrument(), account);
                    int iAmount = iBaseUnitSize * mSampleParams->getLots();
                    O2G2Ptr<IO2GRequest> request = createTrueMarketOrderRequest(mSession, offer->getOfferID(), account->getAccountID(), iAmount, mSampleParams->getBuySell());
                    if (request)
                    {
                        responseListener->setRequestID(request->getRequestID());
                        mSession->sendRequest(request);
                        if (responseListener->waitEvents())
                        {
                            std::cout << "Done!" << std::endl;
                        }
                    }
                    else
                    {
                        std::cout << "Cannot create request" << std::endl;
                        bWasError = true;
                    }
                }
                else
                {
                    std::cout << "Cannot get login rules" << std::endl;
                    bWasError = true;
                }
            }
            else
            {
                std::cout << "The instrument '" << mSampleParams->getInstrument() << "' is not valid" << std::endl;
                bWasError = true;
            }
   
        }

        mSession->unsubscribeResponse(responseListener);
        responseListener->release();
        logout(mSession, sessionListener);
    }
    else
    {
        bWasError = true;
    }

    mSession->unsubscribeSessionStatus(sessionListener);
    sessionListener->release();

    if (bWasError)
        return -1;
    return 0;
}

bool Connection::login(IO2GSession *session, SessionStatusListener *statusListener, const char *sLogin, const char *sPassword,
        const char *sUrl, const char *sConnection)
{
    statusListener->reset();
    session->login(sLogin, sPassword, sUrl, sConnection);
    return statusListener->waitEvents() && statusListener->isConnected();
}

IO2GRequest *Connection::createTrueMarketOrderRequest(IO2GSession *session, const char *sOfferID, const char *sAccountID, int iAmount, const char *sBuySell)
{
    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
    if (!requestFactory)
    {
        std::cout << "Cannot create request factory" << std::endl;
        return NULL;
    }
    O2G2Ptr<IO2GValueMap> valuemap = requestFactory->createValueMap();
    valuemap->setString(Command, O2G2::Commands::CreateOrder);
    valuemap->setString(OrderType, O2G2::Orders::TrueMarketOpen);
    valuemap->setString(AccountID, sAccountID);
    valuemap->setString(OfferID, sOfferID);
    valuemap->setString(BuySell, sBuySell);
    valuemap->setInt(Amount, iAmount);
    valuemap->setString(CustomID, "TrueMarketOrder");
    O2G2Ptr<IO2GRequest> request = requestFactory->createOrderRequest(valuemap);
    if (!request)
    {
        std::cout << requestFactory->getLastError() << std::endl;
        return NULL;
    }
    return request.Detach();
}
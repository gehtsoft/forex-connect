
#define DEBUG_OUT

#include "OffersController.h"
#include <sys/timeb.h>
#include <pthread.h>

#include <libkern/OSAtomic.h>
#ifdef __LP64__
    #define InterlockedIncrement(A) (long)OSAtomicIncrement64((volatile int64_t *)A)
    #define InterlockedDecrement(A) (long)OSAtomicDecrement64((volatile int64_t *)A)
#else
    #define InterlockedIncrement(A) (long)OSAtomicIncrement32((volatile int32_t *)A)
    #define InterlockedDecrement(A) (long)OSAtomicDecrement32((volatile int32_t *)A)
#endif

typedef const char          *LPCSTR;

COffersController *COffersController::inst = NULL;


COffersController* COffersController::getInstance()
{
    if (!inst)
        inst = new COffersController;

    return inst;
}

COffersController::COffersController() : mIdWrapper(NULL), dwRef(1)
{
    mSession = CO2GTransport::createSession();
    mSession->subscribeSessionStatus(this);
    mSession->subscribeResponse(this);
}

COffersController::~COffersController()
{
    
   pthread_mutex_lock(&mRowsLock);

    mSession->unsubscribeSessionStatus(this);
    mSession->unsubscribeResponse(this);

    for (std::vector<COfferRow*>::reverse_iterator it = mRows.rbegin(); it != mRows.rend(); ++it)
        delete *it;
    mRows.clear();

    mSession->release();
    inst = NULL;
    pthread_mutex_unlock(&mRowsLock);
}

void COffersController::setLoginData(const char *user, const char *pwd, const char *url, const char *connection)
{
    mUserName = user;
    mPassword = pwd;
    mHost = url;
    mConnection = connection;
}

void COffersController::login()
{
#ifdef DEBUG_OUT
    std::cout << "Connect to: " << mUserName << " "
              << "*" << " "
              << mHost << " "
              << mConnection << " "
              << mSessionID << " "
              << mPin << std::endl;
#endif
    mSession->login(mUserName.c_str(), mPassword.c_str(), mHost.c_str(), mConnection.c_str());
}

void COffersController::logout()
{
    mSession->logout();
}

void COffersController::setTradingSessionDescriptors(const char *sessionID, const char *pin)
{
    mSessionID = sessionID;
    mPin = pin;
}

void COffersController::setProxy(const char *pname, int port, const char *user, const char *password)
{
#ifdef DEBUG_OUT
    if (pname && pname[0] != '\x0')
    {
        std::cout << "Proxy set: " << pname << ":"
              << port << " "
              << user << " *"
              << std::endl;
    }
#endif
    CO2GTransport::setProxy(pname, port, user, password);
}

void COffersController::setCAInfo(const char *caFilePath)
{
    logString(caFilePath);
    CO2GTransport::setCAInfo(caFilePath);
}

bool COffersController::isCellChanged(size_t index)
{
    pthread_mutex_lock(&mRowsLock);
    if (index >= mRows.size())
    {
        pthread_mutex_unlock(&mRowsLock);
        return true;
    }
    if (mRows[index]->isChanged)
    {
        pthread_mutex_unlock(&mRowsLock);
        return true;
    }
    if (mRows[index]->dropDirectionCount > 0 && mRows[index]->dropDirectionCount <= getTickCount())
    {
        mRows[index]->dropDirectionCount = 0;
        mRows[index]->nBidDirection = 0;
        mRows[index]->nAskDirection = 0;
        pthread_mutex_unlock(&mRowsLock);
        return true;
    }
    pthread_mutex_unlock(&mRowsLock);
    return false;
}

void COffersController::setData(IO2GOffersTableResponseReader *pReader)
{
    {
        pthread_mutex_lock(&mRowsLock);
        std::string sOfferId;
        COfferRow *offerRow;
        double fBid, fAsk, fHigh, fLow;
        std::vector<COfferRow*>::iterator it;

        for (int i = 0; i < pReader->size(); i++)
        {
            IO2GOfferRow *offer = pReader->getRow(i);
            if (!offer)
                continue;
            sOfferId = offer->getOfferID();

            if (offer->getSubscriptionStatus()[0] != 'T')
            {
                offer->release();
                continue;
            }

            fAsk = offer->getAsk();
            fBid = offer->getBid();
            fHigh = offer->getHigh();
            fLow = offer->getLow();

            for (it = mRows.begin(); it != mRows.end(); ++it)
                if ((*it)->sOfferId == sOfferId)
                    break;

            if (it != mRows.end())
            {
                offerRow = *it;
                if (fAsk != offerRow->fAsk || fBid != offerRow->fBid || fHigh != offerRow->fHigh ||
                    fLow != offerRow->fLow)
                {
                    offerRow->nBidDirection = (fBid < offerRow->fBid) ? -1 : (fBid > offerRow->fBid ? 1 : 0);
                    offerRow->nAskDirection = (fAsk < offerRow->fAsk) ? -1 : (fAsk > offerRow->fAsk ? 1 : 0);
                    offerRow->fBid = fBid;
                    offerRow->fAsk = fAsk;
                    offerRow->fHigh = fHigh;
                    offerRow->fLow = fLow;
                    offerRow->dropDirectionCount = getTickCount()+1000;
                    offerRow->isChanged = true;
                }
            }
            else
            {
                offerRow = new COfferRow;
                offerRow->fBid = fBid;
                offerRow->fAsk = fAsk;
                offerRow->fHigh = fHigh;
                offerRow->fLow = fLow;
                offerRow->nDigits = offer->getDigits();
                offerRow->sOfferId = sOfferId;
                offerRow->sInstrument = offer->getInstrument();
                offerRow->nBidDirection = 0;
                offerRow->nAskDirection = 0;
                offerRow->dropDirectionCount = 0;
                offerRow->isChanged = true;
                mRows.push_back(offerRow);
            }
            offer->release();
        }
        pthread_mutex_unlock(&mRowsLock);
    }

    // Subscriber call
    notify();
}

void COffersController::notify()
{
    // Subscriber call
    if (mIdWrapper)
        mIdWrapper->call(mIdWrapper->target, mIdWrapper->selector, mIdWrapper->param);

/*
    // The same subscriber call using Obj-C syntax

#ifdef __OBJC__
    if (mIdWrapper)
        [mIdWrapper->target performSelector: mIdWrapper->selector];
#endif
*/
}

int COffersController::getBidDirection(size_t index) const
{
    return mRows[index]->nBidDirection;
}

int COffersController::getAskDirection(size_t index) const
{
    return mRows[index]->nAskDirection;
}

int COffersController::getDigits(size_t index) const
{
    return mRows[index]->nDigits;
}

double COffersController::getLow(size_t index) const
{
    return mRows[index]->fLow;
}

double COffersController::getHigh(size_t index) const
{
    return mRows[index]->fHigh;
}

double COffersController::getBid(size_t index) const
{
    return mRows[index]->fBid;
}

double COffersController::getAsk(size_t index) const
{
    return mRows[index]->fAsk;
}

const char * COffersController::getInstrumentText(size_t index)
{
    mRows[index]->isChanged = false;
    return mRows[index]->sInstrument.c_str();
}

const char * COffersController::getStatusText()
{
    return mStatusText.c_str();
}


void COffersController::subscribe(void *pListener)
{
     mIdWrapper = (idWrapper *)pListener;
}

void COffersController::createOrder(int offerIndex, bool isBuy, int nAmount, double fRate, int orderType)
{
    IO2GRequestFactory *factory = mSession->getRequestFactory();

    LPCSTR orderTypes[]={O2G2::Orders::TrueMarketOpen, O2G2::Orders::StopEntry, O2G2::Orders::LimitEntry};

    IO2GValueMap *valuemapStop2 = factory->createValueMap();
    valuemapStop2->setString(Command, O2G2::Commands::CreateOrder);
    //valuemapStop2->setString(OrderType, O2G2::Orders::LimitEntry);
    valuemapStop2->setString(OrderType, orderTypes[orderType]);
    valuemapStop2->setString(AccountID, msFirstAccountID.c_str());
    valuemapStop2->setString(OfferID, mRows[offerIndex]->sOfferId.c_str());
    valuemapStop2->setString(BuySell, isBuy ? "B": "S");
    valuemapStop2->setInt(Amount, nAmount);
    valuemapStop2->setDouble(Rate, fRate);
    valuemapStop2->setString(TimeInForce, O2G2::TIF::GTC);
    //valuemapStop2->setDouble(RateStop, 1.3300);
    //valuemapStop2->setInt(TrailStepStop, 1);
    //valuemapStop2->setString(PegTypeLimit, "O");
    //valuemapStop2->setDouble(PegOffsetLimit, 10.0);


    IO2GRequest *request = factory->createOrderRequest(valuemapStop2);
    mSession->sendRequest(request);
    request->release();
    valuemapStop2->release();
}


void COffersController::onSessionStatusChanged(O2GSessionStatus status)
{
    switch(status)
    {
     case Disconnected:
        {
            NSNumber *flag = [NSNumber numberWithBool:YES];
            NSDictionary *message = [NSDictionary dictionaryWithObject:flag forKey:@"okButtonIsEnable"];
            [[NSNotificationCenter defaultCenter] postNotificationName:@"UIChangeOKButtonStatusNotification" object:nil userInfo:message];
        }
        mStatusText = "Disconnected";
        logString("Status: Disconnected");
        break;
     case Disconnecting:
        mStatusText = "Disconnecting...";
        logString("Status: Disconnecting");
        break;
     case Connecting:
        {
            NSNumber *flag = [NSNumber numberWithBool:NO];
            NSDictionary *message = [NSDictionary dictionaryWithObject:flag forKey:@"okButtonIsEnable"];
            [[NSNotificationCenter defaultCenter] postNotificationName:@"UIChangeOKButtonStatusNotification" object:nil userInfo:message];
        }
        mStatusText = "Connecting...";
        logString("Status: Connecting");
        break;

     case TradingSessionRequested:
        mStatusText = "TradingSessionRequested...";
        logString("Status: TradingSessionRequested");
        if (mSessionID.empty())
        {
            IO2GSessionDescriptorCollection *descriptors = mSession->getTradingSessionDescriptors();
            if (descriptors->size() > 0)
                mSessionID = descriptors->get(0)->getID();
            descriptors->release();
        }
        mSession->setTradingSession(mSessionID.c_str(), mPin.c_str());
        break;

     case Connected:

        mStatusText = "Rates";
        logString("Status: Connected");
        {

        IO2GLoginRules *loginRules = mSession->getLoginRules();
        if (loginRules && loginRules->isTableLoadedByDefault(Offers))
        {
            IO2GResponse *response = loginRules->getTableRefreshResponse(Offers);
            onOffersRecieved(response);
            response->release();
        }
        if (loginRules && loginRules->isTableLoadedByDefault(Accounts))
        {
            IO2GResponse *response = loginRules->getTableRefreshResponse(Accounts);
            onAccountsReceived(response);
            response->release();
        }

        if (loginRules)
            loginRules->release();
        }

      break;
     case Reconnecting:
      mStatusText = "Reconnecting...";
      logString("Status: Reconnecting");
      break;
     case SessionLost:
      mStatusText = "Session lost...";
      logString("Status: Session Lost");
      login();
      break;
    }
    notify();
}

void COffersController::onLoginFailed(const char *error)
{
    logString(error);
}

void COffersController::onOffersRecieved(IO2GResponse *response)
{
    IO2GResponseReaderFactory *factory = mSession->getResponseReaderFactory();
    if (factory == NULL)
        return;
    IO2GOffersTableResponseReader *offersReader = factory->createOffersTableReader(response);
    if (offersReader == NULL)
    {
        factory->release();
        return;
    }

    setData(offersReader);

    offersReader->release();
    factory->release();
}

void COffersController::onTablesUpdates(IO2GResponse *response)
{
    logString("Data recieved");
    if (response->getType() == TablesUpdates)
            onTableUpdateReceive(response);
}

void COffersController::onTableUpdateReceive(IO2GResponse *response)
{
    IO2GResponseReaderFactory *factory = mSession->getResponseReaderFactory();
    if (factory == NULL)
        return;
    IO2GTablesUpdatesReader *updatesReader = factory->createTablesUpdatesReader(response);
    if (updatesReader == NULL)
    {
        factory->release();
        return;
    }
    //char szBuffer[512];
    for (int i = 0; i < updatesReader->size(); i++)
    {
        O2GTable o2gTable = updatesReader->getUpdateTable(i);
        if (o2gTable == TableUnknown)
            continue;
       /* O2GTableUpdateType o2gUpdateType = updatesReader->getUpdateType(i);
        LPCSTR szUpdateType = "Unknown";
        switch(o2gUpdateType)
        {
        case Insert:
            szUpdateType = "Insert";
        break;
        case Delete:
            szUpdateType = "Delete";
        break;
        case Update:
            szUpdateType = "Update";
        break;
        default:
        break;
        }*/

        switch (o2gTable)
        {
         case Offers:
            onOffersRecieved(response);
            break;

        case Accounts:
        {
            onAccountsReceived(response);
            //sprintf_s(szBuffer, sizeof(szBuffer), "Action=%s Table=%s", szUpdateType, "Account");
            //logString(szBuffer);
            //IO2GAccountRow *account = updatesReader->getAccountRow(i);
            //printToLogAccount(account);
            //account->release();
        }
        break;
        /*
        case Orders:
        {
            sprintf_s(szBuffer, sizeof(szBuffer), "Action=%s Table=%s", szUpdateType, "Order");
            logString(szBuffer);
            IO2GOrderRow *order = updatesReader->getOrderRow(i);
            printToLogOrder(order);
            order->release();
        }
        break;
        case Trades:
        {
            sprintf_s(szBuffer, sizeof(szBuffer), "Action=%s Table=%s", szUpdateType, "Trade");
            logString(szBuffer);
            IO2GTradeRow *trade = updatesReader->getTradeRow(i);
            printToLogTrade(trade);
            trade->release();
        }
        break;
        case ClosedTrades:
        {
            sprintf_s(szBuffer, sizeof(szBuffer), "Action=%s Table=%s", szUpdateType, "Closed Trade");
            logString(szBuffer);
            IO2GClosedTradeRow *closedTrade = updatesReader->getClosedTradeRow(i);
            printToLogClosedTrade(closedTrade);
            closedTrade->release();
        }
        break;
        case Messages:
        {
            sprintf_s(szBuffer, sizeof(szBuffer), "Action=%s Table=%s", szUpdateType, "Message");
            logString(szBuffer);
            IO2GMessageRow *message = updatesReader->getMessageRow(i);
            printToLogMessage(message);
            message->release();
        }
        break;*/
        default:
            break;
        }
    }
    factory->release();
    updatesReader->release();
}

void COffersController::onAccountsReceived(IO2GResponse *response)
{
    IO2GResponseReaderFactory *factory = mSession->getResponseReaderFactory();
    if (factory == NULL)
        return;
    IO2GAccountsTableResponseReader *accountsReader = factory->createAccountsTableReader(response);
    if (accountsReader == NULL)
    {
        factory->release();
        return;
    }

    if (msFirstAccountID.empty())
    {
      IO2GAccountRow *account = accountsReader->getRow(0);
      msFirstAccountID = account->getAccountID();
      account->release();
    }

    accountsReader->release();
    factory->release();
}

void COffersController::logString(LPCSTR logmessage)
{
#ifdef DEBUG_OUT
    std::cout << logmessage << std::endl;
#endif
}

long COffersController::addRef()
{
    return InterlockedIncrement(&dwRef);
}

long COffersController::release()
{
    long dwRes = InterlockedDecrement(&dwRef);
    if (dwRes == 0)
        delete this;
    return dwRes;
}

unsigned long COffersController::getTickCount()
{
    struct timeb currSysTime;
    ftime(&currSysTime);
    return long(currSysTime.time) * 1000 + currSysTime.millitm;
}


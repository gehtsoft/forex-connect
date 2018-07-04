#include "stdafx.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "CommonSources.h"
#include <sstream>
#include <iomanip>

bool login(IO2GSession *session, SessionStatusListener *statusListener, LoginParams *loginParams)
{
    statusListener->reset();
    session->login(loginParams->getLogin(), loginParams->getPassword(),
            loginParams->getURL(), loginParams->getConnection());
    return statusListener->waitEvents() && statusListener->isConnected();
}

void logout(IO2GSession *session, SessionStatusListener *statusListener)
{
    statusListener->reset();
    session->logout();
    statusListener->waitEvents();
}

void formatDate(DATE date, char *buf)
{
    struct tm tmBuf = {0};
    CO2GDateUtils::OleTimeToCTime(date, &tmBuf);
    
    using namespace std;
    stringstream sstream;
    sstream << setw(2) << setfill('0') << tmBuf.tm_mon + 1 << "." \
            << setw(2) << setfill('0') << tmBuf.tm_mday << "." \
            << setw(4) << tmBuf.tm_year + 1900 << " " \
            << setw(2) << setfill('0') << tmBuf.tm_hour << ":" \
            << setw(2) << setfill('0') << tmBuf.tm_min << ":" \
            << setw(2) << setfill('0') << tmBuf.tm_sec;
    strcpy(buf, sstream.str().c_str());
}

IO2GOfferTableRow *getOffer(IO2GTableManager *tableManager, const char *sInstrument)
{
    if (!tableManager || !sInstrument)
        return NULL;

    O2G2Ptr<IO2GOffersTable> offersTable = static_cast<IO2GOffersTable *>(tableManager->getTable(Offers));

    IO2GOfferTableRow *offer = NULL;
    IO2GTableIterator it;
    while (offersTable->getNextRow(it, offer))
    {
        if (strcmp(sInstrument, offer->getInstrument()) == 0)
            if (strcmp(offer->getSubscriptionStatus(), "T") == 0)
                return offer;
        offer->release();
    }

    return NULL;
}

IO2GAccountTableRow *getAccount(IO2GTableManager *tableManager, const char *sAccountID)
{
    if (!tableManager)
        return NULL;

    O2G2Ptr<IO2GAccountsTable> accountsTable = static_cast<IO2GAccountsTable *>(tableManager->getTable(Accounts));

    IO2GAccountTableRow *account = NULL;
    IO2GTableIterator it;
    while (accountsTable->getNextRow(it, account))
    {
        if (!sAccountID || strlen(sAccountID) == 0 || strcmp(account->getAccountID(), sAccountID) == 0)
            if (strcmp(account->getMarginCallFlag(), "N") == 0 &&
                (strcmp(account->getAccountKind(), "32") == 0 ||
                strcmp(account->getAccountKind(), "36") == 0))
                return account;
        account->release();
    }

    return NULL;
}

bool isNaN(double value)
{
    return value != value;
}

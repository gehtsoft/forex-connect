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

IO2GOfferRow *getOffer(IO2GSession *session, const char *sInstrument)
{
    if (!session || !sInstrument)
        return NULL;

    O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
    if (loginRules)
    {
        O2G2Ptr<IO2GResponse> response = loginRules->getTableRefreshResponse(Offers);
        if (response)
        {
            O2G2Ptr<IO2GResponseReaderFactory> readerFactory = session->getResponseReaderFactory();
            if (readerFactory)
            {
                O2G2Ptr<IO2GOffersTableResponseReader> reader = readerFactory->createOffersTableReader(response);

                for (int i = 0; i < reader->size(); ++i)
                {
                    O2G2Ptr<IO2GOfferRow> offer = reader->getRow(i);
                    if (offer)
                        if (strcmp(sInstrument, offer->getInstrument()) == 0)
                            if (strcmp(offer->getSubscriptionStatus(), "T") == 0)
                                return offer.Detach();
                }
            }
        }
    }
    return NULL;
}

IO2GAccountRow *getAccount(IO2GSession *session, const char *sAccountID)
{
    O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
    if (loginRules)
    {
        O2G2Ptr<IO2GResponse> response = loginRules->getTableRefreshResponse(Accounts);
        if (response)
        {
            O2G2Ptr<IO2GResponseReaderFactory> readerFactory = session->getResponseReaderFactory();
            if (readerFactory)
            {
                O2G2Ptr<IO2GAccountsTableResponseReader> reader = readerFactory->createAccountsTableReader(response);

                for (int i = 0; i < reader->size(); ++i)
                {
                    O2G2Ptr<IO2GAccountRow> account = reader->getRow(i);
                    if (account)
                        if (!sAccountID || strlen(sAccountID) == 0 || strcmp(account->getAccountID(), sAccountID) == 0)
                            if (strcmp(account->getMarginCallFlag(), "N") == 0 &&
                                    (strcmp(account->getAccountKind(), "32") == 0 ||
                                    strcmp(account->getAccountKind(), "36") == 0))
                                return account.Detach();
                }
            }
        }
    }
    return NULL;
}

bool isNaN(double value)
{
    return value != value;
}

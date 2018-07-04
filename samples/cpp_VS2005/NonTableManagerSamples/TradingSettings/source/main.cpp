#include "stdafx.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *);
void printSampleParams(std::string &, LoginParams *);
bool printTradingSettings(IO2GSession *);

int main(int argc, char *argv[])
{
    std::string procName = "TradingSettings";
    if (argc == 1)
    {
        printHelp(procName);
        return -1;
    }

    LoginParams *loginParams = new LoginParams(argc, argv);

    printSampleParams(procName, loginParams);
    if (!checkObligatoryParams(loginParams))
    {
        delete loginParams;
        return -1;
    }

    IO2GSession *session = CO2GTransport::createSession();

    SessionStatusListener *sessionListener = new SessionStatusListener(session, false,
                                                                       loginParams->getSessionID(),
                                                                       loginParams->getPin());
    session->subscribeSessionStatus(sessionListener);

    bool bConnected = login(session, sessionListener, loginParams);
    bool bWasError = false;

    if (bConnected)
    {
        if (printTradingSettings(session))
            std::cout << "Done!" << std::endl;
        else
            bWasError = true;
        logout(session, sessionListener);
    }
    else
    {
        bWasError = true;
    }

    session->unsubscribeSessionStatus(sessionListener);
    sessionListener->release();
    session->release();

    delete loginParams;

    if (bWasError)
        return -1;
    return 0;
}

// Print trading settings of the first account
bool printTradingSettings(IO2GSession *session)
{
    O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
    if (!loginRules)
    {
        std::cout << "Cannot get login rules" << std::endl;
        return false;
    }
    O2G2Ptr<IO2GResponse> accountsResponse = loginRules->getTableRefreshResponse(Accounts);
    if (!accountsResponse)
    {
        std::cout << "Cannot get response" << std::endl;
        return false;
    }
    O2G2Ptr<IO2GResponse> offersResponse = loginRules->getTableRefreshResponse(Offers);
    if (!offersResponse)
    {
        std::cout << "Cannot get response" << std::endl;
        return false;
    }
    O2G2Ptr<IO2GTradingSettingsProvider> tradingSettingsProvider = loginRules->getTradingSettingsProvider();
    O2G2Ptr<IO2GResponseReaderFactory> factory = session->getResponseReaderFactory();
    if (!factory)
    {
        std::cout << "Cannot create response reader factory" << std::endl;
        return false;
    }
    O2G2Ptr<IO2GAccountsTableResponseReader> accountsReader = factory->createAccountsTableReader(accountsResponse);
    O2G2Ptr<IO2GOffersTableResponseReader> instrumentsReader = factory->createOffersTableReader(offersResponse);
    O2G2Ptr<IO2GAccountRow> account = accountsReader->getRow(0);
    for (int i = 0; i < instrumentsReader->size(); ++i)
    {
        O2G2Ptr<IO2GOfferRow> instrumentRow = instrumentsReader->getRow(i);
        const char *sInstrument = instrumentRow->getInstrument();
        int condDistStopForTrade = tradingSettingsProvider->getCondDistStopForTrade(sInstrument);
        int condDistLimitForTrade = tradingSettingsProvider->getCondDistLimitForTrade(sInstrument);
        int condDistEntryStop = tradingSettingsProvider->getCondDistEntryStop(sInstrument);
        int condDistEntryLimit = tradingSettingsProvider->getCondDistEntryLimit(sInstrument);
        int minQuantity = tradingSettingsProvider->getMinQuantity(sInstrument, account);
        int maxQuantity = tradingSettingsProvider->getMaxQuantity(sInstrument, account);
        int baseUnitSize = tradingSettingsProvider->getBaseUnitSize(sInstrument, account);
        O2GMarketStatus marketStatus = tradingSettingsProvider->getMarketStatus(sInstrument);
        int minTrailingStep = tradingSettingsProvider->getMinTrailingStep();
        int maxTrailingStep = tradingSettingsProvider->getMaxTrailingStep();
        double mmr = tradingSettingsProvider->getMMR(sInstrument, account);
        std::string sMarketStatus = "unknown";
        switch (marketStatus)
        {
        case MarketStatusOpen:
            sMarketStatus = "Market Open";
            break;
        case MarketStatusClosed:
            sMarketStatus = "Market Close";
            break;
        }
        std::cout << "Instrument: " << sInstrument << ", Status: " << sMarketStatus << std::endl;
        std::cout << "Cond.Dist: ST=" << condDistStopForTrade << "; LT=" << condDistLimitForTrade << std::endl;
        std::cout << "Cond.Dist entry stop=" << condDistEntryStop << "; entry limit=" << condDistEntryLimit << std::endl;
        std::cout << "Quantity: Min=" << minQuantity << "; Max=" << maxQuantity
                << "; Base unit size=" << baseUnitSize << "; MMR=" << mmr << std::endl;

        double mmr2=0, emr=0, lmr=0;
        if (tradingSettingsProvider->getMargins(sInstrument, account, mmr2, emr, lmr))
        {
            std::cout << "Three level margin: MMR=" << mmr2 << "; EMR=" << emr
                    << "; LMR=" << lmr << std::endl;
        }
        else
        {
            std::cout << "Single level margin: MMR=" << mmr2 << "; EMR=" << emr
                    << "; LMR=" << lmr << std::endl;
        }
        std::cout << "Trailing step: " << minTrailingStep << "-" << maxTrailingStep << std::endl;
    }
    return true;
}

void printSampleParams(std::string &sProcName, LoginParams *loginParams)
{
    std::cout << "Running " << sProcName << " with arguments:" << std::endl;

    // Login (common) information
    if (loginParams)
    {
        std::cout << loginParams->getLogin() << " * "
                  << loginParams->getURL() << " "
                  << loginParams->getConnection() << " "
                  << loginParams->getSessionID() << " "
                  << loginParams->getPin() << std::endl;
    }
}

void printHelp(std::string &sProcName)
{
    std::cout << sProcName << " sample parameters:" << std::endl << std::endl;
            
    std::cout << "/login | --login | /l | -l" << std::endl;
    std::cout << "Your user name." << std::endl << std::endl;
                
    std::cout << "/password | --password | /p | -p" << std::endl;
    std::cout << "Your password." << std::endl << std::endl;
                
    std::cout << "/url | --url | /u | -u" << std::endl;
    std::cout << "The server URL. For example, http://www.fxcorporate.com/Hosts.jsp." << std::endl << std::endl;
                
    std::cout << "/connection | --connection | /c | -c" << std::endl;
    std::cout << "The connection name. For example, \"Demo\" or \"Real\"." << std::endl << std::endl;
                
    std::cout << "/sessionid | --sessionid " << std::endl;
    std::cout << "The database name. Required only for users who have accounts in more than one database. Optional parameter." << std::endl << std::endl;
                
    std::cout << "/pin | --pin " << std::endl;
    std::cout << "Your pin code. Required only for users who have a pin. Optional parameter." << std::endl << std::endl;
                
}

bool checkObligatoryParams(LoginParams *loginParams)
{
    /* Check login parameters. */
    if (strlen(loginParams->getLogin()) == 0)
    {
        std::cout << LoginParams::Strings::loginNotSpecified << std::endl;
        return false;
    }
    if (strlen(loginParams->getPassword()) == 0)
    {
        std::cout << LoginParams::Strings::passwordNotSpecified << std::endl;
        return false;
    }
    if (strlen(loginParams->getURL()) == 0)
    {
        std::cout << LoginParams::Strings::urlNotSpecified << std::endl;
        return false;
    }
    if (strlen(loginParams->getConnection()) == 0)
    {
        std::cout << LoginParams::Strings::connectionNotSpecified << std::endl;
        return false;
    }

    return true;
}


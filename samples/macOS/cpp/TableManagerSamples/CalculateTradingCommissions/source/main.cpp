#include "stdafx.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *sampleParams);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
void printEstimatedTradingCommissions(IO2GTableManager *tableManager, IO2GCommissionsProvider *commissionsProvider,
    IO2GTradingSettingsProvider *tradingSettingsProvider, SampleParams *sampleParams);

int main(int argc, char *argv[])
{
    std::string procName = "CalculateTradingCommissions";
    if (argc == 1)
    {
        printHelp(procName);
        return -1;
    }

    LoginParams *loginParams = new LoginParams(argc, argv);
    SampleParams *sampleParams = new SampleParams(argc, argv);

    printSampleParams(procName, loginParams, sampleParams);
    if (!checkObligatoryParams(loginParams, sampleParams))
    {
        delete loginParams;
        delete sampleParams;
        return -1;
    }

    IO2GSession *session = CO2GTransport::createSession();
    session->useTableManager(::Yes, NULL);

    SessionStatusListener *sessionListener = new SessionStatusListener(session, false,
                                                                       loginParams->getSessionID(),
                                                                       loginParams->getPin());
    session->subscribeSessionStatus(sessionListener);

    bool bConnected = login(session, sessionListener, loginParams);
    bool bWasError = false;

    if (bConnected)
    {   
        O2G2Ptr<IO2GTableManager> tableManager = session->getTableManager();
        O2GTableManagerStatus managerStatus = tableManager->getStatus();
        while (managerStatus == TablesLoading)
        {
            Sleep(50);
            managerStatus = tableManager->getStatus();
        }

        if (managerStatus == TablesLoadFailed)
        {
            std::cout << "Cannot refresh all tables of table manager" << std::endl;
        }

        //wait until commissions related information will be loaded
        O2G2Ptr<IO2GCommissionsProvider> commissionProvider = session->getCommissionsProvider();
        while (commissionProvider->getStatus() == CommissionStatusLoading)
            Sleep(50);
        
        if (commissionProvider->getStatus() == CommissionStatusReady)
        {
            O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
            O2G2Ptr<IO2GTradingSettingsProvider> tradingSettingsProvider = loginRules->getTradingSettingsProvider();
            printEstimatedTradingCommissions(tableManager, commissionProvider, tradingSettingsProvider, sampleParams);
        }
        else
            std::cout << "Could not calculate the estimated commissions." << std::endl;

        std::cout << "Done!" << std::endl;
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

void printEstimatedTradingCommissions(IO2GTableManager *tableManager, IO2GCommissionsProvider *commissionsProvider, 
    IO2GTradingSettingsProvider *tradingSettingsProvider, SampleParams *sampleParams)
{
    //retrieve the required parameter values from the sampleParams
    O2G2Ptr<IO2GOfferTableRow> offer = getOffer(tableManager, sampleParams->getInstrument());
    O2G2Ptr<IO2GAccountTableRow> account = getAccount(tableManager, sampleParams->getAccount());
    if (!offer || !account)
    {
        std::cout << "Incorrect input parameters." << std::endl;
        return;
    }

    int baseUnitSize = tradingSettingsProvider->getBaseUnitSize(sampleParams->getInstrument(), account);
    int amount = baseUnitSize * sampleParams->getLots();

    //calculate commissions
    std::cout << "Commission for open the position is " << commissionsProvider->calcOpenCommission(offer, account, amount, sampleParams->getBuySell(), 0) << std::endl;
    std::cout << "Commission for close the position is " << commissionsProvider->calcCloseCommission(offer, account, amount, sampleParams->getBuySell(), 0) << std::endl;
    std::cout << "Total commission for open and close the position is " << commissionsProvider->calcTotalCommission(offer, account, amount, sampleParams->getBuySell(), 0, 0) << std::endl;
}

void printSampleParams(std::string &sProcName, LoginParams *loginParams, SampleParams *sampleParams)
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

    // Sample specific information
    if (sampleParams)
    {
        std::cout << "Instrument='" << sampleParams->getInstrument() << "', "
                << "BuySell='" << sampleParams->getBuySell() << "', "
                << "Lots='" << sampleParams->getLots() << "', "
                << "Account='" << sampleParams->getAccount() << "'"
                << std::endl;
    }
}

void printHelp(std::string &sProcName)
{
    std::cout << sProcName << " sample parameters:" << std::endl << std::endl;
            
    std::cout << "/login | --login | /l | -l" << std::endl;
    std::cout << "Your user name. Mandatory parameter." << std::endl << std::endl;
                
    std::cout << "/password | --password | /p | -p" << std::endl;
    std::cout << "Your password. Mandatory parameter." << std::endl << std::endl;
                
    std::cout << "/url | --url | /u | -u" << std::endl;
    std::cout << "The server URL. For example, http://www.fxcorporate.com/Hosts.jsp. Mandatory parameter." << std::endl << std::endl;
                
    std::cout << "/connection | --connection | /c | -c" << std::endl;
    std::cout << "The connection name. For example, \"Demo\" or \"Real\". Mandatory parameter." << std::endl << std::endl;
                
    std::cout << "/sessionid | --sessionid " << std::endl;
    std::cout << "The database name. Required only for users who have a multiple database login. Optional parameter." << std::endl << std::endl;
                
    std::cout << "/pin | --pin " << std::endl;
    std::cout << "Your pin code. Required only for users who have a pin. Optional parameter." << std::endl << std::endl;

    std::cout << "/instrument | --instrument | /i | -i" << std::endl;
    std::cout << "An instrument which you want to use in sample. For example, \"EUR/USD\"." << std::endl << std::endl;
            
    std::cout << "/account | --account " << std::endl;
    std::cout << "An account which you want to use in sample. Optional parameter." << std::endl << std::endl;
            
    std::cout << "/buysell | --buysell | /d | -d" << std::endl;
    std::cout << "The order direction. Possible values are: B - buy, S - sell." << std::endl << std::endl;
            
    std::cout << "/lots | --lots " << std::endl;
    std::cout << "Trade amount in lots. Optional parameter." << std::endl << std::endl;
                
}

bool checkObligatoryParams(LoginParams *loginParams, SampleParams *sampleParams)
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

     /* Check other parameters. */
    if (strlen(sampleParams->getInstrument()) == 0)
    {
        std::cout << SampleParams::Strings::instrumentNotSpecified << std::endl;
        return false;
    }
    if (strlen(sampleParams->getBuySell()) == 0)
    {
        std::cout << SampleParams::Strings::buysellNotSpecified << std::endl;
        return false;
    }
    if (sampleParams->getLots() <= 0)
    {
        std::cout << "'Lots' value " << sampleParams->getLots() << " is invalid" << std::endl;
        return false;
    }

    return true;
}


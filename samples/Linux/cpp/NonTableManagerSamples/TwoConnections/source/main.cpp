#include "stdafx.h"
#include "Connection.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);

int main(int argc, char *argv[])
{
    std::string procName = "TwoConnections";
    if (argc == 1)
    {
        printHelp(procName);
        return -1;
    }

    bool bWasError = false;

    LoginParams *loginParams = new LoginParams(argc, argv);
    SampleParams *sampleParams = new SampleParams(argc, argv);

    printSampleParams(procName, loginParams, sampleParams);
    if (!checkObligatoryParams(loginParams, sampleParams))
    {
        delete loginParams;
        delete sampleParams;
        return -1;
    }

    IO2GSession *session1 = CO2GTransport::createSession();
    IO2GSession *session2 = CO2GTransport::createSession();
    Connection *connection1 = new Connection(session1, loginParams, sampleParams, true);
    Connection *connection2 = new Connection(session2, loginParams, sampleParams, false);
    connection1->start();
    connection2->start();
    connection1->join();
    connection2->join();

    delete connection1;
    delete connection2;

    session1->release();
    session2->release();

    delete loginParams;
    delete sampleParams;

    if (bWasError)
        return -1;
    return 0;
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
                << "Account='" << sampleParams->getAccount() << "', "
                << "Account2='" << sampleParams->getAccount2() << "'"
                << std::endl;
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
                
    std::cout << "/instrument | --instrument | /i | -i" << std::endl;
    std::cout << "An instrument which you want to use in sample. For example, \"EUR/USD\"." << std::endl << std::endl;
            
    std::cout << "/account | --account " << std::endl;
    std::cout << "An account which you want to use in sample. Optional parameter." << std::endl << std::endl;
            
    std::cout << "/buysell | --buysell | /d | -d" << std::endl;
    std::cout << "The order direction. Possible values are: B - buy, S - sell." << std::endl << std::endl;
            
    std::cout << "/lots | --lots " << std::endl;
    std::cout << "Trade amount in lots. Optional parameter." << std::endl << std::endl;
            
    std::cout << "/login2 | --login2 | /l | -l" << std::endl;
    std::cout << "Your user name for second session." << std::endl << std::endl;
            
    std::cout << "/password2 | --password2 | /p | -p" << std::endl;
    std::cout << "Your password for second session." << std::endl << std::endl;
            
    std::cout << "/url2 | --url2 | /u | -u" << std::endl;
    std::cout << "The server URL for second session. For example, http://www.fxcorporate.com/Hosts.jsp." << std::endl << std::endl;
            
    std::cout << "/connection2 | --connection2 | /c | -c" << std::endl;
    std::cout << "The connection name for second session. For example, \"Demo\" or \"Real\"." << std::endl << std::endl;
            
    std::cout << "/sessionid2 | --sessionid2 " << std::endl;
    std::cout << "The database name for second session. Required only for users who have accounts in more than one database. Optional parameter." << std::endl << std::endl;
            
    std::cout << "/pin2 | --pin2 " << std::endl;
    std::cout << "Your pin code for second session. Optional argument. Required only for users who have a pin. Optional parameter." << std::endl << std::endl;
            
    std::cout << "/account2 | --account2 " << std::endl;
    std::cout << "An account for second session which you want to use in sample. Optional parameter." << std::endl << std::endl;
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
    if (strlen(loginParams->getLogin2()) == 0)
    {
        std::cout << LoginParams::Strings::login2NotSpecified << std::endl;
        return false;
    }
    if (strlen(loginParams->getPassword2()) == 0)
    {
        std::cout << LoginParams::Strings::password2NotSpecified << std::endl;
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


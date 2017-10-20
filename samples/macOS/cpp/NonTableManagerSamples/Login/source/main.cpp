#include "stdafx.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *);
void printSampleParams(std::string &, LoginParams *);
void printAccounts(IO2GSession *);

int main(int argc, char *argv[])
{
    std::string procName = "Login";
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
        printAccounts(session);
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

// Print accounts table
void printAccounts(IO2GSession *session)
{
    O2G2Ptr<IO2GResponseReaderFactory> readerFactory = session->getResponseReaderFactory();
    if (!readerFactory)
    {
        std::cout << "Cannot create response reader factory" << std::endl;
        return;
    }
    O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
    O2G2Ptr<IO2GResponse> response = loginRules->getTableRefreshResponse(Accounts);
    O2G2Ptr<IO2GAccountsTableResponseReader> accountsResponseReader = readerFactory->createAccountsTableReader(response);
    std::cout.precision(2);
    for (int i = 0; i < accountsResponseReader->size(); ++i)
    {
        O2G2Ptr<IO2GAccountRow> accountRow = accountsResponseReader->getRow(i);
        std::cout << "AccountID: " << accountRow->getAccountID() << ", "
                << "Balance: " << std::fixed << accountRow->getBalance() << std::endl;
    }
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


#include "stdafx.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "CommonSources.h"

#define MAX_TOKEN_SIZE 2048

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *);
void printSampleParams(std::string &, LoginParams *);
bool getReports(IO2GSession *session);

int main(int argc, char *argv[])
{
    std::string procName = "LoginWithToken";
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
    //// Login
    std::cout << "Connecting..." << std::endl;

    IO2GSession *session = CO2GTransport::createSession();

    SessionStatusListener *sessionListener = new SessionStatusListener(session, false,
                                                                       loginParams->getSessionID(),
                                                                       loginParams->getPin());
    session->subscribeSessionStatus(sessionListener);

    bool bConnected = login(session, sessionListener, loginParams);
    bool bWasError = false;

    if (bConnected)
    {
        std::cout << "Connected using the provided parameters\n" << std::endl;

        //// SSO login
        char sToken[MAX_TOKEN_SIZE] = {};
        const int iSize = session->getToken(sToken, sizeof(sToken));

        if ('\0' == sToken[0] || iSize <= 0)
            std::cout << "SSO token is not received " << std::endl;
        else
        {
            std::cout << "SSO token to use for new connection: " << sToken << std::endl;
            std::cout << "Connecting using SSO token..." << std::endl;

            IO2GSession* const ssoSession = CO2GTransport::createSession();
            SessionStatusListener* const ssoSessionListener = new SessionStatusListener(ssoSession, false, loginParams->getSessionID(), loginParams->getPin());

            ssoSession->subscribeSessionStatus(ssoSessionListener);
            ssoSessionListener->reset();
            
            ssoSession->loginWithToken(loginParams->getLogin(), sToken, loginParams->getURL(), loginParams->getConnection());
            bool waitRes = false, connected = false;
            while (false == (connected = ssoSessionListener->isConnected()))
            {
                waitRes = ssoSessionListener->waitEvents();

                static const bool sSignaled = (bool)WAIT_OBJECT_0;
                if (sSignaled == waitRes)
                    break;
            }

            if (connected && IO2GSessionStatus::O2GSessionStatus::Connected == ssoSession->getSessionStatus())
            {
                std::cout << "Connected" << std::endl;

                std::cout << "Performing SSO session logout..." << std::endl;

                logout(ssoSession, ssoSessionListener);
                while (IO2GSessionStatus::O2GSessionStatus::Disconnected != ssoSession->getSessionStatus())
                    ssoSessionListener->waitEvents();

                std::cout << "Done" << std::endl;
            }
            else
            {
                bWasError = true;
                std::cout << "ERROR: failed to connect using SSO token\n" << std::endl;
            }
            
            //// Cleanup
            std::cout << "Cleanup SSO session..." << std::endl;

            ssoSession->unsubscribeSessionStatus(ssoSessionListener);
            ssoSessionListener->release();
            ssoSession->release();

            std::cout << "Done" << std::endl;
        }

        //// Logout
        std::cout << "Performing session logout..." << std::endl;
        logout(session, sessionListener);
        std::cout << "Done" << std::endl;
    }
    else
    {
        bWasError = true;
        std::cout << "ERROR: failed to connect using the provided parameters\n" << std::endl;
    }
    std::cout << "Cleanup session..." << std::endl;

    session->unsubscribeSessionStatus(sessionListener);
    sessionListener->release();
    session->release();

    delete loginParams;
    loginParams = nullptr;

    std::cout << "Done" << std::endl;

    if (bWasError)
        return -1;
    return 0;
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
    std::cout << "Your user name. [!] Required [!]" << std::endl << std::endl;
    
    std::cout << "/password | --password | /p | -p" << std::endl;
    std::cout << "Your password. [!] Required [!]" << std::endl << std::endl;
    
    std::cout << "/url | --url | /u | -u" << std::endl;
    std::cout << "The server URL. For example, http://www.fxcorporate.com/Hosts.jsp. [!] Required [!]" << std::endl << std::endl;
    
    std::cout << "/connection | --connection | /c | -c" << std::endl;
    std::cout << "The connection name. For example, \"Demo\" or \"Real\". [!] Required [!]" << std::endl << std::endl;
    
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


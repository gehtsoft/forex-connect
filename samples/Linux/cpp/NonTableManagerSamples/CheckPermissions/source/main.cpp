#include "stdafx.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
bool checkPermissions(IO2GSession *, const char *);
std::string parseO2GPermissionStatus(O2GPermissionStatus status);

int main(int argc, char *argv[])
{
    std::string procName = "CheckPermissions";
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

    SessionStatusListener *sessionListener = new SessionStatusListener(session, false,
                                                                       loginParams->getSessionID(),
                                                                       loginParams->getPin());
    session->subscribeSessionStatus(sessionListener);

    bool bConnected = login(session, sessionListener, loginParams);
    bool bWasError = false;

    if (bConnected)
    {
        if (checkPermissions(session, sampleParams->getInstrument()))
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
    delete sampleParams;

    if (bWasError)
        return -1;
    return 0;
}

// Print trading settings of the first account
bool checkPermissions(IO2GSession *session, const char* sInstrument)
{
    O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
    if (!loginRules)
    {
        std::cout << "Cannot get login rules" << std::endl;
        return false;
    }

    O2G2Ptr<IO2GPermissionChecker> permissionChecker = loginRules->getPermissionChecker();
    if (!permissionChecker)
    {
        std::cout << "Cannot get permission checker" << std::endl;
        return false;
    }

    std::cout << "canCreateMarketOpenOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canCreateMarketOpenOrder(sInstrument)) << std::endl;
    std::cout << "canChangeMarketOpenOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canChangeMarketOpenOrder(sInstrument)) << std::endl;
    std::cout << "canDeleteMarketOpenOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canDeleteMarketOpenOrder(sInstrument)) << std::endl;
    std::cout << "canCreateMarketCloseOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canCreateMarketCloseOrder(sInstrument)) << std::endl;
    std::cout << "canChangeMarketCloseOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canChangeMarketCloseOrder(sInstrument)) << std::endl;
    std::cout << "canDeleteMarketCloseOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canDeleteMarketCloseOrder(sInstrument)) << std::endl;
    std::cout << "canCreateEntryOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canCreateEntryOrder(sInstrument)) << std::endl;
    std::cout << "canChangeEntryOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canChangeEntryOrder(sInstrument)) << std::endl;
    std::cout << "canDeleteEntryOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canDeleteEntryOrder(sInstrument)) << std::endl;
    std::cout << "canCreateStopLimitOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canCreateStopLimitOrder(sInstrument)) << std::endl;
    std::cout << "canChangeStopLimitOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canChangeStopLimitOrder(sInstrument)) << std::endl;
    std::cout << "canDeleteStopLimitOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canDeleteStopLimitOrder(sInstrument)) << std::endl;
    std::cout << "canRequestQuote = " <<
            parseO2GPermissionStatus(permissionChecker->canRequestQuote(sInstrument)) << std::endl;
    std::cout << "canAcceptQuote = " <<
            parseO2GPermissionStatus(permissionChecker->canAcceptQuote(sInstrument)) << std::endl;
    std::cout << "canDeleteQuote = " <<
            parseO2GPermissionStatus(permissionChecker->canDeleteQuote(sInstrument)) << std::endl;
    std::cout << "canJoinToNewContingencyGroup = " <<
            parseO2GPermissionStatus(permissionChecker->canJoinToNewContingencyGroup(sInstrument)) << std::endl;
    std::cout << "canJoinToExistingContingencyGroup = " <<
            parseO2GPermissionStatus(permissionChecker->canJoinToExistingContingencyGroup(sInstrument)) << std::endl;
    std::cout << "canRemoveFromContingencyGroup = " <<
            parseO2GPermissionStatus(permissionChecker->canRemoveFromContingencyGroup(sInstrument)) << std::endl;
    std::cout << "canChangeOfferSubscription = " <<
            parseO2GPermissionStatus(permissionChecker->canChangeOfferSubscription(sInstrument)) << std::endl;
    std::cout << "canCreateNetCloseOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canCreateNetCloseOrder(sInstrument)) << std::endl;
    std::cout << "canChangeNetCloseOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canChangeNetCloseOrder(sInstrument)) << std::endl;
    std::cout << "canDeleteNetCloseOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canDeleteNetCloseOrder(sInstrument)) << std::endl;
    std::cout << "canCreateNetStopLimitOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canCreateNetStopLimitOrder(sInstrument)) << std::endl;
    std::cout << "canChangeNetStopLimitOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canChangeNetStopLimitOrder(sInstrument)) << std::endl;
    std::cout << "canDeleteNetStopLimitOrder = " <<
            parseO2GPermissionStatus(permissionChecker->canDeleteNetStopLimitOrder(sInstrument)) << std::endl;
    std::cout << "canUseDynamicTrailingForStop = " <<
            parseO2GPermissionStatus(permissionChecker->canUseDynamicTrailingForStop()) << std::endl;
    std::cout << "canUseDynamicTrailingForLimit = " <<
            parseO2GPermissionStatus(permissionChecker->canUseDynamicTrailingForLimit()) << std::endl;
    std::cout << "canUseDynamicTrailingForEntryStop = " <<
            parseO2GPermissionStatus(permissionChecker->canUseDynamicTrailingForEntryStop()) << std::endl;
    std::cout << "canUseDynamicTrailingForEntryLimit = " <<
            parseO2GPermissionStatus(permissionChecker->canUseDynamicTrailingForEntryLimit()) << std::endl;
    std::cout << "canUseFluctuateTrailingForStop = " <<
            parseO2GPermissionStatus(permissionChecker->canUseFluctuateTrailingForStop()) << std::endl;
    std::cout << "canUseFluctuateTrailingForLimit = " <<
            parseO2GPermissionStatus(permissionChecker->canUseFluctuateTrailingForLimit()) << std::endl;
    std::cout << "canUseFluctuateTrailingForEntryStop = " <<
            parseO2GPermissionStatus(permissionChecker->canUseFluctuateTrailingForEntryStop()) << std::endl;
    std::cout << "canUseFluctuateTrailingForEntryLimit = " <<
            parseO2GPermissionStatus(permissionChecker->canUseFluctuateTrailingForEntryLimit()) << std::endl;
    return true;
}

std::string parseO2GPermissionStatus(O2GPermissionStatus status)
{
    switch(status)
    {
    case PermissionDisabled:
        return std::string("Permission Disabled");
    case PermissionEnabled:
        return std::string("Permission Enabled");
    case PermissionHidden:
        return std::string("Permission Hidden");
	case PermissionUnknown:
		return std::string("Permission Unknown");
    default:
        return std::string("Unknown Permission Status");
    }
    return std::string("");
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
        std::cout << "Instrument: " << sampleParams->getInstrument() << std::endl;
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
    std::cout << "An instrument which you want to use in sample. For example, \"EUR/USD\". Mandatory parameter." << std::endl << std::endl;
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

    return true;
}


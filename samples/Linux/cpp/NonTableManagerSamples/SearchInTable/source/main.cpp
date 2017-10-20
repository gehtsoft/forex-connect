#include "stdafx.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
void findOrder(IO2GSession *, const char *, const char *, ResponseListener *);

int main(int argc, char *argv[])
{
    std::string procName = "SearchInTable";
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

    IO2GSession *session = CO2GTransport::createSession();

    SessionStatusListener *sessionListener = new SessionStatusListener(session, true,
                                                                       loginParams->getSessionID(),
                                                                       loginParams->getPin());
    session->subscribeSessionStatus(sessionListener);

    bool bConnected = login(session, sessionListener, loginParams);

    if (bConnected)
    {
        bool bIsAccountEmpty = !sampleParams->getAccount() || strlen(sampleParams->getAccount()) == 0;
        O2G2Ptr<IO2GAccountRow> account = getAccount(session, sampleParams->getAccount());
        ResponseListener *responseListener = new ResponseListener(session);
        session->subscribeResponse(responseListener);
        if (account)
        {
            if (bIsAccountEmpty)
            {
                sampleParams->setAccount(account->getAccountID());
                std::cout << "Account: " << sampleParams->getAccount() << std::endl;
            }
            findOrder(session, sampleParams->getAccount(), sampleParams->getOrderID(), responseListener);
            std::cout << "Done!" << std::endl;
        }
        else
        {
            std::cout << "No valid accounts" << std::endl;
            bWasError = true;
        }
        session->unsubscribeResponse(responseListener);
        responseListener->release();
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

void findOrder(IO2GSession *session, const char *sAccountID, const char *sOrderID, ResponseListener *responseListener)
{
    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
    if (!requestFactory)
    {
        std::cout << "Cannot create request factory" << std::endl;
        return;
    }
    O2G2Ptr<IO2GRequest> request = requestFactory->createRefreshTableRequestByAccount(Orders, sAccountID);
    if (!request)
    {
        std::cout << requestFactory->getLastError() << std::endl;
        return;
    }
    responseListener->setRequestID(request->getRequestID());
    session->sendRequest(request);
    if (!responseListener->waitEvents())
    {
        std::cout << "Response waiting timeout expired" << std::endl;
        return;
    }
    O2G2Ptr<IO2GResponse> orderResponse = responseListener->getResponse();
    if (orderResponse)
    {
        if (orderResponse->getType() == GetOrders)
        {
            O2G2Ptr<IO2GResponseReaderFactory> responseReaderFactory = session->getResponseReaderFactory();
            bool bFound = false;
            O2G2Ptr<IO2GOrdersTableResponseReader> responseReader = responseReaderFactory->createOrdersTableReader(orderResponse);
            for (int i = 0; i < responseReader->size(); ++i)
            {
                O2G2Ptr<IO2GOrderRow> order = responseReader->getRow(i);
                if (strcmp(sOrderID, order->getOrderID()) == 0)
                {
                    std::cout << "OrderID='" << order->getOrderID() << "', "
                            << "AccountID='" << order->getAccountID() << "', "
                            << "Type='" << order->getType() << "', "
                            << "Status='" << order->getStatus() << "', "
                            << "OfferID='" << order->getOfferID() << "', "
                            << "Amount='" << order->getAmount() << "', "
                            << "BuySell='" << order->getBuySell() << "', "
                            << "Rate='" << order->getRate() << "', "
                            << "TimeInForce='" << order->getTimeInForce() << "'"
                            << std::endl;
                    bFound = true;
                    break;
                }
            }
            if (!bFound)
                std::cout << "Order '" << sOrderID << "' is not found" << std::endl;
        }
    }
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
        std::cout << "Account='" << sampleParams->getAccount() << "', "
                << "OrderID='" << sampleParams->getOrderID() << "'"
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
                
    std::cout << "/account | --account " << std::endl;
    std::cout << "An account which you want to use in sample. Optional parameter." << std::endl << std::endl;

    std::cout << "/orderid | --orderid " << std::endl;
    std::cout << "Order, for which you want to display information. Mandatory argument." << std::endl << std::endl;
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
    if (strlen(sampleParams->getOrderID()) == 0)
    {
        std::cout << SampleParams::Strings::orderidNotSpecified << std::endl;
        return false;
    }

    return true;
}


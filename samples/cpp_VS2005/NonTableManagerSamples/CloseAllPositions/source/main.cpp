#include "stdafx.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

enum OrderSide
{
   Buy,
   Sell,
   Both
};

struct CloseOrderData
{
    OrderSide side;
    std::string account;
};

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
IO2GRequest *createCloseAllRequest(IO2GSession *, std::map<std::string, CloseOrderData> &);
bool getCloseOrdersData(IO2GSession *, ResponseListener *, const char *, std::map<std::string, CloseOrderData> &);

int main(int argc, char *argv[])
{
    std::string procName = "CloseAllPositions";
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
            std::map<std::string, CloseOrderData> closeOrdersData;
            if (getCloseOrdersData(session, responseListener, sampleParams->getAccount(), closeOrdersData))
            {
                O2G2Ptr<IO2GRequest> request = createCloseAllRequest(session, closeOrdersData);
                if (request)
                {
                    std::vector<std::string> requestIDs(request->getChildrenCount());
                    for (int i = 0; i < request->getChildrenCount(); ++i)
                    {
                        IO2GRequest *requestChild = request->getChildRequest(i);
                        requestIDs[i] = requestChild->getRequestID();
                        requestChild->release();
                    }
                    responseListener->setRequestIDs(requestIDs);
                    session->sendRequest(request);
                    if (responseListener->waitEvents())
                    {
                        Sleep(1000); // Wait for the balance update
                        std::cout << "Done!" << std::endl;
                    }
                    else
                    {
                        std::cout << "Response waiting timeout expired" << std::endl;
                        bWasError = true;
                    }
                }
                else
                {
                    std::cout << "Cannot create request" << std::endl;
                    bWasError = true;
                }
            }
            else
            {
                std::cout << "There are no opened positions" << std::endl;
                bWasError = true;
            }
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

IO2GRequest *createCloseAllRequest(IO2GSession *session, std::map<std::string, CloseOrderData> &closeOrdersData)
{
    if (closeOrdersData.empty())
        return NULL;

    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
    std::map<std::string, CloseOrderData>::iterator iter;
    O2G2Ptr<IO2GValueMap> batchValuemap = requestFactory->createValueMap();
    batchValuemap->setString(Command, O2G2::Commands::CreateOrder);

    for (iter = closeOrdersData.begin(); iter != closeOrdersData.end(); ++iter)
    {
        std::string sOfferID = iter->first;
        std::string sAccountID = iter->second.account;
        OrderSide side = iter->second.side;
        std::string sOrderType = O2G2::Orders::TrueMarketClose;

        switch (side)
        {
        case Buy:
        {
            O2G2Ptr<IO2GValueMap> childValuemap = requestFactory->createValueMap();
            childValuemap->setString(Command, O2G2::Commands::CreateOrder);
            childValuemap->setString(NetQuantity, "Y");
            childValuemap->setString(OrderType, sOrderType.c_str());
            childValuemap->setString(AccountID, sAccountID.c_str());
            childValuemap->setString(OfferID, sOfferID.c_str());
            childValuemap->setString(BuySell, O2G2::Buy);
            batchValuemap->appendChild(childValuemap);
        }
        break;
        case Sell:
        {
            O2G2Ptr<IO2GValueMap> childValuemap = requestFactory->createValueMap();
            childValuemap->setString(Command, O2G2::Commands::CreateOrder);
            childValuemap->setString(NetQuantity, "Y");
            childValuemap->setString(OrderType, sOrderType.c_str());
            childValuemap->setString(AccountID, sAccountID.c_str());
            childValuemap->setString(OfferID, sOfferID.c_str());
            childValuemap->setString(BuySell, O2G2::Sell);
            batchValuemap->appendChild(childValuemap);
        }
        break;
        case Both:
        {
            O2G2Ptr<IO2GValueMap> buyValuemap = requestFactory->createValueMap();
            buyValuemap->setString(Command, O2G2::Commands::CreateOrder);
            buyValuemap->setString(NetQuantity, "Y");
            buyValuemap->setString(OrderType, sOrderType.c_str());
            buyValuemap->setString(AccountID, sAccountID.c_str());
            buyValuemap->setString(OfferID, sOfferID.c_str());
            buyValuemap->setString(BuySell, O2G2::Buy);
            batchValuemap->appendChild(buyValuemap);

            O2G2Ptr<IO2GValueMap> sellValuemap = requestFactory->createValueMap();
            sellValuemap->setString(Command, O2G2::Commands::CreateOrder);
            sellValuemap->setString(NetQuantity, "Y");
            sellValuemap->setString(OrderType, sOrderType.c_str());
            sellValuemap->setString(AccountID, sAccountID.c_str());
            sellValuemap->setString(OfferID, sOfferID.c_str());
            sellValuemap->setString(BuySell, O2G2::Sell);
            batchValuemap->appendChild(sellValuemap);
        }
        break;
        }
    }
    IO2GRequest *request = requestFactory->createOrderRequest(batchValuemap);
    if (!request)
    {
        std::cout << requestFactory->getLastError() << std::endl;
        return NULL;
    }
    return request;
}

bool getCloseOrdersData(IO2GSession *session, ResponseListener *responseListener, const char *sAccountID, std::map<std::string, CloseOrderData> &closeOrdersData)
{
    closeOrdersData.clear();
    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
    if (!requestFactory)
    {
        std::cout << "Cannot create request factory" << std::endl;
        return false;
    }
    O2G2Ptr<IO2GRequest> request = requestFactory->createRefreshTableRequestByAccount(Trades, sAccountID);
    if (!request) {
        std::cout << requestFactory->getLastError() << std::endl;
        return false;
    }
    responseListener->setRequestID(request->getRequestID());
    session->sendRequest(request);
    if (!responseListener->waitEvents())
    {
        std::cout << "Response waiting timeout expired" << std::endl;
        return false;
    }

    O2G2Ptr<IO2GResponse> response = responseListener->getResponse();

    if (response)
    {
        O2G2Ptr<IO2GResponseReaderFactory> readerFactory = session->getResponseReaderFactory();
        O2G2Ptr<IO2GTradesTableResponseReader> responseReader = readerFactory->createTradesTableReader(response);
        for (int i = 0; i < responseReader->size(); ++i)
        {
            O2G2Ptr<IO2GTradeRow> trade = responseReader->getRow(i);
            std::string sOfferID = trade->getOfferID();
            std::string sBuySell = trade->getBuySell();
            // Set opposite side
            OrderSide side = (sBuySell == O2G2::Buy) ? Sell : Buy;
            if (closeOrdersData.find(sOfferID) == closeOrdersData.end())
            {
                CloseOrderData data;
                data.account = trade->getAccountID();
                data.side = side;
                closeOrdersData[sOfferID] = data;
            }
            else
            {
                OrderSide currentSide = closeOrdersData[sOfferID].side;
                if (currentSide != Both && currentSide != side)
                    closeOrdersData[sOfferID].side = Both;
            }
        }
    }
    return !closeOrdersData.empty();
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
        std::cout << "Account='" << sampleParams->getAccount() << "'"
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

    return true;
}


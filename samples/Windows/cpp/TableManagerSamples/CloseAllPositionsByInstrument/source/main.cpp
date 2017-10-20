#include "stdafx.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "TableListener.h"
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
    std::string offerID;
    OrderSide side;
    std::string account;
};

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
IO2GRequest *createCloseMarketNettingOrderRequest(IO2GSession *, CloseOrderData *);
bool getCloseOrdersData(IO2GTableManager *, const char *, const char *, CloseOrderData &);

int main(int argc, char *argv[])
{
    std::string procName = "CloseAllPositionsByInstrument";
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
    session->useTableManager(Yes, 0);

    SessionStatusListener *sessionListener = new SessionStatusListener(session, false,
                                                                       loginParams->getSessionID(),
                                                                       loginParams->getPin());
    session->subscribeSessionStatus(sessionListener);

    bool bConnected = login(session, sessionListener, loginParams);
    bool bWasError = false;

    if (bConnected)
    {
        bool bIsAccountEmpty = !sampleParams->getAccount() || strlen(sampleParams->getAccount()) == 0;
        ResponseListener *responseListener = new ResponseListener();
        TableListener *tableListener = new TableListener(responseListener);
        session->subscribeResponse(responseListener);

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

        O2G2Ptr<IO2GAccountRow> account = getAccount(tableManager, sampleParams->getAccount());

        if (account)
        {
            if (bIsAccountEmpty)
            {
                sampleParams->setAccount(account->getAccountID());
                std::cout << "Account: " << sampleParams->getAccount() << std::endl;
            }
            O2G2Ptr<IO2GOfferRow> offer = getOffer(tableManager, sampleParams->getInstrument());
            if (offer)
            {
                CloseOrderData closeOrdersData;
                if (getCloseOrdersData(tableManager, sampleParams->getAccount(), offer->getOfferID(), closeOrdersData))
                {
                    O2G2Ptr<IO2GRequest> request = createCloseMarketNettingOrderRequest(session, &closeOrdersData);
                    if (request)
                    {
                        tableListener->subscribeEvents(tableManager);

                        std::vector<std::string> requestIDs(request->getChildrenCount());
                        for (int i = 0; i < request->getChildrenCount(); ++i)
                        {
                            IO2GRequest *requestChild = request->getChildRequest(i);
                            requestIDs[i] = requestChild->getRequestID();
                            requestChild->release();
                        }
                        responseListener->setRequestIDs(requestIDs);
                        tableListener->setRequestIDs(requestIDs);
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

                        tableListener->unsubscribeEvents(tableManager);
                        tableListener->release();
                    }
                    else
                    {
                        std::cout << "Cannot create request" << std::endl;
                        bWasError = true;
                    }
                }
                else
                {
                    std::cout << "There are no opened positions for instrument '" <<
                            sampleParams->getInstrument() << "'" << std::endl;
                }
            }
            else
            {
                std::cout << "The instrument '" << sampleParams->getInstrument() << "' is not valid" << std::endl;
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

IO2GRequest *createCloseMarketNettingOrderRequest(IO2GSession *session, CloseOrderData *closeOrdersData)
{
    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
     O2G2Ptr<IO2GValueMap> batchValuemap = requestFactory->createValueMap();
    batchValuemap->setString(Command, O2G2::Commands::CreateOrder);

    std::string sOfferID = closeOrdersData->offerID;
    std::string sAccountID = closeOrdersData->account;
    OrderSide side = closeOrdersData->side;

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

    IO2GRequest *request = requestFactory->createOrderRequest(batchValuemap);
    if (!request)
    {
        std::cout << requestFactory->getLastError() << std::endl;
        return NULL;
    }
    return request;
}

bool getCloseOrdersData(IO2GTableManager *tableManager, const char *sAccountID, const char *sOfferID, CloseOrderData &closeOrdersData)
{
    O2G2Ptr<IO2GTradesTable> tradesTable = (IO2GTradesTable *)tableManager->getTable(Trades);

    bool bIsTradesFound = false;
    if (tradesTable)
    {
        for (int i = 0; i < tradesTable->size(); ++i)
        {
            O2G2Ptr<IO2GTradeRow> trade = tradesTable->getRow(i);
            if (strcmp(sOfferID, trade->getOfferID()) != 0 || strcmp(sAccountID, trade->getAccountID()) != 0)
                continue;

            bIsTradesFound = true;
            std::string sBuySell = trade->getBuySell();
            // Set opposite side
            OrderSide side = (sBuySell == O2G2::Buy) ? Sell : Buy;
            if (closeOrdersData.offerID != sOfferID)
            {
                closeOrdersData.offerID = sOfferID;
                closeOrdersData.account = sAccountID;
                closeOrdersData.side = side;
            }
            else
            {
                OrderSide currentSide = closeOrdersData.side;
                if (currentSide != Both && currentSide != side)
                    closeOrdersData.side = Both;
            }
        }
    }
    return bIsTradesFound;
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
                << "Account='" << sampleParams->getAccount() << "'"
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


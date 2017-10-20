#include "stdafx.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "TableListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
IO2GRequest *createELSRequest(IO2GSession *, const char *, const char *, int,
        double, double, double, const char *, const char *);
void findOrdersByRequestID(IO2GTableManager *, const char *);
void findOrdersByTypeAndDirection(IO2GTableManager *, const char *, const char *);
void findConditionalOrders(IO2GTableManager *);

int main(int argc, char *argv[])
{
    std::string procName = "FindRowByColumnValue";
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
    const char *sOrderType = O2G2::Orders::LimitEntry;
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
                O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
                if (loginRules)
                {
                    O2G2Ptr<IO2GTradingSettingsProvider> tradingSettingsProvider = loginRules->getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider->getBaseUnitSize(sampleParams->getInstrument(), account);
                    int iAmount = iBaseUnitSize * sampleParams->getLots();

                    double dRate;
                    double dRateStop;
                    double dRateLimit;
                    double dBid = offer->getBid();
                    double dAsk = offer->getAsk();
                    double dPointSize = offer->getPointSize();

                    // For the purpose of this example we will place entry order 8 pips from the current market price
                    // and attach stop and limit orders 10 pips from an entry order price
                    if (strcmp(sOrderType, O2G2::Orders::LimitEntry)==0)
                    {
                        if (strcmp(sampleParams->getBuySell(), O2G2::Buy)==0)
                        {
                            dRate = dAsk - 8 * dPointSize;
                            dRateLimit = dRate + 10 * dPointSize;
                            dRateStop = dRate - 10 * dPointSize;
                        }
                        else
                        {
                            dRate = dBid + 8 * dPointSize;
                            dRateLimit = dRate - 10 * dPointSize;
                            dRateStop = dRate + 10 * dPointSize;
                        }
                    }
                    else
                    {
                        if (strcmp(sampleParams->getBuySell(), O2G2::Buy)==0)
                        {
                            dRate = dAsk + 8 * dPointSize;
                            dRateLimit = dRate + 10 * dPointSize;
                            dRateStop = dRate - 10 * dPointSize;
                        }
                        else
                        {
                            dRate = dBid - 8 * dPointSize;
                            dRateLimit = dRate - 10 * dPointSize;
                            dRateStop = dRate + 10 * dPointSize;
                        }
                    }

                    O2G2Ptr<IO2GRequest> request = createELSRequest(session, offer->getOfferID(), account->getAccountID(), iAmount,
                            dRate, dRateLimit, dRateStop, sampleParams->getBuySell(), sOrderType);
                    if (request)
                    {
                        tableListener->subscribeEvents(tableManager);

                        responseListener->setRequestID(request->getRequestID());
                        tableListener->setRequestID(request->getRequestID());
                        session->sendRequest(request);
                        if (responseListener->waitEvents())
                        {
                            std::cout << "Search by RequestID: " << request->getRequestID() << std::endl;
                            findOrdersByRequestID(tableManager, request->getRequestID());
                            std::cout << "Search by Type: " << sOrderType << " and BuySell: " << sampleParams->getBuySell() << std::endl;
                            findOrdersByTypeAndDirection(tableManager, sOrderType, sampleParams->getBuySell());
                            std::cout << "Search conditional orders" << std::endl;
                            findConditionalOrders(tableManager);

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
                    std::cout << "Cannot get login rules" << std::endl;
                    bWasError = true;
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

IO2GRequest *createELSRequest(IO2GSession *session, const char *sOfferID, const char *sAccountID, int iAmount,
        double dRate, double dRateLimit, double dRateStop, const char *sBuySell, const char *sOrderType)
{
    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
    if (!requestFactory)
    {
        std::cout << "Cannot create request factory" << std::endl;
        return NULL;
    }
    O2G2Ptr<IO2GValueMap> valuemap = requestFactory->createValueMap();
    valuemap->setString(Command, O2G2::Commands::CreateOrder);
    valuemap->setString(OrderType, sOrderType);
    valuemap->setString(AccountID, sAccountID);
    valuemap->setString(OfferID, sOfferID);
    valuemap->setString(BuySell, sBuySell);
    valuemap->setInt(Amount, iAmount);
    valuemap->setDouble(Rate, dRate);
    valuemap->setDouble(RateLimit, dRateLimit);
    valuemap->setDouble(RateStop, dRateStop);
    valuemap->setString(CustomID, "EntryOrderWithStopLimit");

    O2G2Ptr<IO2GRequest> request = requestFactory->createOrderRequest(valuemap);
    if (!request)
    {
        std::cout << requestFactory->getLastError() << std::endl;
        return NULL;
    }
    return request.Detach();
}

// Find orders by request ID and print it
void findOrdersByRequestID(IO2GTableManager *tableManager, const char *sRequestID)
{
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)tableManager->getTable(Orders);
    IO2GTableIterator ordersIterator;
    IO2GOrderTableRow *orderRow = NULL;
    while (ordersTable->getNextRowByColumnValue("RequestID", sRequestID, ordersIterator, orderRow))
    {
        std::cout << "Order: " << orderRow->getOrderID() << std::endl;
        std::cout << "RequestID='" << orderRow->getRequestID() << "', "
                << "Type='" << orderRow->getType() << "', "
                << "BuySell='" << orderRow->getBuySell() << "', "
                << "AccountID='" << orderRow->getAccountID() << "', "
                << "Status='" << orderRow->getStatus() << "', "
                << "OfferID='" << orderRow->getOfferID() << "', "
                << "Amount='" << orderRow->getAmount() << "', "
                << "Rate='" << orderRow->getRate() << "', "
                << "TimeInForce='" << orderRow->getTimeInForce() << "'"
                << std::endl;
        orderRow->release();
    }
}

// Find orders by Type and BuySell and print it
void findOrdersByTypeAndDirection(IO2GTableManager *tableManager, const char *sOrderType, const char *sBuySell)
{
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)tableManager->getTable(Orders);
    IO2GTableIterator ordersIterator;
    IO2GOrderTableRow *orderRow = NULL;
    const char *names[] = {"Type", "BuySell"};
    const void *values[] = {(void *)sOrderType, (void *)sBuySell};
	O2GRelationalOperators ops[] = { EqualTo };
    while (ordersTable->getNextRowByMultiColumnValues(2, names, ops, values, OperatorOR, ordersIterator, orderRow))
    {
        std::cout << "Order: " << orderRow->getOrderID() << std::endl;
        std::cout << "RequestID='" << orderRow->getRequestID() << "', "
                << "Type='" << orderRow->getType() << "', "
                << "BuySell='" << orderRow->getBuySell() << "', "
                << "AccountID='" << orderRow->getAccountID() << "', "
                << "Status='" << orderRow->getStatus() << "', "
                << "OfferID='" << orderRow->getOfferID() << "', "
                << "Amount='" << orderRow->getAmount() << "', "
                << "Rate='" << orderRow->getRate() << "', "
                << "TimeInForce='" << orderRow->getTimeInForce() << "'"
                << std::endl;
        orderRow->release();
    }
}

// Find conditional orders and print it
void findConditionalOrders(IO2GTableManager *tableManager)
{
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)tableManager->getTable(Orders);
    IO2GTableIterator ordersIterator;
    IO2GOrderTableRow *orderRow = NULL;

    const void *values[] = {(void *)O2G2::Orders::LimitEntry, (void *)O2G2::Orders::StopEntry,
            (void *)O2G2::Orders::Stop, (void *)O2G2::Orders::Limit,
            (void *)O2G2::Orders::StopTrailingEntry, (void *)O2G2::Orders::LimitTrailingEntry};
    while (ordersTable->getNextRowByColumnValues("Type", EqualTo, 6, values, ordersIterator, orderRow))
    {
        std::cout << "Order: " << orderRow->getOrderID() << std::endl;
        std::cout << "RequestID='" << orderRow->getRequestID() << "', "
                << "Type='" << orderRow->getType() << "', "
                << "BuySell='" << orderRow->getBuySell() << "', "
                << "AccountID='" << orderRow->getAccountID() << "', "
                << "Status='" << orderRow->getStatus() << "', "
                << "OfferID='" << orderRow->getOfferID() << "', "
                << "Amount='" << orderRow->getAmount() << "', "
                << "Rate='" << orderRow->getRate() << "', "
                << "TimeInForce='" << orderRow->getTimeInForce() << "'"
                << std::endl;
        orderRow->release();
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
            
    std::cout << "/buysell | --buysell | /d | -d" << std::endl;
    std::cout << "The order direction. Possible values are: B - buy, S - sell." << std::endl << std::endl;
            
    std::cout << "/lots | --lots " << std::endl;
    std::cout << "Trade amount in lots. Optional parameter." << std::endl << std::endl;
            
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


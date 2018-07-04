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
IO2GRequest *createNetEntryOrderRequest(IO2GSession *, const char *, const char *, double, const char *, const char *);
IO2GTradeRow *getTrade(IO2GTableManager *, const char *, const char *);

int main(int argc, char *argv[])
{
    std::string procName = "NetStopLimit";
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
                O2G2Ptr<IO2GTradeRow> trade = getTrade(tableManager, sampleParams->getAccount(), offer->getOfferID());
                if (trade)
                {
                    const char *sBuySell = strcmp(trade->getBuySell(), O2G2::Buy) == 0 ? O2G2::Sell : O2G2::Buy;
                    O2G2Ptr<IO2GRequest> requestStop = createNetEntryOrderRequest(session, offer->getOfferID(),
                            account->getAccountID(), sampleParams->getRateStop(), sBuySell, O2G2::Orders::StopEntry);
                    if (requestStop)
                    {
                        tableListener->subscribeEvents(tableManager);

                        responseListener->setRequestID(requestStop->getRequestID());
                        tableListener->setRequestID(requestStop->getRequestID());
                        session->sendRequest(requestStop);
                        if (responseListener->waitEvents())
                        {
                            O2G2Ptr<IO2GRequest> requestLimit = createNetEntryOrderRequest(session, offer->getOfferID(),
                                    account->getAccountID(), sampleParams->getRateLimit(), sBuySell, O2G2::Orders::LimitEntry);
                            if (requestLimit)
                            {
                                responseListener->setRequestID(requestLimit->getRequestID());
                                tableListener->setRequestID(requestLimit->getRequestID());
                                session->sendRequest(requestLimit);
                                if (responseListener->waitEvents())
                                {
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
                    std::cout << "There are no opened positions for instrument '" <<
                            sampleParams->getInstrument() << "'" << std::endl;
					bWasError = true;
                }
            }
            else
            {
                std::cout << "The instrument '" << sampleParams->getInstrument() <<
                        "' is not valid" << std::endl;
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
        tableListener->unsubscribeEvents(tableManager);
        tableListener->release();
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

IO2GRequest *createNetEntryOrderRequest(IO2GSession *session, const char *sOfferID,
        const char *sAccountID, double dRate, const char *sBuySell, const char *sOrderType)
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
    valuemap->setString(NetQuantity, "Y");
    valuemap->setDouble(Rate, dRate);
    valuemap->setString(CustomID, "EntryOrder");
    O2G2Ptr<IO2GRequest> request = requestFactory->createOrderRequest(valuemap);
    if (!request)
    {
        std::cout << requestFactory->getLastError() << std::endl;
        return NULL;
    }
    return request.Detach();
}

// Find the first opened position by AccountID and OfferID
IO2GTradeRow *getTrade(IO2GTableManager *tableManager, const char *sAccountID, const char *sOfferID)
{
    O2G2Ptr<IO2GTradesTable> tradesTable = (IO2GTradesTable *)tableManager->getTable(Trades);
    for (int i = 0; i < tradesTable->size(); ++i)
    {
        O2G2Ptr<IO2GTradeRow> trade = tradesTable->getRow(i);
        if (strcmp(sAccountID, trade->getAccountID()) == 0 && 
                strcmp(sOfferID, trade->getOfferID()) == 0)
            return trade.Detach();
    }
    return NULL;
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
                << "Account='" << sampleParams->getAccount() << "', "
                << "RateStop='" << sampleParams->getRateStop() << "', "
                << "RateLimit='" << sampleParams->getRateLimit() << "'"
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
            
    std::cout << "/ratestop | --ratestop " << std::endl;
    std::cout << "Rate of the net stop order." << std::endl << std::endl;
            
    std::cout << "/ratelimit | --ratelimit " << std::endl;
    std::cout << "Rate of the net limit order." << std::endl << std::endl;
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
    if (isNaN(sampleParams->getRateStop()))
    {
        std::cout << SampleParams::Strings::ratestopNotSpecified << std::endl;
        return false;
    }
    if (isNaN(sampleParams->getRateLimit()))
    {
        std::cout << SampleParams::Strings::ratelimitNotSpecified << std::endl;
        return false;
    }

    return true;
}


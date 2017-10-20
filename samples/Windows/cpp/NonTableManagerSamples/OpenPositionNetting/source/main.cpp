#include "stdafx.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
IO2GRequest *createTrueMarketOrderRequest(IO2GSession *, const char *, const char *, int, const char *);
IO2GTradesTableResponseReader *getTradesTable(IO2GSession *, const char *, ResponseListener *);

int main(int argc, char *argv[])
{
    std::string procName = "OpenPositionNetting";
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
        if (account && strcmp(account->getMaintenanceType(), "0") == 0) // netting account
        {
            if (bIsAccountEmpty)
            {
                sampleParams->setAccount(account->getAccountID());
                std::cout << "Account: " << sampleParams->getAccount() << std::endl;
            }
            O2G2Ptr<IO2GOfferRow> offer = getOffer(session, sampleParams->getInstrument());
            if (offer)
            {
                O2G2Ptr<IO2GTradesTableResponseReader> tradesTable = getTradesTable(session, sampleParams->getAccount(), responseListener);
                if (tradesTable)
                {
                    responseListener->setTradesTable(tradesTable);
                    O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
                    if (loginRules)
                    {
                        O2G2Ptr<IO2GTradingSettingsProvider> tradingSettingsProvider = loginRules->getTradingSettingsProvider();
                        int iBaseUnitSize = tradingSettingsProvider->getBaseUnitSize(sampleParams->getInstrument(), account);
                        int iAmount = iBaseUnitSize * sampleParams->getLots();
                        O2G2Ptr<IO2GRequest> request = createTrueMarketOrderRequest(session, offer->getOfferID(), account->getAccountID(), iAmount, sampleParams->getBuySell());
                        if (request)
                        {
                            responseListener->setRequestID(request->getRequestID());
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
                        std::cout << "Cannot get login rules" << std::endl;
                        bWasError = true;
                    }
                }
                else
                {
                    std::cout << "Cannot get trades table" << std::endl;
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

// Get initial Trades state
IO2GTradesTableResponseReader *getTradesTable(IO2GSession *session, const char *sAccountID, ResponseListener *responseListener)
{
    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
    if (!requestFactory)
    {
        std::cout << "Cannot create request factory" << std::endl;
        return NULL;
    }
    O2G2Ptr<IO2GRequest> refreshTrades = requestFactory->createRefreshTableRequestByAccount(Trades, sAccountID);
    responseListener->setRequestID(refreshTrades->getRequestID());
    session->sendRequest(refreshTrades);
    if (!responseListener->waitEvents())
    {
        std::cout << "Response waiting timeout expired" << std::endl;
        return NULL;
    }
    O2G2Ptr<IO2GResponse> tradeResponse = responseListener->getResponse();
    if (tradeResponse)
    {
        O2G2Ptr<IO2GResponseReaderFactory> readerFactory = session->getResponseReaderFactory();
        if (readerFactory)
        {
            O2G2Ptr<IO2GTradesTableResponseReader> tradesTable = readerFactory->createTradesTableReader(tradeResponse);
            if (tradesTable)
                return tradesTable.Detach();
        }
    }
    return NULL;
}

IO2GRequest *createTrueMarketOrderRequest(IO2GSession *session, const char *sOfferID, const char *sAccountID, int iAmount, const char *sBuySell)
{
    O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
    if (!requestFactory)
    {
        std::cout << "Cannot create request factory" << std::endl;
        return NULL;
    }
    O2G2Ptr<IO2GValueMap> valuemap = requestFactory->createValueMap();
    valuemap->setString(Command, O2G2::Commands::CreateOrder);
    valuemap->setString(OrderType, O2G2::Orders::TrueMarketOpen);
    valuemap->setString(AccountID, sAccountID);
    valuemap->setString(OfferID, sOfferID);
    valuemap->setString(BuySell, sBuySell);
    valuemap->setInt(Amount, iAmount);
    valuemap->setString(CustomID, "TrueMarketOrder");
    O2G2Ptr<IO2GRequest> request = requestFactory->createOrderRequest(valuemap);
    if (!request)
    {
        std::cout << requestFactory->getLastError() << std::endl;
        return NULL;
    }
    return request.Detach();
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


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
IO2GRequest *createOTOCORequest(IO2GSession *, const char *, const char *, int, double, double, double);
void fillRequestIDs(std::vector<std::string> &, IO2GRequest *);

int main(int argc, char *argv[])
{
    std::string procName = "CreateOTOCO";
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
                O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
                if (loginRules)
                {
                    O2G2Ptr<IO2GTradingSettingsProvider> tradingSettingsProvider = loginRules->getTradingSettingsProvider();
                    int iBaseUnitSize = tradingSettingsProvider->getBaseUnitSize(sampleParams->getInstrument(), account);
                    int iAmount = iBaseUnitSize * sampleParams->getLots();

					double dRatePrimary = offer->getAsk() - 30.0 * offer->getPointSize();
					double dRateSecondary = offer->getAsk() + 15.0 * offer->getPointSize();
					double dRateThirdly = offer->getAsk() - 15.0 * offer->getPointSize();
					O2G2Ptr<IO2GRequest> request = createOTOCORequest(session, offer->getOfferID(), account->getAccountID(), iAmount, dRatePrimary, dRateSecondary, dRateThirdly);
                    if (request)
                    {
                        tableListener->subscribeEvents(tableManager);

						std::vector<std::string> requestIDs;
						fillRequestIDs(requestIDs, request);

                        responseListener->setRequestIDs(requestIDs);
                        tableListener->setRequestIDs(requestIDs);
                        session->sendRequest(request);

                        if (responseListener->waitEvents())
                        {
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

IO2GRequest *createOTOCORequest(IO2GSession *session, const char *sOfferID, const char *sAccountID, int iAmount, double dRatePrimary, double dRateOcoFirst, double dRateOcoSecond)
{
	O2G2Ptr<IO2GRequestFactory> requestFactory = session->getRequestFactory();
	if (requestFactory)
	{
		// Create OTO command
		O2G2Ptr<IO2GValueMap> valuemapMain = requestFactory->createValueMap();
		valuemapMain->setString(Command, O2G2::Commands::CreateOTO);

		// Create Entry order
		O2G2Ptr<IO2GValueMap> valuemapPrimary = requestFactory->createValueMap();
		valuemapPrimary->setString(Command, O2G2::Commands::CreateOrder);
		valuemapPrimary->setString(OrderType, O2G2::Orders::StopEntry);
		valuemapPrimary->setString(AccountID, sAccountID);
		valuemapPrimary->setString(OfferID, sOfferID);
		valuemapPrimary->setString(BuySell, O2G2::Sell);
		valuemapPrimary->setInt(Amount, iAmount);
		valuemapPrimary->setDouble(Rate, dRatePrimary);

		// Create OCO group of orders
		O2G2Ptr<IO2GValueMap> valuemapOCO = requestFactory->createValueMap();
		valuemapOCO->setString(Command, O2G2::Commands::CreateOCO);

		// Create Entry orders to OCO
		O2G2Ptr<IO2GValueMap> valuemapOCOFirst = requestFactory->createValueMap();
		valuemapOCOFirst->setString(Command, O2G2::Commands::CreateOrder);
		valuemapOCOFirst->setString(OrderType, O2G2::Orders::StopEntry);
		valuemapOCOFirst->setString(AccountID, sAccountID);
		valuemapOCOFirst->setString(OfferID, sOfferID);
		valuemapOCOFirst->setString(BuySell, O2G2::Buy);
		valuemapOCOFirst->setInt(Amount, iAmount);
		valuemapOCOFirst->setDouble(Rate, dRateOcoFirst);

		O2G2Ptr<IO2GValueMap> valuemapOCOSecond = requestFactory->createValueMap();
		valuemapOCOSecond->setString(Command, O2G2::Commands::CreateOrder);
		valuemapOCOSecond->setString(OrderType, O2G2::Orders::StopEntry);
		valuemapOCOSecond->setString(AccountID, sAccountID);
		valuemapOCOSecond->setString(OfferID, sOfferID);
		valuemapOCOSecond->setString(BuySell, O2G2::Buy);
		valuemapOCOSecond->setInt(Amount, iAmount);
		valuemapOCOSecond->setDouble(Rate, dRateOcoSecond);

		// Fill the created groups. Please note, first you should add an entry order to OTO order and then OCO group of orders
		valuemapMain->appendChild(valuemapPrimary);;
		valuemapOCO->appendChild(valuemapOCOFirst);
		valuemapOCO->appendChild(valuemapOCOSecond);
		valuemapMain->appendChild(valuemapOCO);

		O2G2Ptr<IO2GRequest> request = requestFactory->createOrderRequest(valuemapMain);
		if (!request)
		{
			std::cout << requestFactory->getLastError() << std::endl;
			return NULL;
		}
		return request.Detach();
	}
	else
	{
		std::cout << "Cannot create request factory" << std::endl;
		return NULL;
	}
}

void fillRequestIDs(std::vector<std::string> &requestsIDs, IO2GRequest *request)
{
	int childrenCount = request->getChildrenCount();
	if (childrenCount == 0)
	{
		requestsIDs.push_back(request->getRequestID());
		return;
	}

	for (int i = 0; i < childrenCount; i++)
	{
		IO2GRequest *childRequest = request->getChildRequest(i);
		fillRequestIDs(requestsIDs, childRequest);
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
    if (sampleParams->getLots() <= 0)
    {
        std::cout << "'Lots' value " << sampleParams->getLots() << " is invalid" << std::endl;
        return false;
    }

    return true;
}


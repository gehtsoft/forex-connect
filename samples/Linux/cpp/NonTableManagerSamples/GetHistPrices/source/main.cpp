#include "stdafx.h"
#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);

void printPrices(IO2GSession *, IO2GResponse *);
bool getHistoryPrices(IO2GSession *, const char *, const char *, DATE, DATE, ResponseListener *);

int main(int argc, char *argv[])
{
    std::string procName = "GetHistPrices";
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
        ResponseListener *responseListener = new ResponseListener(session);
        session->subscribeResponse(responseListener);

        if (!getHistoryPrices(session, sampleParams->getInstrument(), sampleParams->getTimeframe(), 
                sampleParams->getDateFrom(), sampleParams->getDateTo(), responseListener))
            bWasError = true;

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

bool getHistoryPrices(IO2GSession *session, const char *sInstrument, const char *sTimeframe, DATE dtFrom, DATE dtTo, ResponseListener *responseListener)
{
    O2G2Ptr<IO2GRequestFactory> factory = session->getRequestFactory();
    if (!factory)
    {
        std::cout << "Cannot create request factory" << std::endl;
        return false;
    }
    //find timeframe by identifier
    O2G2Ptr<IO2GTimeframeCollection> timeframeCollection = factory->getTimeFrameCollection();
    O2G2Ptr<IO2GTimeframe> timeframe = timeframeCollection->get(sTimeframe);
    if (!timeframe)
    {
        std::cout << "Timeframe '" << sTimeframe << "' is incorrect!" << std::endl;
        return false;
    }
    O2G2Ptr<IO2GRequest> request = factory->createMarketDataSnapshotRequestInstrument(sInstrument, timeframe, timeframe->getQueryDepth());
    DATE dtFirst = dtTo;
     // there is limit for returned candles amount
    do
    {
        factory->fillMarketDataSnapshotRequestTime(request, dtFrom, dtFirst, false);
        responseListener->setRequestID(request->getRequestID());
        session->sendRequest(request);
        if (!responseListener->waitEvents())
        {
            std::cout << "Response waiting timeout expired" << std::endl;
            return false;
        }
        // shift "to" bound to oldest datetime of returned data
        O2G2Ptr<IO2GResponse> response = responseListener->getResponse();
        if (response && response->getType() == MarketDataSnapshot)
        {
            O2G2Ptr<IO2GResponseReaderFactory> readerFactory = session->getResponseReaderFactory();
            if (readerFactory)
            {
                O2G2Ptr<IO2GMarketDataSnapshotResponseReader> reader = readerFactory->createMarketDataSnapshotReader(response);
                if (reader->size() > 0)
                {
                    if (fabs(dtFirst - reader->getDate(0)) > 0.0001)
                        dtFirst = reader->getDate(0); // earliest datetime of returned data
                    else
                        break;
                }
                else
                {
                    std::cout << "0 rows received" << std::endl;
                    break;
                }
            }
            printPrices(session, response);
        }
        else
        {
            break;
        }
    } while (dtFirst - dtFrom > 0.0001);
    return true;
}

void printPrices(IO2GSession *session, IO2GResponse *response)
{
    if (response != 0)
    {
        if (response->getType() == MarketDataSnapshot)
        {
            std::cout << "Request with RequestID='" << response->getRequestID() << "' is completed:" << std::endl;
            O2G2Ptr<IO2GResponseReaderFactory> factory = session->getResponseReaderFactory();
            if (factory)
            {
                O2G2Ptr<IO2GMarketDataSnapshotResponseReader> reader = factory->createMarketDataSnapshotReader(response);
                if (reader)
                {
                    char sTime[20];
                    for (int ii = reader->size() - 1; ii >= 0; ii--)
                    {
                        DATE dt = reader->getDate(ii);
                        formatDate(dt, sTime);
                        if (reader->isBar())
                        {
                            printf("DateTime=%s, BidOpen=%f, BidHigh=%f, BidLow=%f, BidClose=%f, AskOpen=%f, AskHigh=%f, AskLow=%f, AskClose=%f, Volume=%i\n",
                                    sTime, reader->getBidOpen(ii), reader->getBidHigh(ii), reader->getBidLow(ii), reader->getBidClose(ii),
                                    reader->getAskOpen(ii), reader->getAskHigh(ii), reader->getAskLow(ii), reader->getAskClose(ii), reader->getVolume(ii));
                        }
                        else
                        {
                            printf("DateTime=%s, Bid=%f, Ask=%f\n", sTime, reader->getBid(ii), reader->getAsk(ii));
                        }
                    }
                }
            }
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
        std::cout << "Instrument='" << sampleParams->getInstrument() << "', "
                  << "Timeframe='" << sampleParams->getTimeframe() << "', ";
        if (isNaN(sampleParams->getDateFrom()))
        {
            std::cout << "DateFrom='', ";
        }
        else
        {
            char sDateFrom[20];
            formatDate(sampleParams->getDateFrom(), sDateFrom);
            std::cout << "DateFrom='" << sDateFrom << "', ";
        }
        if (isNaN(sampleParams->getDateTo()))
        {
            std::cout << "DateTo='', ";
        }
        else
        {
            char sDateTo[20];
            formatDate(sampleParams->getDateTo(), sDateTo);
            std::cout << "DateTo='" << sDateTo << "', ";
        }
        std::cout << std::endl;
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

    std::cout << "/timeframe | --timeframe " << std::endl;
    std::cout << "Time period which forms a single candle. For example, m1 - for 1 minute, H1 - for 1 hour." << std::endl << std::endl;

    std::cout << "/datefrom | --datefrom " << std::endl;
    std::cout << "Date/time from which you want to receive historical prices. If you leave this argument as it is, it will mean from last trading day. Format is \"m.d.Y H:M:S\". Optional parameter." << std::endl << std::endl;

    std::cout << "/dateto | --dateto " << std::endl;
    std::cout << "Datetime until which you want to receive historical prices. If you leave this argument as it is, it will mean to now. Format is \"m.d.Y H:M:S\". Optional parameter." << std::endl;
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
    if (strlen(sampleParams->getTimeframe()) == 0)
    {
        std::cout << SampleParams::Strings::timeframeNotSpecified << std::endl;
        return false;
    }

    bool bIsDateFromNotSpecified = false;
    bool bIsDateToNotSpecified = false;
    DATE dtFrom = sampleParams->getDateFrom();
    DATE dtTo = sampleParams->getDateTo();
    char sDateFrom[20];
    char sDateTo[20];

    time_t tNow = time(NULL); // get time now
    struct tm *tmNow = gmtime(&tNow);
    DATE dtNow = 0;
    CO2GDateUtils::CTimeToOleTime(tmNow, &dtNow);

    if (isNaN(dtFrom))
    {
        bIsDateFromNotSpecified = true;
        dtFrom = 0;
        sampleParams->setDateFrom(dtFrom);
    }
    else
    {
        if (dtFrom - dtNow > 0.0001)
        {
            formatDate(dtFrom, sDateFrom);
            std::cout << "'DateFrom' value " << dtFrom << " is invalid" << std::endl;
            return false;
        }
    }

    if (isNaN(dtTo))
    {
        bIsDateToNotSpecified = true;
        dtTo = 0;
        sampleParams->setDateTo(dtTo);
    }
    else
    {
        if (!bIsDateFromNotSpecified && dtFrom - dtTo > 0.001)
        {
            formatDate(dtTo, sDateTo);
            std::cout << "'DateTo' value " << dtTo << " is invalid" << std::endl;
            return false;
        }
    }

    return true;
}


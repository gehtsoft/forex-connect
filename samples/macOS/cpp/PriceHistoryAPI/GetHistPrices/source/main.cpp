/* Copyright 2019 FXCM Global Services, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use these files except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
#include "stdafx.h"

#include "ResponseListener.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "CommonSources.h"
#include "CommunicatorStatusListener.h"
#include "LocalFormat.h"

/** Forward declaration. */
void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *, SampleParams *);
void printSampleParams(std::string &, LoginParams *, SampleParams *);
void writePrices(pricehistorymgr::IPriceHistoryCommunicator *communicator, pricehistorymgr::IPriceHistoryCommunicatorResponse *response,
    const char *instrument, const char *outputFile, bool utcMode);
bool getHistoryPrices(pricehistorymgr::IPriceHistoryCommunicator *communicator, const char *instrument,
    const char *timeframe, DATE from, DATE to, int quotesCount, ResponseListener *responseListener, const char *outputFile, bool utcMode);
int getInstrumentPrecision(pricehistorymgr::IPriceHistoryCommunicator *communicator, const char *instrument);

/** The sample's entry point.

    How does it work:
    - create ForexConnect session;
    - create IPriceHistoryCommunicator using the session;
    - login to ForexConnect;
    - wait until IPriceHistoryCommunicator is ready;
    - create and send a history request;
    - wait and write results to a CSV file;
    - cleanup and exit.
 */
int main(int argc, char *argv[])
{
    std::string procName = "GetHistPrices";
    if (argc == 1)
    {
        printHelp(procName);
        return -1;
    }

    bool bWasError = false;

    LoginParams loginParams(argc, argv);
    SampleParams sampleParams(argc, argv);

    printSampleParams(procName, &loginParams, &sampleParams);
    if (!checkObligatoryParams(&loginParams, &sampleParams))
        return -1;

    // create the ForexConnect trading session
    O2G2Ptr<IO2GSession> session = CO2GTransport::createSession();
    O2G2Ptr<SessionStatusListener> statusListener = new SessionStatusListener(session, true,
                                                                              loginParams.getSessionID(),
                                                                              loginParams.getPin());

    // subscribe IO2GSessionStatus interface implementation for the status events
    session->subscribeSessionStatus(statusListener);
    statusListener->reset();

    // create an instance of IPriceHistoryCommunicator
    pricehistorymgr::IError *error = NULL;
    O2G2Ptr<pricehistorymgr::IPriceHistoryCommunicator> communicator(
        pricehistorymgr::PriceHistoryCommunicatorFactory::createCommunicator(session, "History", &error));
    O2G2Ptr<pricehistorymgr::IError> autoError(error);

    if (!communicator)
    {
        std::cout << error->getMessage() << std::endl;
        return -1;
    }

    // log in to ForexConnect
    session->login(loginParams.getLogin(), loginParams.getPassword(),
        loginParams.getURL(), loginParams.getConnection());

    if (statusListener->waitEvents() && statusListener->isConnected())
    {
        O2G2Ptr<CommunicatorStatusListener> communicatorStatusListener(new CommunicatorStatusListener());
        communicator->addStatusListener(communicatorStatusListener);

        // wait until the communicator signals that it is ready
        if (communicator->isReady() ||
            communicatorStatusListener->waitEvents() && communicatorStatusListener->isReady())
        {
            // attach the instance of the class that implements the IPriceHistoryCommunicatorListener
            // interface to the communicator
            O2G2Ptr<ResponseListener> responseListener(new ResponseListener());
            communicator->addListener(responseListener);

            bool utcMode = (strcmp(sampleParams.getTimezone(), "UTC") == 0);
            if (!getHistoryPrices(communicator, sampleParams.getInstrument(),
                sampleParams.getTimeframe(), sampleParams.getDateFrom(), sampleParams.getDateTo(),
                sampleParams.getQuotesCount(), responseListener, sampleParams.getOutputFile(), utcMode))
                bWasError = true;

            std::cout << "Done!" << std::endl;

            communicator->removeListener(responseListener);
        }

        communicator->removeStatusListener(communicatorStatusListener);
        statusListener->reset();

        session->logout();
        statusListener->waitEvents();
    }
    else
    {
        bWasError = true;
    }

    session->unsubscribeSessionStatus(statusListener);

    if (bWasError)
        return -1;
    return 0;
}

/** Request historical prices for the specified timeframe of the specified period.

    @param communicator
        The price history communicator.
    @param instrument
        The instrument.
    @param timeframe
        The timeframe.
    @param from
        From-date.
    @param to
        To-date
    @param quotesCount
        The quotes count.
    @param responseListener
        The response listener.
    @param outputFile
        The output file name.
 */
bool getHistoryPrices(pricehistorymgr::IPriceHistoryCommunicator *communicator, const char *instrument,
                      const char *timeframe, DATE from, DATE to, int quotesCount,
                      ResponseListener *responseListener, const char *outputFile, bool utcMode)
{
    if (!communicator->isReady())
    {
        std::cout << "History communicator is not ready." << std::endl;
        return false;
    }

    // create timeframe entity
    O2G2Ptr<pricehistorymgr::ITimeframeFactory> timeframeFactory = communicator->getTimeframeFactory();
    pricehistorymgr::IError *error = NULL;
    O2G2Ptr<IO2GTimeframe> timeframeObj = timeframeFactory->create(timeframe, &error);
    O2G2Ptr<pricehistorymgr::IError> autoError(error);
    if (!timeframeObj)
    {
        std::cout << "Timeframe '" << timeframe << "' is incorrect! " << std::endl;
        return false;
    }

    // create and send a history request
    O2G2Ptr<pricehistorymgr::IPriceHistoryCommunicatorRequest> request =
        communicator->createRequest(instrument, timeframeObj, from, to, quotesCount, &error);
    if (!request)
    {
        std::cout << error->getMessage() << std::endl;
        return false;
    }

    responseListener->setRequest(request);
    if (!communicator->sendRequest(request, &error))
    {
        std::cout << error->getMessage() << std::endl;
        return false;
    }

    // wait results
    responseListener->wait();

    // print results if any
    O2G2Ptr<pricehistorymgr::IPriceHistoryCommunicatorResponse> response = responseListener->getResponse();
    if (response)
        writePrices(communicator, response, instrument, outputFile, utcMode);

    return true;
}

/** Gets precision of a specified instrument.

    @param communicator
        The price history communicator.
    @param instrument
        The instrument.
    @return
        The precision.
 */
int getInstrumentPrecision(pricehistorymgr::IPriceHistoryCommunicator *communicator, const char *instrument)
{
    int precision = 6;

    O2G2Ptr<quotesmgr::IQuotesManager> quotesManager = communicator->getQuotesManager();
    quotesmgr::IError *error = NULL;
    O2G2Ptr<quotesmgr::IInstruments> instruments = quotesManager->getInstruments(&error);
    O2G2Ptr<quotesmgr::IError> autoError(error);
    if (instruments)
    {
        O2G2Ptr<quotesmgr::IInstrument> instr = instruments->find(instrument);
        if (instr)
            precision = instr->getPrecision();
    }

    return precision;
}

/** Writes history data from response.

    @param communicator
        The price history communicator.
    @param response
        The response. Cannot be null.
    @param instrument
        The instrument.
    @param outputFile
        The output file name.
 */
void writePrices(pricehistorymgr::IPriceHistoryCommunicator *communicator, pricehistorymgr::IPriceHistoryCommunicatorResponse *response,
                 const char *instrument, const char *outputFile, bool utcMode)
{
    std::fstream fs;
    fs.open(outputFile, std::fstream::out | std::fstream::trunc);
    if (!fs.is_open())
    {
        std::cout << "Could not open the output file." << std::endl;
        return;
    }

    LocalFormat localFormat;
    const char *separator = localFormat.getListSeparator();
    int precision = getInstrumentPrecision(communicator, instrument);

    // use IO2GMarketDataSnapshotResponseReader to extract price data from the response object 
    pricehistorymgr::IError *error = NULL;
    O2G2Ptr<IO2GMarketDataSnapshotResponseReader> reader = communicator->createResponseReader(response, &error);
    O2G2Ptr<pricehistorymgr::IError> autoError(error);
    if (reader)
    {
        if (reader->isBar())
        {
            fs << "DateTime" << separator
               << "BidOpen"  << separator << "BidHigh"  << separator
               << "BidLow"   << separator << "BidClose" << separator
               << "AskOpen"  << separator << "AskHigh"  << separator
               << "AskLow"   << separator << "AskClose" << separator
               << "Volume"   << separator << std::endl;
        }
        else
        {
            fs << "DateTime" << separator
               << "Bid"      << separator << "Ask" << separator << std::endl;
        }

        for (int i = 0; i < reader->size(); ++i)
        {
            DATE dt = reader->getDate(i);

            if (!utcMode)
                dt = hptools::date::DateConvertTz(dt, hptools::date::UTC, hptools::date::EST);
            
            std::string time = localFormat.formatDate(dt);            
            if (reader->isBar())
            {
                fs << time << separator
                   << localFormat.formatDouble(reader->getBidOpen(i), precision)  << separator
                   << localFormat.formatDouble(reader->getBidHigh(i), precision)  << separator
                   << localFormat.formatDouble(reader->getBidLow(i), precision)   << separator
                   << localFormat.formatDouble(reader->getBidClose(i), precision) << separator
                   << localFormat.formatDouble(reader->getAskOpen(i), precision)  << separator
                   << localFormat.formatDouble(reader->getAskHigh(i), precision)  << separator
                   << localFormat.formatDouble(reader->getAskLow(i), precision)   << separator
                   << localFormat.formatDouble(reader->getAskClose(i), precision) << separator
                   << localFormat.formatDouble(reader->getVolume(i), 0)   << separator
                   << std::endl;
            }
            else
            {
                fs << time << separator
                   << localFormat.formatDouble(reader->getBid(i), precision) << separator
                   << localFormat.formatDouble(reader->getAsk(i), precision) << separator
                   << std::endl;
            }
        }
    }

    fs.close();
}

/** Print sample parameters data.

    @param sProcName
        The sample process name.
    @param loginParams
        The LoginParams instance pointer.
    @param sampleParams
        The LoginParams instance pointer.
 */
void printSampleParams(std::string &sProcName, LoginParams *loginParams, SampleParams *sampleParams)
{
    std::cout << "Running " << sProcName << " with arguments:" << std::endl;

    LocalFormat localFormat;

    // login (common) information
    if (loginParams)
    {
        std::cout << loginParams->getLogin() << " * "
                  << loginParams->getURL() << " "
                  << loginParams->getConnection() << " "
                  << loginParams->getSessionID() << " "
                  << loginParams->getPin() << std::endl;
    }

    // sample specific information
    if (sampleParams)
    {
        std::cout << "Instrument='" << sampleParams->getInstrument() << "', "
                  << "Timeframe='" << sampleParams->getTimeframe() << "', ";

        const char *timezone = sampleParams->getTimezone();
        if (isNaN(sampleParams->getDateFrom()))
            std::cout << "DateFrom='', ";
        else
        {
            auto dateFrom = sampleParams->getDateFrom();

            if (strcmp(timezone, "EST") == 0)
                dateFrom = hptools::date::DateConvertTz(dateFrom, hptools::date::UTC, hptools::date::EST);

            std::cout << "DateFrom='" << localFormat.formatDate(dateFrom) << "' (" << timezone << "), ";
        }

        if (isNaN(sampleParams->getDateTo()))
            std::cout << "DateTo='', ";
        else
        {
            auto dateTo = sampleParams->getDateTo();

            if (strcmp(timezone, "EST") == 0)
                dateTo = hptools::date::DateConvertTz(dateTo, hptools::date::UTC, hptools::date::EST);

            std::cout << "DateTo='" << localFormat.formatDate(dateTo) << "' (" << timezone << "), ";
        }

        std::cout << "QuotesCount='" << sampleParams->getQuotesCount() << "'";

        std::cout << std::endl;
    }
}

/** Print expected sample-login parameters and their description. 

    @param sProcName
        The sample process name.
 */
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
    std::cout << "The database name. Required only for users who have accounts in more than one database. "
        "Optional parameter." << std::endl << std::endl;
                
    std::cout << "/pin | --pin " << std::endl;
    std::cout << "Your pin code. Required only for users who have a pin. "
        "Optional parameter." << std::endl << std::endl;
                
    std::cout << "/instrument | --instrument | /i | -i" << std::endl;
    std::cout << "An instrument which you want to use in sample. "
        "For example, \"EUR/USD\"." << std::endl << std::endl;

    std::cout << "/timeframe | --timeframe " << std::endl;
    std::cout << "Time period which forms a single candle. "
        "For example, m1 - for 1 minute, H1 - for 1 hour." << std::endl << std::endl;

    std::cout << "/datefrom | --datefrom " << std::endl;
    std::cout << "Date/time from which you want to receive historical prices. "
        "If you leave this argument as it is, it will mean from last trading day. "
        "Format is \"m.d.Y H:M:S\". Optional parameter." << std::endl << std::endl;

    std::cout << "/dateto | --dateto " << std::endl;
    std::cout << "Date/time until which you want to receive historical prices. "
        "If you leave this argument as it is, it will mean to now. Format is \"m.d.Y H:M:S\". "
        "Optional parameter." << std::endl << std::endl;

    std::cout << "/tz | --tz " << std::endl;
    std::cout << "Timezone for /datefrom and /dateto parameters: EST or UTC "
        "Optional parameter. Default: EST" << std::endl << std::endl;

    std::cout << "/count | --count " << std::endl;
    std::cout << "Count of historical prices you want to receive. If you "
        << "leave this argument as it is, it will mean -1 (use some default "
        << "value or ignore if datefrom is specified)" << std::endl << std::endl;

    std::cout << "/output | --output " << std::endl;
    std::cout << "The output file name." << std::endl;
}

/** Check parameters for correct values.

    @param loginParams
        The LoginParams instance pointer.
    @param sampleParams
        The SampleParams instance pointer.
    @return
        true if parameters are correct.
 */
bool checkObligatoryParams(LoginParams *loginParams, SampleParams *sampleParams)
{
    // check login parameters
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

    // check other parameters
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
    if (strlen(sampleParams->getOutputFile()) == 0)
    {
        std::cout << SampleParams::Strings::outputFileNotSpecified << std::endl;
        return false;
    }

    const char *timezone = sampleParams->getTimezone();
    if (strcmp(timezone, "EST") != 0 && strcmp(timezone, "UTC") != 0)
    {
        std::cout << SampleParams::Strings::timezoneNotSupported << ": " << timezone << std::endl;
        return false;
    }

    bool bIsDateFromNotSpecified = false;
    bool bIsDateToNotSpecified = false;
    DATE dtFrom = sampleParams->getDateFrom();
    DATE dtTo = sampleParams->getDateTo();

    time_t tNow = time(NULL); // get time now
    struct tm *tmNow = gmtime(&tNow);
    DATE dtNow = 0;
    CO2GDateUtils::CTimeToOleTime(tmNow, &dtNow);
    LocalFormat localFormat;

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
            std::cout << "Sorry, 'DateFrom' value " << localFormat.formatDate(dtFrom) << " should be in the past" << std::endl;
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
            std::cout << "Sorry, 'DateTo' value " << localFormat.formatDate(dtTo) << " should be later than 'DateFrom' value "
                << localFormat.formatDate(dtFrom) << std::endl;
            return false;
        }
    }

    return true;
}


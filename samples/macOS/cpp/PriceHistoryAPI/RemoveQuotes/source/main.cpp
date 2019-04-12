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

#include "QMDataCollection.h"
#include "QMData.h"
#include "CommonSources.h"
#include "LoginParams.h"
#include "SampleParams.h"
#include "RemoveQuotesListener.h"
#include "UpdateInstrumentsListener.h"
#include "LocalFormat.h"
#include "SessionStatusListener.h"
#include "CommunicatorStatusListener.h"

#include "util.h"

/** Forward declaration. */
void printHelp(std::string &sProcName);
void printSampleParams(std::string &sProcName, LoginParams *loginParams, SampleParams *sampleParams);
bool checkObligatoryParams(LoginParams *loginParams, SampleParams *sampleParams);
IQMDataCollection *getQuotes(quotesmgr::IQuotesManager *quotesManager, std::string instrument, int year);
void removeQuotes(quotesmgr::IQuotesManager *, SampleParams *sampleParams);
void removeLocalQuotes(quotesmgr::IQuotesManager *, const std::string&, int);
void removeData(quotesmgr::IQuotesManager *quotesManager, IQMDataCollection *quotes);
void startNextRemoveTask(quotesmgr::IQuotesManager *quotesManager, IQMData *data);
void showLocalQuotes(quotesmgr::IQuotesManager *);
IQMDataCollection *prepareListOfQMData(quotesmgr::IQuotesManager *quotesManager);
void showPreparedQMDataList(IQMDataCollection *collection);

/** The sample's entry point.
 */
int main(int argc, char **argv)
{
    std::string procName = "RemoveQuotes";
    if (argc < 3)
    {
        printHelp(procName);
        return -1;
    }

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
            O2G2Ptr<quotesmgr::IQuotesManager> quotesManager(communicator->getQuotesManager());
            removeQuotes(quotesManager, &sampleParams);
            std::cout << std::endl;
            showLocalQuotes(quotesManager);
        }
        communicator->removeStatusListener(communicatorStatusListener);

        statusListener->reset();
        session->logout();
        statusListener->waitEvents();
    }
    session->unsubscribeSessionStatus(statusListener);

    return 0;
}

/** Shows help information.
 */
void showHelp()
{
    std::cout << "Remove local quotes of some instrument for the specified year." << std::endl;
    std::cout << "usage: RemoveQuotes <instrument> <year>" << std::endl;
    std::cout << "where:" << std::endl;
    std::cout << "  instrument is a specific instrument such as EUR/USD" << std::endl;
    std::cout << "  year is a specific year." << std::endl;
    std::cout << std::endl;
}

/** Print expected sample-login parameters and their description.

@param sProcName
The sample process name.
*/
void printHelp(std::string &sProcName)
{
    std::cout << sProcName << " : " << "Remove local quotes of some instrument for the specified year." << std::endl;
    std::cout << " sample parameters:" << std::endl << std::endl;

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

    std::cout << "/year | --year | /y | -y" << std::endl;
    std::cout << "The specific year. For example, \"2018\"" << std::endl << std::endl;
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
    if (sampleParams->getYear() == -1)
    {
        std::cout << SampleParams::Strings::yearNotSpecified << std::endl;
        return false;
    }

    return true;
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
                  << "Year='" << sampleParams->getYear() << "'";
        std::cout << std::endl;
    }
}

/** Remove quotes.

    @param quotesManager
        The QuotesManager instance.
    @param arguments
        Parameters of remove command.
 */
void removeQuotes(quotesmgr::IQuotesManager *quotesManager, SampleParams *sampleParams)
{
    if (sampleParams->getYear() <= 0)
    {
        std::cout << "Error : year must be an integer value." << std::endl;
        return;
    }

    removeLocalQuotes(quotesManager, sampleParams->getInstrument(), sampleParams->getYear());
}

/** Removes quotes from local cache.

    @param quotesManager
        QuotesManager instance.
    @param instrument
        Instrument name.

    @param year
        Year.
 */
void removeLocalQuotes(quotesmgr::IQuotesManager *quotesManager, const std::string &instrument, int year)
{
    O2G2Ptr<IQMDataCollection> quotes(getQuotes(quotesManager, instrument, year));

    if (!quotes || quotes->size() == 0)
    {
        std::cout << "Error : There are no quotes to remove." << std::endl;
        return;
    }

    removeData(quotesManager, quotes);
}

/** Removes the data from cache.

    @param quotesManager
        QuotesManager instance.
    @param quotes
        Collection of quotes manager storage data.
    @param year
        Year.
 */
void removeData(quotesmgr::IQuotesManager *quotesManager, IQMDataCollection *quotes)
{
    while (quotes->size() > 0)
    {
        O2G2Ptr<IQMData> data(quotes->get(0));
        quotes->remove(0);

        startNextRemoveTask(quotesManager, data);
    }
}

/** Fills QuotesManager data for the specified instrument and year.

    @param quotesManager
        QuotesManager instance.
    @param instrument
        Instrument name.
    @param year
        Year.

    @return
        Collection of quotes manager storage data.
 */
IQMDataCollection *getQuotes(quotesmgr::IQuotesManager *quotesManager, std::string instrument, int year)
{
    O2G2Ptr<quotesmgr::IBaseTimeframes> baseTimeframes(quotesManager->getBaseTimeframes());
    O2G2Ptr<IQMDataCollection> quotes(new QMDataCollection());

    quotesmgr::IError *error = NULL;
    for(int i(0); i < baseTimeframes->size(); ++i)
    {
        std::string timeframe = baseTimeframes->get(i);
        quotesmgr::int64 size = quotesManager->getDataSize(instrument.c_str(), timeframe.c_str(), year, &error);

        if (error)
        {
            std::cout << error->getMessage() << std::endl;
            error->release();
            continue;
        }

        if (size > 0)
        {
            O2G2Ptr<IQMData> data(new QMData(instrument.c_str(), timeframe.c_str(), year, size));
            quotes->add(data);
        }
    }

    quotes->addRef();
    return quotes;
}

/** Tries to start next remove task.

    @param quotesManager
        QuotesManager instance.
    @param data
        Quotes manager storage data.
 */
void startNextRemoveTask(quotesmgr::IQuotesManager *quotesManager, IQMData *data)
{
    O2G2Ptr<RemoveQuotesListener> removeQuotesListener(new RemoveQuotesListener());
    quotesmgr::IError *error = NULL;

    O2G2Ptr<quotesmgr::IRemoveQuotesTask> removeTask(quotesManager->createRemoveQuotesTask(data->getInstrument(), 
        data->getTimeframe(), removeQuotesListener, &error));
    if (error)
    {
        std::cout << "Failed to create remove quotes task instance: " << error->getMessage() << std::endl;
        error->release();
        return;
    }

    removeTask->addYear(data->getYear());

    quotesManager->executeTask(removeTask, &error);
    if (error)
    {
        std::cout << "Failed to execute remove task: " << error->getMessage() << std::endl;
        error->release();
        return;
    }

    removeQuotesListener->waitEvents();
}

/** Shows quotes that are available in local cache.

    @param quotesManager
        QuotesManager instance.
 */
void showLocalQuotes(quotesmgr::IQuotesManager *quotesManager)
{
    // if the instruments list is not updated, update it now
    if (!quotesManager->areInstrumentsUpdated())
    {
        O2G2Ptr<UpdateInstrumentsListener> instrumentsCallback(new UpdateInstrumentsListener());

        quotesmgr::IError *error = NULL;
        O2G2Ptr<quotesmgr::IUpdateInstrumentsTask> task(quotesManager->createUpdateInstrumentsTask(instrumentsCallback, &error));
        if (error)
        {
            std::cout << "Failed to create update instruments task instance: " << error->getMessage() << std::endl;
            error->release();
            return;
        }

        quotesManager->executeTask(task, &error);
        if (error)
        {
            std::cout << "Failed to execute update instruments task: " << error->getMessage() << std::endl;
            error->release();
            return;
        }

        instrumentsCallback->waitEvents();
    }

    O2G2Ptr<IQMDataCollection> collection(prepareListOfQMData(quotesManager));
    showPreparedQMDataList(collection);
}

/** Prepares the collection of the quotes stored in the Quotes Manager cache.

    @param quotesManager
        QuotesManager instance.
    @return
        Collection of quotes manager storage data.
 */
IQMDataCollection *prepareListOfQMData(quotesmgr::IQuotesManager *quotesManager)
{
    O2G2Ptr<QMDataCollection> collection(new QMDataCollection());
    O2G2Ptr<quotesmgr::IBaseTimeframes> timeframes(quotesManager->getBaseTimeframes());

    quotesmgr::IError *error = NULL;
    O2G2Ptr<quotesmgr::IInstruments> instruments(quotesManager->getInstruments(&error));
    if (error)
    {
        std::cout << "Failed to get instruments: " << error->getMessage() << std::endl;
        error->release();

        return NULL;
    }

    for (int i = 0; i < instruments->size(); ++i)
    {
        O2G2Ptr<quotesmgr::IInstrument> instrument(instruments->get(i));

        for (int j = 0; j < timeframes->size(); ++j)
        {
            std::string timeframe = timeframes->get(j);

            const int oldestYear(util::convertYear(instrument->getOldestQuoteDate(timeframe.c_str())));
            const int latestYear(util::convertYear(instrument->getLatestQuoteDate(timeframe.c_str())));
            
            if (latestYear >= oldestYear)
            {
                for (int year = oldestYear; year <= latestYear; ++year)
                {
                    quotesmgr::int64 size = quotesManager->getDataSize(instrument->getName(), timeframe.c_str(), year);
                    if (size > 0)
                    {
                        O2G2Ptr<IQMData> data(new QMData(instrument->getName(), timeframe.c_str(), year, size));
                        collection->add(data);
                    }
                }
            }
        }
    }

    collection->addRef();
    return collection;
}

/** Show list of available quotes.

    @param collection
        Collection of quotes manager storage data.
 */
void showPreparedQMDataList(IQMDataCollection *collection)
{
    util::SummaryInfoType instrumentsInfo;
    if (!util::summariseInstrumentDataSize(collection, instrumentsInfo))
    {
        std::cout << "There are no quotes in the local storage." << std::endl;
        return;
    }

    std::cout << "Quotes in the local storage" << std::endl;
    std::for_each(instrumentsInfo.begin(), instrumentsInfo.end(), util::printInstrumentSummaryInfo);
}

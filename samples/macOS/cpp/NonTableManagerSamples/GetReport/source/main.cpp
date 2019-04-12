#include "stdafx.h"
#include "FileDownloader.h"
#include "SessionStatusListener.h"
#include "LoginParams.h"
#include "CommonSources.h"

#define MAX_URL_SIZE 2048

void printHelp(std::string &);
bool checkObligatoryParams(LoginParams *);
void printSampleParams(std::string &, LoginParams *);
bool getReports(IO2GSession *session);
void parseUrl(const char *url, std::string &protocolAndHost);

int main(int argc, char *argv[])
{
    std::string procName = "GetReport";
    if (argc == 1)
    {
        printHelp(procName);
        return -1;
    }

    LoginParams *loginParams = new LoginParams(argc, argv);

    printSampleParams(procName, loginParams);
    if (!checkObligatoryParams(loginParams))
    {
        delete loginParams;
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
        if (!getReports(session))
            bWasError = true;
        std::cout << "Done!" << std::endl;
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

    if (bWasError)
        return -1;
    return 0;
}

// Get reports for all accounts
bool getReports(IO2GSession *session)
{
    O2G2Ptr<IO2GLoginRules> loginRules = session->getLoginRules();
    if (!loginRules)
    {
        std::cout << "Cannot get login rules" << std::endl;
        return false;
    }
    O2G2Ptr<IO2GResponseReaderFactory> responseFactory = session->getResponseReaderFactory();
    O2G2Ptr<IO2GResponse> accountsResponse = loginRules->getTableRefreshResponse(Accounts);
    O2G2Ptr<IO2GAccountsTableResponseReader> accountsReader = responseFactory->createAccountsTableReader(accountsResponse);
    bool bWasError = false;
    for (int i = 0; i < accountsReader->size(); ++i)
    {
        O2G2Ptr<IO2GAccountRow> account = accountsReader->getRow(i);

        DATE dtFrom = 0.0;
        time_t t = time(0); // get time now
        struct tm *date = localtime(&t);
        CO2GDateUtils::CTimeToOleTime(date, &dtFrom);

        DATE dtTo = 0.0;
        // get previous month
        if (date->tm_mon == 0)
        {
            date->tm_year = date->tm_year - 1;
            date->tm_mon = date->tm_mon + 11;
        }
        else
        {
            date->tm_mon = date->tm_mon - 1;
        }
        CO2GDateUtils::CTimeToOleTime(date, &dtTo);

        // create buffer for the URL and get it
        char sUrl[MAX_URL_SIZE] = {};
            
        int iResult = session->getReportURL(sUrl, sizeof(sUrl), account->getAccountID(), dtFrom, dtTo, "html", 0);
        if (iResult > 0)
        {
            printf("AccountID=%s; Balance=%f; Report URL=%s\n",
                    account->getAccountID(), account->getBalance(), sUrl);
        }
        else
        {
            switch (iResult)
            {
            case ReportUrlNotSupported:
                bWasError = true;
                printf("Report URL is not supported!\n");
                break;
            case ReportUrlTooSmallBuffer:
                bWasError = true;
                printf("The buffer is too small!\n");
                break;
            }
        }

        std::string sReportTempFileName = std::tmpnam(NULL);
        FileDownloader::download(sUrl, sReportTempFileName.c_str());

        std::ifstream is(sReportTempFileName);
        if (!is)
        {
            bWasError = true;
            printf("Report is not received!\n");
            break;
        }
        
        std::stringstream reportBuffer;
        reportBuffer << is.rdbuf();
        is.close();
        std::remove(sReportTempFileName.c_str()); // delete temp file

        std::string prefix;
        parseUrl(sUrl, prefix);
        std::string report = CO2GHtmlContentUtils::replaceRelativePathWithAbsolute(reportBuffer.str().c_str(), prefix.c_str());

        std::string sReportFileName = account->getAccountID();
        sReportFileName.append(".html");
        std::ofstream os(sReportFileName.c_str());
        if (!os) 
        {
            bWasError = true;
            printf("Report is not created!\n");
            break;
        }
        else
        {
            os << report;
            os.close();
        }

        printf("Report is saved to %s\n", sReportFileName.c_str());
    }
    return bWasError == false;
}

void parseUrl(const char *url, std::string &protocolAndHost)
{
    const std::string url_s(url);
    const std::string prot_end("://");
    std::string::const_iterator prot_i = std::search(url_s.begin(), url_s.end(),
        prot_end.begin(), prot_end.end());

    advance(prot_i, prot_end.length());
    std::string::const_iterator path_i = std::find(prot_i, url_s.end(), '/');
    protocolAndHost.reserve(distance(url_s.begin(), path_i));
    protocolAndHost.assign(url_s.begin(), path_i);
}

void printSampleParams(std::string &sProcName, LoginParams *loginParams)
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
                
}

bool checkObligatoryParams(LoginParams *loginParams)
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


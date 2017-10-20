#include "stdafx.h"
#include "LoginParams.h"

const char *LoginParams::Strings::loginNotSpecified = "'Login' is not specified (/l|-l|/login|--login)";
const char *LoginParams::Strings::passwordNotSpecified = "'Password' is not specified (/p|-p|/password|--password)";
const char *LoginParams::Strings::urlNotSpecified = "'URL' is not specified (/u|-u|/url|--url)";
const char *LoginParams::Strings::connectionNotSpecified = "'Connection' is not specified (/c|-c|/connection|--connection)";

LoginParams::LoginParams(int argc, char **argv)
{
    /* Load parameters with short keys. */
    mLogin = getArgument(argc, argv, "l");
    mPassword = getArgument(argc, argv, "p");
    mURL = getArgument(argc, argv, "u");
    mConnection = getArgument(argc, argv, "c");

    /* If parameters with short keys not loaded, load with long keys. */
    if (mLogin.empty())
        mLogin = getArgument(argc, argv, "login");
    if (mPassword.empty())
        mPassword = getArgument(argc, argv, "password");
    if (mURL.empty())
        mURL = getArgument(argc, argv, "url");
    if (mConnection.empty())
        mConnection = getArgument(argc, argv, "connection");

    /* Load optional parameters. */
    mSessionID = getArgument(argc, argv, "sessionid");
    mPin = getArgument(argc, argv, "pin");

    /* Append "Hosts.jsp" to URL if needed. */
    if (mURL.length() != 0)
    {
        // TODO: case insensitive find.
        if (mURL.find("Hosts.jsp") == std::string::npos)
        {
            mURL += "/Hosts.jsp";
        }
    }
}

LoginParams::~LoginParams(void)
{
}

const char *LoginParams::getArgument(int argc, char **argv, const char *key)
{
    for (int i = 1; i < argc; ++i)
    {
        if (argv[i][0] == '-' || argv[i][0] == '/')
        {
            int iDelimOffset = 0;
            if (strncmp(argv[i], "--", 2) == 0)
                iDelimOffset = 2;
            else if (strncmp(argv[i], "-", 1) == 0 || strncmp(argv[i], "/", 1) == 0)
                iDelimOffset = 1;

            if (_stricmp(argv[i] + iDelimOffset, key) == 0 && argc > i+1)
                return argv[i+1];
        }
    }
    return "";
}

const char *LoginParams::getLogin()
{
    return mLogin.c_str();
}

const char *LoginParams::getPassword()
{
    return mPassword.c_str();
}

const char *LoginParams::getURL()
{
    return mURL.c_str();
}

const char *LoginParams::getConnection()
{
    return mConnection.c_str();
}

const char *LoginParams::getSessionID()
{
    return mSessionID.c_str();
}

const char *LoginParams::getPin()
{
    return mPin.c_str();
}

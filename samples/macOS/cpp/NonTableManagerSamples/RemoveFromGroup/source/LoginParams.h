#pragma once

class LoginParams
{
public:
    class Strings
    {
    public:
        static const char *loginNotSpecified;
        static const char *passwordNotSpecified;
        static const char *urlNotSpecified;
        static const char *connectionNotSpecified;
    };
public:
    LoginParams(int, char **);
    ~LoginParams(void);

    const char *getLogin();
    const char *getPassword();
    const char *getURL();
    const char *getConnection();
    const char *getSessionID();
    const char *getPin();

private:
    const char *getArgument(int, char **, const char *);

    std::string mLogin;
    std::string mPassword;
    std::string mURL;
    std::string mConnection;
    std::string mSessionID;
    std::string mPin;
};


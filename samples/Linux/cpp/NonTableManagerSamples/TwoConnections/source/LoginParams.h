#pragma once

class LoginParams
{
public:
    class Strings
    {
    public:
        static const char *loginNotSpecified;
        static const char *passwordNotSpecified;
        static const char *login2NotSpecified;
        static const char *password2NotSpecified;
        static const char *urlNotSpecified;
        static const char *connectionNotSpecified;
    };
public:
    LoginParams(int, char **);
    ~LoginParams(void);

    const char *getLogin();
    const char *getPassword();
    const char *getLogin2();
    const char *getPassword2();
    const char *getURL();
    const char *getConnection();
    const char *getSessionID();
    const char *getPin();
    const char *getSessionID2();
    const char *getPin2();

private:
    const char *getArgument(int, char **, const char *);

    std::string mLogin;
    std::string mPassword;
    std::string mLogin2;
    std::string mPassword2;
    std::string mURL;
    std::string mConnection;
    std::string mSessionID;
    std::string mPin;
    std::string mSessionID2;
    std::string mPin2;
};


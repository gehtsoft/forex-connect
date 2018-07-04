#pragma once
class LoginParams;
class SampleParams;
class SessionStatusListener;

class Connection : public sample_tools::AThread
{
 public:
    Connection(IO2GSession *, LoginParams *, SampleParams *, bool);
    ~Connection();
 private:
    virtual int run();
    IO2GSession *mSession;
    LoginParams *mLoginParams;
    SampleParams *mSampleParams;
    bool mIsFirstAccount;
    bool login(IO2GSession *, SessionStatusListener *, const char *, const char *, const char *, const char *);
    IO2GRequest *createTrueMarketOrderRequest(IO2GSession *, const char *, const char *, int, const char *);
};
#pragma once

class SampleParams
{
public:
    class Strings
    {
    public:
        static const char *instrumentNotSpecified;
        static const char *buysellNotSpecified;
        static const char *rateNotSpecified;
        static const char *ratestopNotSpecified;
        static const char *ratelimitNotSpecified;
        static const char *orderidNotSpecified;
        static const char *primaryidNotSpecified;
        static const char *secondaryidNotSpecified;
        static const char *contingencyidNotSpecified;
        static const char *timeframeNotSpecified;
        static const char *statusNotSpecified;
    };
public:
    SampleParams(int, char **);
    ~SampleParams(void);

    const char *getInstrument();
    const char *getBuySell();
    const char *getContingencyID();
    const char *getOrderID();
    const char *getPrimaryID();
    const char *getSecondaryID();
    const char *getTimeframe();
    const char *getAccount();
    const char *getAccount2();
    const char *getOrderType();
    const char *getStatus();
    int getLots();
    DATE getDateFrom();
    DATE getDateTo();
    double getRate();
    double getRateStop();
    double getRateLimit();

    void setAccount(const char *);
    void setAccount2(const char *);
    void setOrderType(const char *);
    void setDateFrom(DATE);
    void setDateTo(DATE);

private:
    const char *getArgument(int, char **, const char *);

    std::string mInstrument;
    std::string mBuySell;
    std::string mContingencyID;
    std::string mOrderID;
    std::string mPrimaryID;
    std::string mSecondaryID;
    std::string mTimeframe;
    std::string mAccount;
    std::string mAccount2;
    std::string mOrderType;
    std::string mStatus;
    int mLots;
    DATE mDateFrom;
    DATE mDateTo;
    double mRate;
    double mRateStop;
    double mRateLimit;
};


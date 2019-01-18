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
        static const char *rangeNotSpecified;
        static const char *trailStepNotSpecified;
        static const char *ratestopNotSpecified;
        static const char *ratelimitNotSpecified;
        static const char *orderidNotSpecified;
        static const char *primaryidNotSpecified;
        static const char *secondaryidNotSpecified;
        static const char *contingencyidNotSpecified;
        static const char *timeframeNotSpecified;
        static const char *statusNotSpecified;
        static const char *expDateNotSpecified;
        static const char *pegtypeNotSpecified;
        static const char *pegoffsetNotSpecified;
    };
public:
    SampleParams(int, char **);
    ~SampleParams(void);

    const char *getExpDate();
    const char *getInstrument();
    const char *getBuySell();
    const char *getContingencyID();
    const char *getOrderID();
    const char *getPrimaryID();
    const char *getSecondaryID();
    const char *getTimeframe();
    const char *getAccount();
    const char *getOrderType();
    const char *getStatus();
    int getLots();
    int getTrailStep();
    DATE getDateFrom();
    DATE getDateTo();
    double getRangeInPips();
    double getRate();
    double getRateStop();
    double getRateLimit();

    bool IsPegged();
    const char *getPegType();
    double getPegOffset();

    void setAccount(const char *);
    void setOrderType(const char *);
    void setDateFrom(DATE);
    void setDateTo(DATE);

private:
    const char *getArgument(int, char **, const char *);
    bool isKeyExist(int , char **, const char *);

    std::string mInstrument;
    std::string mBuySell;
    std::string mContingencyID;
    std::string mOrderID;
    std::string mPrimaryID;
    std::string mSecondaryID;
    std::string mTimeframe;
    std::string mAccount;
    std::string mOrderType;
    std::string mStatus;
    std::string mExpDate;
    int mLots;
    int mTrailStep;
    DATE mDateFrom;
    DATE mDateTo;
    double mRangeInPips;
    double mRate;
    double mRateStop;
    double mRateLimit;
    bool mIsPegged;
    std::string mPegType;
    double mPegOffset;
};


#include "stdafx.h"
#include <limits>
#include "SampleParams.h"

const char *SampleParams::Strings::instrumentNotSpecified = "'Instrument' is not specified (/i|-i|/instrument|--instrument)";
const char *SampleParams::Strings::buysellNotSpecified = "'BuySell' is not specified (/d|-d|/buysell|--buysell)";
const char *SampleParams::Strings::rateNotSpecified = "'Rate' is not specified (/r|-r|/rate|--rate)";
const char *SampleParams::Strings::rangeNotSpecified = "'Range' is not specified (/range|--range)";
const char *SampleParams::Strings::trailStepNotSpecified = "'TrailStep' is not specified (/trailstep|--trailstep)";
const char *SampleParams::Strings::ratestopNotSpecified = "'RateStop' is not specified (/ratestop|--ratestop)";
const char *SampleParams::Strings::ratelimitNotSpecified = "'RateLimit' is not specified (/ratelimit|--ratelimit)";
const char *SampleParams::Strings::orderidNotSpecified = "'OrderID' is not specified (/orderid|--orderid)";
const char *SampleParams::Strings::primaryidNotSpecified = "'PrimaryID' is not specified (/primaryid|--primaryid)";
const char *SampleParams::Strings::secondaryidNotSpecified = "'SecondaryID' is not specified (/secondaryid|--secondaryid)";
const char *SampleParams::Strings::contingencyidNotSpecified = "'ContingencyID' is not specified (/contingencyid|--contingencyid)";
const char *SampleParams::Strings::timeframeNotSpecified = "'Timeframe' is not specified (/timeframe|--timeframe)";
const char *SampleParams::Strings::statusNotSpecified = "'SubscriptionStatus' is not specified (/status|--status)";
const char *SampleParams::Strings::expDateNotSpecified = "'expDate' is not specified (/status|--status)";

SampleParams::SampleParams(int argc, char **argv)
{
    /* Load parameters with short keys. */
    mInstrument = getArgument(argc, argv, "i");
    mBuySell = getArgument(argc, argv, "d");

    std::string sRate = getArgument(argc, argv, "r");

    /* If parameters with short keys not loaded, load with long keys. */
    if (mInstrument.empty())
        mInstrument = getArgument(argc, argv, "instrument");
    if (mBuySell.empty())
        mBuySell = getArgument(argc, argv, "buysell");
    if (sRate.empty())
        sRate = getArgument(argc, argv, "rate");

    /* Load parameters with long keys. */
    mContingencyID = getArgument(argc, argv, "contingencyid");
    mOrderID = getArgument(argc, argv, "orderid");
    mPrimaryID = getArgument(argc, argv, "primaryid");
    mSecondaryID = getArgument(argc, argv, "secondaryid");
    mTimeframe = getArgument(argc, argv, "timeframe");
    mAccount = getArgument(argc, argv, "account");
    mOrderType = getArgument(argc, argv, "ordertype");
    mStatus = getArgument(argc, argv, "status");
    mExpDate = getArgument(argc, argv, "expireDate");

    std::string sLots = getArgument(argc, argv, "lots");
    std::string sDateFrom = getArgument(argc, argv, "datefrom");
    std::string sDateTo = getArgument(argc, argv, "dateto");
    std::string sRangeInPips = getArgument(argc, argv, "range");
    std::string sRateStop = getArgument(argc, argv, "ratestop");
    std::string sRateLimit = getArgument(argc, argv, "ratelimit");
    std::string sTrailStep = getArgument(argc, argv, "trailstep");

    /* Convert types. */
    if (sLots.empty())
        mLots = 1;
    else
        mLots = atoi(sLots.c_str());

    double const NaN = std::numeric_limits<double>::quiet_NaN();

    if (sRangeInPips.empty())
        mRangeInPips = NaN;
    else
        mRangeInPips = atoi(sRangeInPips.c_str());

    if (sTrailStep.empty())
        mTrailStep = NaN;
    else
        mTrailStep = atoi(sTrailStep.c_str());

    if (sRate.empty())
        mRate = NaN;
    else
        mRate = atof(sRate.c_str());

    if (sRangeInPips.empty())
        mRangeInPips = NaN;
    else
        mRangeInPips = atof(sRangeInPips.c_str());

    if (sRateStop.empty())
        mRateStop = NaN;
    else
        mRateStop = atof(sRateStop.c_str());

    if (sRateLimit.empty())
        mRateLimit = NaN;
    else
        mRateLimit = atof(sRateLimit.c_str());

    struct tm tmBuf = {0};

    if (sDateFrom.empty())
        mDateFrom = NaN;
    else
    {
        strptime(sDateFrom.c_str(), "%m.%d.%Y %H:%M:%S", &tmBuf);
        CO2GDateUtils::CTimeToOleTime(&tmBuf, &mDateFrom);
    }

    if (sDateTo.empty())
        mDateTo = NaN;
    else
    {
        strptime(sDateTo.c_str(), "%m.%d.%Y %H:%M:%S", &tmBuf);
        CO2GDateUtils::CTimeToOleTime(&tmBuf, &mDateTo);
    }

}

SampleParams::~SampleParams(void)
{
}

const char *SampleParams::getArgument(int argc, char **argv, const char *key)
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

/** Getters. */

const char *SampleParams::getExpDate()
{
    return mExpDate.c_str();
}

const char *SampleParams::getInstrument()
{
    return mInstrument.c_str();
}

const char *SampleParams::getBuySell()
{
    return mBuySell.c_str();
}

const char *SampleParams::getContingencyID()
{
    return mContingencyID.c_str();
}

const char *SampleParams::getOrderID()
{
    return mOrderID.c_str();
}

const char *SampleParams::getPrimaryID()
{
    return mPrimaryID.c_str();
}

const char *SampleParams::getSecondaryID()
{
    return mSecondaryID.c_str();
}

const char *SampleParams::getTimeframe()
{
    return mTimeframe.c_str();
}

const char *SampleParams::getAccount()
{
    return mAccount.c_str();
}

const char *SampleParams::getOrderType()
{
    return mOrderType.c_str();
}

const char *SampleParams::getStatus()
{
    return mStatus.c_str();
}

int SampleParams::getLots()
{
    return mLots;
}

int SampleParams::getTrailStep()
{
    return mTrailStep;
}

DATE SampleParams::getDateFrom()
{
    return mDateFrom;
}

DATE SampleParams::getDateTo()
{
    return mDateTo;
}

double SampleParams::getRangeInPips()
{
    return mRangeInPips;
}

double SampleParams::getRate()
{
    return mRate;
}

double SampleParams::getRateStop()
{
    return mRateStop;
}

double SampleParams::getRateLimit()
{
    return mRateLimit;
}

/** Setters. */

void SampleParams::setAccount(const char *value)
{
    if (!value)
        mAccount = "";
    else
        mAccount = value;
}

void SampleParams::setOrderType(const char *value)
{
    if (!value)
        mOrderType = "";
    else
        mOrderType = value;
}

void SampleParams::setDateFrom(DATE value)
{
    mDateFrom = value;
}

void SampleParams::setDateTo(DATE value)
{
    mDateTo = value;
}

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
#include "SampleParams.h"

const char *SampleParams::Strings::instrumentNotSpecified = "'Instrument' is not specified (/i|-i|/instrument|--instrument)";
const char *SampleParams::Strings::timeframeNotSpecified = "'Timeframe' is not specified (/timeframe|--timeframe)";

SampleParams::SampleParams(int argc, char **argv)
{
    /* Load parameters with short keys. */
    mInstrument = getArgument(argc, argv, "i");

    /* If parameters with short keys not loaded, load with long keys. */
    if (mInstrument.empty())
        mInstrument = getArgument(argc, argv, "instrument");

    /* Load parameters with long keys. */
    mTimeframe = getArgument(argc, argv, "timeframe");
    std::string sDateFrom = getArgument(argc, argv, "datefrom");
    std::string sQuotesCount = getArgument(argc, argv, "count");

    /* Convert types. */
    double const NaN = std::numeric_limits<double>::quiet_NaN();

    struct tm tmBuf = {0};

    if (sDateFrom.empty())
        mDateFrom = NaN;
    else
    {
        strptime(sDateFrom.c_str(), "%m.%d.%Y %H:%M:%S", &tmBuf);
        CO2GDateUtils::CTimeToOleTime(&tmBuf, &mDateFrom);
    }

    mDateTo = NaN;

    if (sQuotesCount.empty())
    {
        mQuotesCount = -1;
    }
    else
    {
        mQuotesCount = atoi(sQuotesCount.c_str());
        if (mQuotesCount <= 0)
            mQuotesCount = -1;
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

const char *SampleParams::getInstrument()
{
    return mInstrument.c_str();
}

const char *SampleParams::getTimeframe()
{
    return mTimeframe.c_str();
}

DATE SampleParams::getDateFrom()
{
    return mDateFrom;
}

DATE SampleParams::getDateTo()
{
    return mDateTo;
}

int SampleParams::getQuotesCount()
{
    return mQuotesCount;
}

/** Setters. */

void SampleParams::setDateFrom(DATE value)
{
    mDateFrom = value;
}

void SampleParams::setDateTo(DATE value)
{
    mDateTo = value;
}

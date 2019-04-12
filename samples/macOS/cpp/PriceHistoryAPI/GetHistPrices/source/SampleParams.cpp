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
const char *SampleParams::Strings::outputFileNotSpecified = "'Output file' is not specified (/output|--output)";
const char *SampleParams::Strings::timezoneNotSupported = "'Timezone' is not supported (/tz|--tz)";

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
    std::string sDateTo = getArgument(argc, argv, "dateto");
    std::string sQuotesCount = getArgument(argc, argv, "count");
    mOutputFile = getArgument(argc, argv, "output");
    std::string timezone = getArgument(argc, argv, "tz");
    if (!timezone.empty())
    {
        std::transform(timezone.begin(), timezone.end(), timezone.begin(), ::toupper);
        mTimezone = timezone;
    }

    /* Convert types. */
    double const NaN = std::numeric_limits<double>::quiet_NaN();

    struct tm tmBuf = {0};

    if (sDateFrom.empty())
        mDateFrom = NaN;
    else
    {
        strptime(sDateFrom.c_str(), "%m.%d.%Y %H:%M:%S", &tmBuf);
        CO2GDateUtils::CTimeToOleTime(&tmBuf, &mDateFrom);
        
        if (mTimezone == "EST")
            mDateFrom = hptools::date::DateConvertTz(mDateFrom, hptools::date::EST, hptools::date::UTC);
    }

    if (sDateTo.empty())
        mDateTo = NaN;
    else
    {
        strptime(sDateTo.c_str(), "%m.%d.%Y %H:%M:%S", &tmBuf);
        CO2GDateUtils::CTimeToOleTime(&tmBuf, &mDateTo);

        if (mTimezone == "EST")
            mDateTo = hptools::date::DateConvertTz(mDateTo, hptools::date::EST, hptools::date::UTC);
    }

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

const char* SampleParams::getOutputFile()
{
    return mOutputFile.c_str();
}

const char* SampleParams::getTimezone()
{
    return mTimezone.c_str();
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

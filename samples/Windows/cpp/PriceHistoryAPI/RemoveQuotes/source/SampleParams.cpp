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
const char *SampleParams::Strings::yearNotSpecified = "'Year' is not specified (/year|--year)";

SampleParams::SampleParams(int argc, char **argv)
{
    /* Load parameters with short keys. */
    mInstrument = getArgument(argc, argv, "i");
    std::string sYear = getArgument(argc, argv, "y");

    /* If parameters with short keys not loaded, load with long keys. */
    if (mInstrument.empty())
        mInstrument = getArgument(argc, argv, "instrument");

    if (sYear.empty())
        sYear = getArgument(argc, argv, "year");

    if (sYear.empty())
    {
        mYear = -1;
    }
    else
    {
        mYear = atoi(sYear.c_str());
        if (mYear <= 0)
            mYear = 0;
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

int SampleParams::getYear()
{
    return mYear;
}
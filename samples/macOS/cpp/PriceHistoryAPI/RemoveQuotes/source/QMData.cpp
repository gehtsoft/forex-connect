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
#include "QMData.h"

QMData::QMData(const char *instrument, const char *timeframe, int year, quotesmgr::int64 size)
    : mInstrument(instrument)
    , mTimeframe(timeframe)
    , mYear(year)
    , mSize(size)
{
}

const char* QMData::getInstrument() const
{
    return mInstrument.c_str();
}

const char* QMData::getTimeframe() const
{
    return mTimeframe.c_str();
}

int QMData::getYear() const
{
    return mYear;
}

quotesmgr::int64 QMData::getSize() const
{
    return mSize;
}

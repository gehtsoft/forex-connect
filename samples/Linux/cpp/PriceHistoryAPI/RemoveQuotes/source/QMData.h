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
#pragma once

#include "IQMData.h"
#include "ThreadSafeAddRefImpl.h"

/** Quotes Manager Storage data */
class QMData : public TThreadSafeAddRefImpl<IQMData>
{
 public:
    QMData(const char *instrument, const char *timeframe, int year, quotesmgr::int64 size);

    /** @name IQMData interface implementation */
    //@{
    virtual const char* getInstrument() const;
    virtual const char* getTimeframe() const;
    virtual int getYear() const;
    virtual quotesmgr::int64 getSize() const;
    //@}

 private:
    std::string mInstrument;
    std::string mTimeframe;
    int mYear;
    quotesmgr::int64 mSize;
};

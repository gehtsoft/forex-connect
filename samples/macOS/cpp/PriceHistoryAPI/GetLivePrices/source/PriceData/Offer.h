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

#include "PriceDataInterfaces.h"
#include "../ThreadSafeAddRefImpl.h"

/** Implementation of the offer interface. */
class Offer : public TThreadSafeAddRefImpl<IOffer>
{
 public:
    Offer(std::string instrument, DATE lastUpdate, double bid, double ask, int minuteVolume, int digits);

    void setLastUpdate(DATE lastUpdate);
    void setBid(double bid);
    void setAsk(double ask);
    void setMinuteVolume(int minuteVolume);

 public:
    /** @name IOffer interface implementation */
    //@{
    virtual std::string getInstrument();
    virtual DATE getLastUpdate();
    virtual double getBid();
    virtual double getAsk();
    virtual int getMinuteVolume();
    virtual int getDigits();
    virtual IOffer *clone();
    //@}

 protected:
    virtual ~Offer();

 private:
    std::string mInstrument;
    DATE mLastUpdate;
    double mBid;
    double mAsk;
    int mMinuteVolume;
    int mDigits;
};

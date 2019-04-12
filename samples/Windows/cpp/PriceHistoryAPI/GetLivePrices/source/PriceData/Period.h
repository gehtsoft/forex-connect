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

/** Implementation of bar interface. */
class Bar : public TThreadSafeAddRefImpl<IBar>
{
 public:
    Bar(double open, double high, double low, double close);
    Bar(double open);

    void setOpen(double open);
    void setHigh(double high);
    void setLow(double low);
    void setClose(double close);

 public:
    /** @name IBar interface implementation */
    //@{
    virtual double getOpen();
    virtual double getHigh();
    virtual double getLow();
    virtual double getClose();
    //@}

 protected:
    virtual ~Bar();
 
 private:
    double mOpen;
    double mHigh;
    double mLow;
    double mClose;
};

/** Implementation of the period interface. */
class Period : public TThreadSafeAddRefImpl<IPeriod>
{
 public:
    Period(DATE time, double bid, double ask, int volume);
    Period(DATE time, double bidOpen, double bidHigh, double bidLow, double bidClose,
           double askOpen, double askHigh, double askLow, double askClose, int volume);

 public:
    /** @name IPeriod interface implementation */
    //@{
    void setVolume(int volume);
    virtual DATE getTime();
    virtual IBar *getAsk();
    virtual IBar *getBid();
    virtual int getVolume();
    //@}

 protected:
    virtual ~Period();

 private:
    DATE mTime;
    O2G2Ptr<Bar> mBid;
    O2G2Ptr<Bar> mAsk;
    int mVolume;
};

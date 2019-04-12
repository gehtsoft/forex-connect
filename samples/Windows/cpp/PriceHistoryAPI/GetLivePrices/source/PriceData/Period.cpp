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
#include "../stdafx.h"
#include "Period.h"

Bar::Bar(double open, double high, double low, double close)
{
    mOpen = open;
    mHigh = high;
    mLow = low;
    mClose = close;
}

Bar::Bar(double open)
{
    mOpen = mHigh = mLow = mClose = open;
}

Bar::~Bar()
{
}

void Bar::setOpen(double open)
{
    mOpen = open;
}

void Bar::setHigh(double high)
{
    mHigh = high;
}

void Bar::setLow(double low)
{
    mLow = low;
}

void Bar::setClose(double close)
{
    mClose = close;
}

double Bar::getOpen()
{
    return mOpen;
}

double Bar::getHigh()
{
    return mHigh;
}

double Bar::getLow()
{
    return mLow;
}

double Bar::getClose()
{
    return mClose;
}

///////////////////////////////////////////////////////////////////////////////

Period::Period(DATE time, double bid, double ask, int volume) :
    mTime(time),
    mBid(new Bar(bid)),
    mAsk(new Bar(ask)),
    mVolume(volume)
{
}

Period::Period(DATE time, double bidOpen, double bidHigh, double bidLow, double bidClose,
               double askOpen, double askHigh, double askLow, double askClose,
               int volume) :
    mTime(time),
    mBid(new Bar(bidOpen, bidHigh, bidLow, bidClose)),
    mAsk(new Bar(askOpen, askHigh, askLow, askClose)),
    mVolume(volume)
{
}

Period:: ~Period()
{
}

void Period::setVolume(int volume)
{
    mVolume = volume;
}

DATE Period::getTime()
{
    return mTime;
}

IBar *Period::getAsk()
{
    mAsk->addRef();
    return mAsk;
}

IBar *Period::getBid()
{
    mBid->addRef();
    return mBid;
}

int Period::getVolume()
{
    return mVolume;
}


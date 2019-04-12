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
#include "Offer.h"

Offer::Offer(std::string instrument, DATE lastUpdate, double bid, double ask, int minuteVolume, int digits) :
    mInstrument(instrument),
    mLastUpdate(lastUpdate),
    mBid(bid),
    mAsk(ask),
    mMinuteVolume(minuteVolume),
    mDigits(digits)
{
}

Offer::~Offer()
{
}

void Offer::setLastUpdate(DATE lastUpdate)
{
    mLastUpdate = lastUpdate;
}

void Offer::setBid(double bid)
{
    mBid = bid;
}

void Offer::setAsk(double ask)
{
    mAsk = ask;
}

void Offer::setMinuteVolume(int minuteVolume)
{
    mMinuteVolume = minuteVolume;
}

std::string Offer::getInstrument()
{
    return mInstrument;
}

DATE Offer::getLastUpdate()
{
    return mLastUpdate;
}

double Offer::getBid()
{
    return mBid;
}

double Offer::getAsk()
{
    return mAsk;
}

int Offer::getMinuteVolume()
{
    return mMinuteVolume;
}

int Offer::getDigits()
{
    return mDigits;
}

IOffer *Offer::clone()
{
    return new Offer(mInstrument, mLastUpdate, mBid, mAsk, mMinuteVolume, mDigits);
}

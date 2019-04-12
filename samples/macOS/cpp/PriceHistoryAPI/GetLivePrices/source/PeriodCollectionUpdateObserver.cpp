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
#include "PeriodCollectionUpdateObserver.h"

PeriodCollectionUpdateObserver::PeriodCollectionUpdateObserver(IPeriodCollection *collection)
    : mCollection(collection)
{
    if (NULL == collection)
        return;

    collection->addRef();
    mCollection->addListener(this);
}

PeriodCollectionUpdateObserver::~PeriodCollectionUpdateObserver()
{
    unsubscribe();
}

void PeriodCollectionUpdateObserver::onCollectionUpdate(IPeriodCollection *collection, int index)
{
    O2G2Ptr<IPeriod> period(collection->getPeriod(index));
    O2G2Ptr<IBar> bid(period->getBid());
    O2G2Ptr<IBar> ask(period->getAsk());

    char sDate[20];
    formatDate(period->getTime(), sDate);

    std::cout << "Price updated:";
    std::cout << std::setprecision(6) 
        << " DateTime=" << sDate 
        << ", BidOpen=" << bid->getOpen()
        << ", BidHigh=" << bid->getHigh()
        << ", BidLow=" << bid->getLow()
        << ", BidClose=" << bid->getClose()
        << ", AskOpen=" << ask->getOpen()
        << ", AskHigh=" << ask->getHigh()
        << ", AskLow=" << ask->getLow()
        << ", AskClose=" << ask->getClose()
        << ", Volume=" << period->getVolume()
        << std::endl;
}

void PeriodCollectionUpdateObserver::unsubscribe()
{
    mCollection->removeListener(this);
}

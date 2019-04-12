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

#include "PriceData/PriceDataInterfaces.h"

/** The observer for live prices. It listens for periods collection updates and prints them. */
class PeriodCollectionUpdateObserver : public ICollectionUpdateListener
{
 public:
    PeriodCollectionUpdateObserver(IPeriodCollection *collection);
    ~PeriodCollectionUpdateObserver();

    void unsubscribe();

 protected:
    /** @name ICollectionUpdateListener interface implementation */
    //@{
    void onCollectionUpdate(IPeriodCollection *collection, int index);
    //@}

 private:
    O2G2Ptr<IPeriodCollection> mCollection;
};


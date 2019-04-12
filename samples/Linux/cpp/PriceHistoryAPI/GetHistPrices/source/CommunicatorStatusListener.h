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
#include "ThreadSafeAddRefImpl.h"

/** The Price History API communicator request status listener. */
class CommunicatorStatusListener :
    public TThreadSafeAddRefImpl<pricehistorymgr::IPriceHistoryCommunicatorStatusListener>
{
 public:
    CommunicatorStatusListener();

    /** Returns true if the communicator is ready. */
    bool isReady();

    /** Reset error information. */
    void reset();

    /** Wait for the communicator's readiness or an error. */
    bool waitEvents();

 protected:
    /** @name IPriceHistoryCommunicatorStatusListener interface implementation */
    //@{
    virtual void onCommunicatorStatusChanged(bool ready);
    virtual void onCommunicatorInitFailed(pricehistorymgr::IError *error);
    //@}

 protected:
    virtual ~CommunicatorStatusListener();

 private:
     bool mReady;
     bool mError;

     HANDLE mSyncCommunicatorEvent;
};


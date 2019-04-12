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

/** The Price History API communicator request result listener. */
class ResponseListener
    : public TThreadSafeAddRefImpl<pricehistorymgr::IPriceHistoryCommunicatorListener>
{
 public:
    ResponseListener();

    /** Wait for request execution or an error. */
    bool wait();

    /** Get response.*/
    pricehistorymgr::IPriceHistoryCommunicatorResponse* getResponse();

    /** Set the request before waiting for execution response. */
    void setRequest(pricehistorymgr::IPriceHistoryCommunicatorRequest *request);

 public:
    /** @name IPriceHistoryCommunicatorListener interface implementation */
    //@{
    virtual void onRequestCompleted(pricehistorymgr::IPriceHistoryCommunicatorRequest *request,
                                    pricehistorymgr::IPriceHistoryCommunicatorResponse *response);
    virtual void onRequestFailed(pricehistorymgr::IPriceHistoryCommunicatorRequest *request, pricehistorymgr::IError *error);
    virtual void onRequestCancelled(pricehistorymgr::IPriceHistoryCommunicatorRequest *request);
    //@}

 protected:
    virtual ~ResponseListener();

 private:
    O2G2Ptr<pricehistorymgr::IPriceHistoryCommunicatorRequest> mRequest;
    O2G2Ptr<pricehistorymgr::IPriceHistoryCommunicatorResponse> mResponse;

    HANDLE mSyncResponseEvent;
};


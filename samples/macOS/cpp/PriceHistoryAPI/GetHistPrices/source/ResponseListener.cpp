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
#include "ResponseListener.h"

ResponseListener::ResponseListener()
{
    mSyncResponseEvent = CreateEvent(0, FALSE, FALSE, 0);

    mResponse = NULL;
    mRequest = NULL;
}

ResponseListener::~ResponseListener()
{
    CloseHandle(mSyncResponseEvent);
}

bool ResponseListener::wait()
{
    return WaitForSingleObject(mSyncResponseEvent, INFINITE) == WAIT_OBJECT_0;
}

/** Gets response.*/
pricehistorymgr::IPriceHistoryCommunicatorResponse* ResponseListener::getResponse()
{
    if (mResponse)
        mResponse->addRef();
    return mResponse;
}

void ResponseListener::setRequest(pricehistorymgr::IPriceHistoryCommunicatorRequest *request)
{
    mResponse = NULL;
    mRequest = request;
    request->addRef();
}

void ResponseListener::onRequestCompleted(pricehistorymgr::IPriceHistoryCommunicatorRequest *request,
                                          pricehistorymgr::IPriceHistoryCommunicatorResponse *response)
{
    if (mRequest == request)
    {
        mResponse = response;
        mResponse->addRef();
        SetEvent(mSyncResponseEvent);
    }
}

void ResponseListener::onRequestFailed(pricehistorymgr::IPriceHistoryCommunicatorRequest *request,
                                       pricehistorymgr::IError *error)
{
    if (mRequest == request)
    {
        std::cout << "Request failed: " << error->getMessage() << std::endl;

        mRequest = NULL;
        mResponse = NULL;

        SetEvent(mSyncResponseEvent);
    }
}

void ResponseListener::onRequestCancelled(pricehistorymgr::IPriceHistoryCommunicatorRequest *request)
{
    if (mRequest == request)
    {
        std::cout << "Request cancelled." << std::endl;

        mRequest = NULL;
        mResponse = NULL;

        SetEvent(mSyncResponseEvent);
    }
}


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
#include "CommunicatorStatusListener.h"

CommunicatorStatusListener::CommunicatorStatusListener()
    : mReady(false), mError(false)
{
    mSyncCommunicatorEvent = CreateEvent(0, FALSE, FALSE, 0);
}

CommunicatorStatusListener::~CommunicatorStatusListener()
{
    CloseHandle(mSyncCommunicatorEvent);
}

bool CommunicatorStatusListener::isReady()
{
    return mReady;
}

void CommunicatorStatusListener::reset()
{
    mReady = false;
    mError = false;
}

bool CommunicatorStatusListener::waitEvents()
{
    int res = WaitForSingleObject(mSyncCommunicatorEvent, _TIMEOUT);
    if (res != 0)
        std::cout << "Timeout occurred during waiting for communicator status is ready" << std::endl;
    return res == 0;
}

void CommunicatorStatusListener::onCommunicatorStatusChanged(bool ready)
{
    mReady = ready;
    SetEvent(mSyncCommunicatorEvent);
}

void CommunicatorStatusListener::onCommunicatorInitFailed(pricehistorymgr::IError *error)
{
    mError = true;
    std::cout << "Communicator initialization error: " << error->getMessage() << std::endl;
    SetEvent(mSyncCommunicatorEvent);
}

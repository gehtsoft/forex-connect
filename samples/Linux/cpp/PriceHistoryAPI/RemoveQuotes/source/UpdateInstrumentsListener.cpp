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
#include "UpdateInstrumentsListener.h"

UpdateInstrumentsListener::UpdateInstrumentsListener()
    : mDoneEvent(NULL)
{
    mDoneEvent = ::CreateEvent(NULL, TRUE, FALSE, NULL);
}

UpdateInstrumentsListener::~UpdateInstrumentsListener()
{
    ::CloseHandle(mDoneEvent);
}

void UpdateInstrumentsListener::onTaskFailed(quotesmgr::IUpdateInstrumentsTask *task, quotesmgr::IError *error)
{
    std::cout << "Error occurred : " << error->getMessage() << std::endl;
    ::SetEvent(mDoneEvent);
}

void UpdateInstrumentsListener::onTaskCompleted(quotesmgr::IUpdateInstrumentsTask *task)
{
    std::cout << "Update instruments task was completed." << std::endl;
    ::SetEvent(mDoneEvent);
}

void UpdateInstrumentsListener::onTaskCanceled(quotesmgr::IUpdateInstrumentsTask *task)
{
    std::cout << "Update instruments task was cancelled." << std::endl;        
    ::SetEvent(mDoneEvent);
}

void UpdateInstrumentsListener::waitEvents()
{
    ::WaitForSingleObject(mDoneEvent, _TIMEOUT);
}

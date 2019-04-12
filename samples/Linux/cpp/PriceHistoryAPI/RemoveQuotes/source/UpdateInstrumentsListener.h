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

/** The listener for QuotesManager instruments update task.
 */
class UpdateInstrumentsListener : public TThreadSafeAddRefImpl<quotesmgr::IUpdateInstrumentsCallback>
{
 public:
    UpdateInstrumentsListener();
    ~UpdateInstrumentsListener();

    /** Waits for a QuotesManaget event. */
    void waitEvents();

 protected:
    /** @name IUpdateInstrumentsCallback interface implementation */
    //@{
    virtual void onTaskFailed(quotesmgr::IUpdateInstrumentsTask *task, quotesmgr::IError *error);
    virtual void onTaskCompleted(quotesmgr::IUpdateInstrumentsTask *task);
    virtual void onTaskCanceled(quotesmgr::IUpdateInstrumentsTask *task);
    //@}

 private:
    HANDLE mDoneEvent;
};

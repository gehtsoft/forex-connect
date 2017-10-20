/* Copyright 2011 Forex Capital Markets LLC

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

#ifdef WIN32

#include <process.h>
#include <memory>

#include "threading/AThread.h"
#include "threading/ThreadHandle.h"
#include "mutex/Mutex.h"


using sample_tools::Mutex;
using sample_tools::AThread;
using sample_tools::ThreadHandle;

AThread::AThread()
    : mIsStopRequested(false),
    mHandle(0),
    mAccessMutex(),
    mThread(NULL)
{
    mDefaultPriority = GetThreadPriority(GetCurrentThread());
}

AThread::~AThread()
{
    Mutex::Lock lock(mAccessMutex);
    if (mThread)
    {
        // if the thread is still running and it's the not current thread
        // then suspend it. Note that normally it's expected that the thread
        // is already stopped here
        ThreadHandle *handler = ThreadHandle::getCurrentThread();
        if (!mHandle.equals(handler))
            ::SuspendThread(mThread);
        handler ? handler->release() : false;

        ::CloseHandle(mThread);
        mThread = NULL;
        mHandle.setHandle(0);
    }
}

ThreadHandle const *AThread::getHandle() const
{
    return &mHandle;
}

bool AThread::start()
{
    Mutex::Lock lock(mAccessMutex);

    if (isRunning())
        return true;

    if (mThread)
    {
        ::CloseHandle(mThread);
        mThread = 0;
    }

    mIsStopRequested = false;

    unsigned int threadId = 0;
    mThread = (HANDLE)_beginthreadex(NULL, 0, threadRunner, reinterpret_cast<void*>(this), 0, &threadId);
    mHandle.setHandle(threadId);

    // http://msdn.microsoft.com/en-us/library/kdzttdcb(v=vs.80).aspx
    // 0 on an error, in which case errno and _doserrno are set
    // or functions set errno to EINVAL and return -1
    if (mThread == (void *)-1L || mThread == 0)
    {
        mThread = NULL;
        mHandle.setHandle(0);
        return false;
    }

    return true;
}

bool AThread::join(unsigned long waitMilliseconds)
{
    HANDLE localCopy = NULL;
    {
        Mutex::Lock lock(mAccessMutex);

        if (!isRunning())
            return true;

        DWORD dwExitCode = 0;
        if (!GetExitCodeThread(mThread, &dwExitCode))
            return true;

        if (dwExitCode != STILL_ACTIVE) 
            return true; // thread is already terminated, so nothing to join.

        if (mHandle.getHandle() == ::GetCurrentThreadId())
            return true;

        localCopy = mThread;
    }

    switch (::WaitForSingleObject(localCopy, waitMilliseconds))
    {
        case WAIT_OBJECT_0:
        {
            Mutex::Lock lock(mAccessMutex);

            if (mThread)
            {
                ::CloseHandle(mThread);
                mThread = NULL;
                mHandle.setHandle(0);
            }
            return true;
        }

        default:
            return false;       
    }
}

void AThread::requestStop()
{
    mIsStopRequested = true;
}

bool AThread::isStopRequested() const 
{
    return mIsStopRequested;
}

bool AThread::isRunning() const
{
    Mutex::Lock lock(mAccessMutex);

    if (!mThread)
        return false;

    return ::WaitForSingleObject(mThread, 0) == WAIT_TIMEOUT;
}

unsigned int AThread::threadRunner(void *param)
{
    if (!param)
        return 1;

    AThread *obj = reinterpret_cast<AThread*>(param);
    obj->run();

    return 0;
}

AThread::PriorityLevel AThread::getPriority() const
{
    int priority = GetThreadPriority(mThread);

    if (priority != THREAD_PRIORITY_ERROR_RETURN)
    {
        if (priority == mDefaultPriority)
            return PriorityDefault;
        else if (priority == THREAD_PRIORITY_BELOW_NORMAL)
            return PriorityLow;
        else if (priority == THREAD_PRIORITY_ABOVE_NORMAL)
            return PriorityHigh;
        else if (priority == THREAD_PRIORITY_NORMAL)
            return PriorityNormal;
        else
            return PriorityUnknown;
    }

    return PriorityError;
}

bool AThread::setPriority(PriorityLevel ePrior)
{
    BOOL bResult = FALSE;

    switch (ePrior)
    {
    case PriorityDefault:
        bResult = SetThreadPriority(mThread, mDefaultPriority);
        break;
    case PriorityLow:
        bResult = SetThreadPriority(mThread, THREAD_PRIORITY_BELOW_NORMAL);
        break;
    case PriorityNormal:
        bResult = SetThreadPriority(mThread, THREAD_PRIORITY_NORMAL);
        break;
    case PriorityHigh:
        bResult = SetThreadPriority(mThread, THREAD_PRIORITY_ABOVE_NORMAL);
        break;
    default:
        bResult = FALSE;
    }

    return bResult == TRUE;
}



#endif // WIN32

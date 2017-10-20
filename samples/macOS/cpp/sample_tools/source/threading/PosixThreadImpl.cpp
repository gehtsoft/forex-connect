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

#ifdef PTHREADS

#include <signal.h>
#include <string.h>
#include <sched.h>
#include <new>
#include <assert.h>

#if defined(_POSIX_TIMERS) && _POSIX_TIMERS > 0
#   include<time.h>
#else
#   include<sys/time.h>
#endif
#include "threading/AThread.h"
#include "threading/ThreadHandle.h"
#include "mutex/Mutex.h"

using sample_tools::AThread;
using sample_tools::ThreadHandle;

namespace
{

struct ThreadRunnerArg
{
    AThread *mObj;
    PosixCondVar *mCondVar;
};

}

AThread::AThread()
    : mIsStopRequested(false),
    mHandle(0),
    mAccessMutex(),
    mIsCreated(false),
    mDefaultPriority(-1)
{
    mCondVar = new PosixCondVar();

    int policy = 0;
    struct sched_param param = {0};
    if (pthread_getschedparam(pthread_self(), &policy, &param) == 0)
        mDefaultPriority = param.sched_priority;
}

AThread::~AThread()
{
    Mutex::Lock lock(mAccessMutex);

    if (mIsCreated)
    {
        threadCleanup(this);

    #ifdef ANDROID
        pthread_kill(mHandle.getHandle(), SIGUSR1);
    #else
        pthread_cancel(mHandle.getHandle());
    #endif

        mHandle.setHandle(0);
    }

    if (mCondVar)
    {
        delete mCondVar;
        mCondVar = NULL;
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

    mCondVar->mIsSignaled = false;

    // the created thread is Detached i.e. cannot be joined - we implement
    // join by ourselves
    pthread_attr_t attr;
    pthread_attr_init(&attr);
    pthread_attr_setdetachstate(&attr, PTHREAD_CREATE_DETACHED);

    PosixCondVar *condVar = new PosixCondVar();
    ThreadRunnerArg arg = {this, condVar};

    pthread_t thread;
    int result = pthread_create(&thread, &attr, &threadRunner, (void*) &arg);

    if (result == 0)
    {
        pthread_mutex_lock(&condVar->getMutex());
        while (!condVar->mIsSignaled)
            pthread_cond_wait(&condVar->getCondVar(), &condVar->getMutex());
        pthread_mutex_unlock(&condVar->getMutex());
    }

    delete condVar;
    pthread_attr_destroy(&attr);

    if (result)
        return false;
    else
    {
        mHandle.setHandle(thread);
        mIsCreated = true;
    }

    return true;
}

bool AThread::join(unsigned long waitMilliseconds)
{
    PosixCondVar *localCondVar = 0;
    {
        Mutex::Lock lock(mAccessMutex);

        localCondVar = mCondVar;
        if (!localCondVar)
            return false;

        pthread_mutex_lock(&localCondVar->getMutex());

        if (!isRunning() || mHandle.isCurrentThread())
        {
            pthread_mutex_unlock(&localCondVar->getMutex());
            return true;
        }
    }

    int result = 0;
    if (waitMilliseconds == INFINITE)
    {
        while (!localCondVar->mIsSignaled)
            result = pthread_cond_wait(&localCondVar->getCondVar(), &localCondVar->getMutex());
    }
    else
    {
        struct timespec time = {0};
        #if defined(_POSIX_TIMERS) && _POSIX_TIMERS > 0
            clock_gettime(CLOCK_REALTIME, &time);
        #else
            struct timeval tv;
            gettimeofday(&tv, NULL);
            time.tv_sec = tv.tv_sec;
            time.tv_nsec = tv.tv_usec * 1000;
        #endif

        unsigned long sec = time.tv_sec + waitMilliseconds / 1000;
        unsigned long nsec = time.tv_nsec + (waitMilliseconds % 1000) * 1000000;
        time.tv_sec = sec + nsec / 1000000000;
        time.tv_nsec = nsec % 1000000000;

        while (!localCondVar->mIsSignaled)
            if ((result = pthread_cond_timedwait(&localCondVar->getCondVar(), &localCondVar->getMutex(), &time)) == ETIMEDOUT)
                break;
    }

    pthread_mutex_unlock(&localCondVar->getMutex());

    return result == 0;
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

    if (mIsCreated)
        return pthread_kill(mHandle.getHandle(), 0) == 0;

    return false;
}

void *AThread::threadRunner(void *param)
{
    if (!param)
    {
        assert(0);
        pthread_exit(0);
    }

    ThreadRunnerArg *arg = static_cast<ThreadRunnerArg*>(param);

    AThread *obj = arg->mObj;
    PosixCondVar *condVar = arg->mCondVar;

    pthread_mutex_lock(&condVar->getMutex());
    condVar->mIsSignaled = true;
    pthread_mutex_unlock(&condVar->getMutex());
    pthread_cond_signal(&condVar->getCondVar());

    if (obj->run() == THREAD_OBJECT_HAS_DELETED)
        pthread_exit(0);

    threadCleanup(obj);
}

void AThread::threadCleanup(void *param)
{
    if (!param)
        return;

    AThread *obj = static_cast<AThread*>(param);

    Mutex::Lock lock(obj->mAccessMutex);

    if (!obj->mIsCreated || !obj->mCondVar)
        return;

    pthread_mutex_lock(&obj->mCondVar->getMutex());
    obj->mCondVar->mIsSignaled = true;
    pthread_mutex_unlock(&obj->mCondVar->getMutex());

    pthread_cond_broadcast(&obj->mCondVar->getCondVar());
    sched_yield();

    while (pthread_mutex_trylock(&obj->mCondVar->getMutex()) ==  EBUSY)
        sched_yield();
    pthread_mutex_unlock(&obj->mCondVar->getMutex());

    obj->mIsCreated = false;
}

/**
 Get thread priority.
*/
AThread::PriorityLevel AThread::getPriority() const
{
    int policy = 0;
    struct sched_param param = {0};

    int ret_val = pthread_getschedparam(mHandle.getHandle(), &policy, &param);
    if (ret_val == 0)
    {
        if (param.sched_priority == mDefaultPriority)
            return PriorityDefault;
        else if (param.sched_priority == sched_get_priority_min(policy))
            return PriorityLow;
        else if (param.sched_priority == sched_get_priority_max(policy))
            return PriorityHigh;
        else if (param.sched_priority == ((sched_get_priority_min(policy) + sched_get_priority_min(policy)) / 2))
            return PriorityNormal;
        else
            return PriorityUnknown;
    }

    return PriorityError;
}

/**
 Set thread priority.
*/
bool AThread::setPriority(PriorityLevel ePrior)
{
    int policy = 0;
    struct sched_param param = {0};
    int ret_val = 0;

    ret_val = pthread_getschedparam(mHandle.getHandle(), &policy, &param);
    if (ret_val != 0)
        return false;

    switch (ePrior)
    {
    case PriorityLow:
        param.sched_priority = sched_get_priority_min(policy);
        break;
    case PriorityNormal:
        param.sched_priority = (sched_get_priority_min(policy) + sched_get_priority_max(policy)) / 2;
        break;
    case PriorityHigh:
        param.sched_priority = sched_get_priority_max(policy);
        break;
    case PriorityDefault:
        param.sched_priority = mDefaultPriority;
        break;
    default:
        return false;
    }

    ret_val = pthread_setschedparam(mHandle.getHandle(), policy, &param);

    return ret_val == 0;
}

#endif // PTHREADS

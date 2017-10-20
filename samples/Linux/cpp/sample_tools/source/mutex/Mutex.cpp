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
#include "mutex/Mutex.h"

using namespace sample_tools;

Mutex::Mutex()
{
#ifdef PTHREADS_MUTEX
    pthread_mutexattr_t attr;
    pthread_mutexattr_init(&attr);
    pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
    pthread_mutex_init(&m_oMutex, &attr);
    pthread_mutexattr_destroy(&attr);
#else
    ::InitializeCriticalSectionAndSpinCount(&m_oCritSection, 4000);
#endif
}

Mutex::~Mutex()
{
#ifdef PTHREADS_MUTEX
    pthread_mutex_destroy(&m_oMutex);
#else
    ::DeleteCriticalSection(&m_oCritSection);
#endif
}

void Mutex::lock() 
{
#ifdef PTHREADS_MUTEX
    pthread_mutex_lock(&m_oMutex);
#else
    ::EnterCriticalSection(&m_oCritSection);
#endif
}

void Mutex::unlock()
{
#ifdef PTHREADS_MUTEX
    pthread_mutex_unlock(&m_oMutex);
#else
    ::LeaveCriticalSection(&m_oCritSection);
#endif
}

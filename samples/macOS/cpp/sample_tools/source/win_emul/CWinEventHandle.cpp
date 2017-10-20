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
#   include <pthread.h>
#   include <errno.h>
#   include <string>
#   include <set>
#   include <sys/time.h>
#   include "threading/Interlocked.h"
#   include "CWinEventHandle.h"

namespace
{
  timespec* getTimeout(struct timespec* spec, unsigned numMs)
  {
    struct timeval currSysTime;
    gettimeofday(&currSysTime, NULL);

    unsigned long nUsNew = currSysTime.tv_usec + numMs * 1000;

    spec->tv_sec = (long)currSysTime.tv_sec + (nUsNew / 1000000);
    spec->tv_nsec = (nUsNew % 1000000) * 1000;

    return spec;
  }
}

/* METHOD *********************************************************************/
/**
  Ctor
@param manualReset: Reset mode
@param        name: Event name, may be NULL
*******************************************************************************/
CWinEventHandle::CWinEventHandle(bool manualReset, bool signaled, const wchar_t* name)
  : CBaseHandle()
  , m_ManualReset(manualReset)
  , m_Signaled(signaled)
  , m_Count(0)
  , m_RefCount(1)
  , m_Name(name == NULL ? L"" : name)
{
  pthread_mutexattr_t attr;
  pthread_mutexattr_init(&attr);
  pthread_mutex_init(&m_Mutex, &attr);
  pthread_mutex_init(&m_SubscrMutex, &attr);
  pthread_mutexattr_destroy(&attr);
  pthread_cond_init(&m_Cond, NULL);
}

CWinEventHandle::~CWinEventHandle()
{
  pthread_cond_destroy(&m_Cond);
  pthread_mutex_destroy(&m_Mutex);
  pthread_mutex_destroy(&m_SubscrMutex);
}

void CWinEventHandle::incRefCount()
{
    InterlockedIncrement(&m_RefCount);
}

int CWinEventHandle::decRefCount()
{
    InterlockedDecrement(&m_RefCount);
    return m_RefCount;
} 

void CWinEventHandle::signal()
{
  pthread_mutex_lock(&m_Mutex);
  m_Signaled = true;
  InterlockedIncrement(&m_Count);
  pthread_cond_broadcast(&m_Cond);
  pthread_mutex_unlock(&m_Mutex);//
  // signal all subscribers (used in WaitForMultipleObjects())
  pthread_mutex_lock(&m_SubscrMutex);
  for (std::set<CWinEventHandle*>::iterator iter(m_Subscriber.begin()); iter != m_Subscriber.end(); ++iter)
  {
    (*iter)->signal();
  }
  pthread_mutex_unlock(&m_SubscrMutex);
}

bool CWinEventHandle::pulse()
{
  // Only used for shutdown. ToDo !
  signal();
  return true;
}

void CWinEventHandle::reset()
{
  pthread_mutex_lock(&m_Mutex);
  m_Signaled = false;
  pthread_mutex_unlock(&m_Mutex);
}

bool CWinEventHandle::wait()
{
# define TIMEOUT_INF ~((unsigned)0)
  return wait(TIMEOUT_INF);
}

bool CWinEventHandle::wait(unsigned numMs)
{
  int rc(0);
  struct timespec spec;
  pthread_mutex_lock(&m_Mutex);//
  const long count(m_Count);
  while (!m_Signaled && m_Count == count)
  {
    if (numMs != TIMEOUT_INF)
    {
      rc = pthread_cond_timedwait(&m_Cond, &m_Mutex, getTimeout(&spec, numMs));
    }
    else
    {
      pthread_cond_wait(&m_Cond, &m_Mutex);
    }
    if (rc == ETIMEDOUT)
    {
      break;
    }
  }
  if (!m_ManualReset)
  {
    // autoReset
    m_Signaled = false;
  }
  pthread_mutex_unlock(&m_Mutex);//
  return rc != ETIMEDOUT;
}

/* METHOD *********************************************************************/
/**
  Stores subscriber event to signal instead of signalling "this".
*******************************************************************************/
void CWinEventHandle::subscribe(CWinEventHandle* subscriber)
{
  pthread_mutex_lock(&m_SubscrMutex);
  m_Subscriber.insert(subscriber);
  pthread_mutex_unlock(&m_SubscrMutex);
}

/* METHOD *********************************************************************/
/**
  Removes a subscriber event from the subscriber set (external critical section).
*******************************************************************************/
void CWinEventHandle::unSubscribe(CWinEventHandle* subscriber)
{
  pthread_mutex_lock(&m_SubscrMutex);

  std::set<CWinEventHandle*>::iterator element = m_Subscriber.find(subscriber);
  if (element != m_Subscriber.end())
      m_Subscriber.erase(element);

  pthread_mutex_unlock(&m_SubscrMutex);
}

/* METHOD *********************************************************************/
/**
  Performs auto reset.
*******************************************************************************/
void CWinEventHandle::resetIfAuto()
{
  if (!m_ManualReset)
  {
      reset();
  }
}

#endif

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
#include <cstdio>
#include "hidden_class.h"
#ifdef PTHREADS
#   include <set>
#   include <string>
#   include <sys/timeb.h>
#   include "win_emul/winEmul.h"
#   include "winevent.h"
#   include "CWinEventHandle.h"
#   include "mutex/Mutex.h"

using std::wstring;

sample_tools::Mutex mAccessMutex;

/* CLASS DECLARATION **********************************************************/
/**
  Critical section helper class, encapsulates a (recursive) pthread_mutex_t.
*******************************************************************************/
static class HIDDEN_CLASS CCriticalSection
{
  pthread_mutex_t m_Mutex;
public:
  CCriticalSection(bool recursive = true)
  {
    if (recursive)
    {
      pthread_mutexattr_t attr;
      pthread_mutexattr_init(&attr);
      pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
      pthread_mutex_init(&m_Mutex, &attr);
    }
    else
    {
      pthread_mutex_init(&m_Mutex, 0);
    }
  }
  ~CCriticalSection()
  {
    pthread_mutex_destroy(&m_Mutex);
  }
  inline void enter() { pthread_mutex_lock(&m_Mutex); }
  inline void leave() { pthread_mutex_unlock(&m_Mutex); }
} s_CritSect;

class HIDDEN_CLASS CEventLock
{
public:
  CEventLock() { s_CritSect.enter(); }
  ~CEventLock() { s_CritSect.leave(); }
};

/* CLASS REALIZATION **********************************************************/
/**
  Administrates a std::set of CWinEventHandle objects.
  Must be possible to find a CWinEventHandle by name.
  (std::map not very helpful: There are CWinEventHandles without a name, and
  name is stored in CWinEventHandle).
*******************************************************************************/
TWinEventHandleSet *CWinEventHandleSet::s_Set = NULL;

void CWinEventHandleSet::init()
{
    if (s_Set == NULL)
        s_Set = new TWinEventHandleSet();
}
void CWinEventHandleSet::cleanup()
{
  sample_tools::Mutex::Lock l(mAccessMutex);

  if (s_Set != NULL)
  {
      delete s_Set;
      s_Set = NULL;
  }
}

CBaseHandle* CWinEventHandleSet::createEvent(bool manualReset, bool signaled, const wchar_t* name)
{
    CWinEventHandle* handle(new CWinEventHandle(manualReset, signaled, name));
    CEventLock lock;//
    init();

    {
        sample_tools::Mutex::Lock l(mAccessMutex);
        if (s_Set)
            s_Set->insert(handle);
    }

    return handle;
}
void CWinEventHandleSet::closeHandle(CWinEventHandle* eventHandle)
{
    CEventLock lock;//
    if (eventHandle->decRefCount() == 0)
    {
      init();
      // ToDo: Inform/wakeup waiting threads ? !

      {
          sample_tools::Mutex::Lock l(mAccessMutex);
          if (s_Set)
              s_Set->erase(eventHandle);
      }

      delete eventHandle;
    }
}
HANDLE CWinEventHandleSet::openEvent(const wchar_t* name)
{
    CEventLock lock;//

    sample_tools::Mutex::Lock l(mAccessMutex);
    if (s_Set)
    {
        for (TWinEventHandleSet::iterator iter(s_Set->begin()); iter != s_Set->end(); ++iter)
        {
          if ((*iter)->name() == name)
          {
            (*iter)->incRefCount();
            return *iter;
          }
        }
    }

    return NULL;
}

/* METHOD *********************************************************************/
/**
 Called from CloseHandle(HANDLE) when the HANDLE refers to an event.
 Decrements the reference count. If the becomes zero: Removes the handle from
 CWinEventHandleSet, deletes the event.
*
@return true
*******************************************************************************/
bool CWinEventHandle::close()
{
  CWinEventHandleSet::closeHandle(this);
  // Note: "this" may be deleted now!
  return true;
}

inline CWinEventHandle* castToWinEventHandle(HANDLE hEvent)
{
    return (CWinEventHandle *)(hEvent);
    // the cast below causes unexpected crashes due to 0 returned in several environments (android build, and some mac's (with gcc 4.1.2 and -O2))
    //return dynamic_cast<CWinEventHandle*>( reinterpret_cast<CBaseHandle*>(hEvent) );
}

/* FUNCTION *******************************************************************/
/**
  See MSDN. Requires realtime library, -lrt.
*******************************************************************************/
DWORD WINAPI GetTickCount()
{
    struct timeb currSysTime;
    ftime(&currSysTime);
    return long(currSysTime.time) * 1000 + currSysTime.millitm;
}
/* FUNCTION *******************************************************************/
/**
  See MSDN.
*******************************************************************************/
void WINAPI Sleep(DWORD dwMilliseconds)
{
    usleep(1000 * dwMilliseconds);
}
/* FUNCTION *******************************************************************/
/**
  See MSDN
*******************************************************************************/
HANDLE WINAPI CreateEventW(LPSECURITY_ATTRIBUTES, BOOL bManualReset, BOOL bInitialState, LPCWSTR lpName)
{
  return CWinEventHandleSet::createEvent(bManualReset != FALSE , bInitialState != FALSE, lpName);
}

/* FUNCTION *******************************************************************/
/**
*******************************************************************************/
HANDLE WINAPI OpenEventW(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCWSTR lpName)
{
  return CWinEventHandleSet::openEvent(lpName);
}

/* FUNCTION *******************************************************************/
/**

*******************************************************************************/
BOOL WINAPI CloseHandle(HANDLE handle)
{
  bool ret(false);
  if (handle != NULL)
  {
    CBaseHandle* baseHandle(static_cast<CBaseHandle*>(handle));
    if (!baseHandle->close())
    {
      printf("Closing unknown HANDLE type\n");
    }
    ret = true;
  }
  return ret;
}

/* FUNCTION *******************************************************************/
/**
*******************************************************************************/
BOOL WINAPI SetEvent(HANDLE hEvent)
{
  CEventLock lock; //The lock avoids a race condition with subscribe() in WaitForMultipleObjects()//
  castToWinEventHandle(hEvent)->signal();
  return true;
}

/* FUNCTION *******************************************************************/
/**
*******************************************************************************/
BOOL WINAPI ResetEvent(HANDLE hEvent)
{
  castToWinEventHandle(hEvent)->reset();
  return true;
}

/* FUNCTION *******************************************************************/
/**
*******************************************************************************/
BOOL WINAPI PulseEvent(HANDLE hEvent)
{
  return castToWinEventHandle(hEvent)->pulse();
}

/* FUNCTION *******************************************************************/
/**
  See MSDN
*******************************************************************************/
DWORD WINAPI WaitForSingleObject(HANDLE obj, DWORD timeMs)
{
  CBaseHandle* handle(static_cast<CBaseHandle*>(obj));
  if (handle->wait(timeMs))
  {
    return WAIT_OBJECT_0;
  }
  // Might be handle of wrong type?
  return WAIT_TIMEOUT;
}

/* FUNCTION *******************************************************************/
/**
  See MSDN.
*******************************************************************************/
DWORD WINAPI WaitForMultipleObjects(DWORD numObj, const HANDLE* objs, BOOL waitAll, DWORD timeMs)
{
  CWinEventHandle* eventHandle[MAXIMUM_WAIT_OBJECTS];
  //assert(numObj <= MAXIMUM_WAIT_OBJECTS);
  if (waitAll)
  {
    const DWORD startMs(GetTickCount());
    for (unsigned ix(0); ix < numObj; ix++)
    {
      // Wait for all events, one after the other.
      CWinEventHandle* event(castToWinEventHandle(objs[ix]));
      //assert(event);
      DWORD usedMs(GetTickCount() - startMs);
      if (usedMs > timeMs)
      {
        return WAIT_TIMEOUT;
      }
      if (!event->wait(timeMs - usedMs))
      {
        return WAIT_TIMEOUT;
      }
    }
    return WAIT_OBJECT_0;
  }
  s_CritSect.enter();//
  // Check whether any event is already signaled
  for (unsigned ix(0); ix < numObj; ix++)
  {
    CWinEventHandle* event(castToWinEventHandle(objs[ix]));
    //assert(event);
    if (event->signaled())
    {
      event->resetIfAuto(); // Added 13.09.2008 (bug detected by BRAD H)
      s_CritSect.leave();//
      return ix;
    }
    eventHandle[ix] = event;
  }
  if (timeMs == 0)
  {
    // Only check, do not wait. Has already failed, see above.
    s_CritSect.leave();//
    return WAIT_TIMEOUT;
  }
  /***************************************************************************
  Main use case: No event signaled yet, create a subscriber event.
  ***************************************************************************/
  CWinEventHandle subscriberEvent(false, 0);
  // Subscribe at the original objects
  for (unsigned ix(0); ix < numObj; ix++)
  {
    eventHandle[ix]->subscribe(&subscriberEvent);
  }
  s_CritSect.leave(); // Re-enables SetEvent(). OK since the subscription is done

  bool success(subscriberEvent.wait(timeMs));

  // Unsubscribe and determine return value
  DWORD ret(WAIT_TIMEOUT);
  s_CritSect.enter();//
  for (unsigned ix(0); ix < numObj; ix++)
  {
    if (success && eventHandle[ix]->signaled())
    {
      success = false;
      ret = ix;
      // Reset event that terminated the WaitForMultipleObjects() call
      eventHandle[ix]->resetIfAuto(); // Added 16.09.2009 (Alessandro)
    }
    eventHandle[ix]->unSubscribe(&subscriberEvent);
  }
  s_CritSect.leave();//
  return ret;
}

#endif


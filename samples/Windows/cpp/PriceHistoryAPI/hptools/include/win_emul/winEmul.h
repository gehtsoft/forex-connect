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

#ifndef WINEMUL_H
#   define WINEMUL_H
#   ifdef WIN32
#       ifdef PTHREADS
#           ifdef _M_IX86
#               define _X86_
#           endif
#           include <windef.h>
#       else
#           include <windows.h>
#       endif
#   else
#       ifndef PTHREADS
#           define PTHREADS
#       endif
#       define WINBASEAPI
#       define CONST const
#       define STATUS_WAIT_0   ((DWORD)0x00000000L)
#       define STATUS_TIMEOUT  ((DWORD)0x00000102L)
#       ifndef _WINDEF_
#           define WINAPI
#           ifndef FALSE
#               define FALSE               0
#           endif
#       endif
#       define stdext __gnu_cxx
#       define _WINDEF_
#       define WINAPI
#       ifndef FALSE
#           define FALSE               0
#       endif
#       ifndef TRUE
#           define TRUE                1
#       endif

        // Some win*.h types redefine
typedef unsigned long       ULONG_PTR, DWORD, DWORD_PTR, *LPDWORD, COLORREF, *LPCOLORREF;
typedef int                 BOOL, *LPBOOL, errno_t;
typedef unsigned char       BYTE, *LPBYTE;
typedef unsigned short      WORD, *LPWORD;
typedef int                 INT, *LPINT;
typedef long                LONG, *LPLONG, HRESULT;
typedef void                *LPVOID, *HINTERNET, *HANDLE, *HMODULE, *HINSTANCE;
typedef const void          *LPCVOID;
typedef unsigned int        UINT, *PUINT;
typedef char                *LPSTR, *LPTSTR;
typedef const char          *LPCSTR, *LPCTSTR;
typedef wchar_t             WCHAR, *PWCHAR, *LPWCH, *PWCH, *LPWSTR, *PWSTR;
typedef const WCHAR         *LPCWCH, *PCWCH, *LPCWSTR, *PCWSTR;

#       define _int8 char
#       define _int16 short
#       define _int32 int
#       define __int64 long long
#       define _int64 long long

#       define sprintf_s(A, B, args...) sprintf(A, ## args)
static const DWORD MAXIMUM_WAIT_OBJECTS(32);

#   endif

#   ifndef _WINBASE_
#       define WAIT_TIMEOUT    STATUS_TIMEOUT
#       define WAIT_OBJECT_0   ((STATUS_WAIT_0 ) + 0 )
#       define CREATE_SUSPENDED 0x00000004
#       ifndef INFINITE
#           define INFINITE        0xFFFFFFFF    // Infinite timeout
#       endif//INFINITE

typedef struct _SECURITY_ATTRIBUTES
{
    BOOL bInheritHandle;
} SECURITY_ATTRIBUTES;
typedef SECURITY_ATTRIBUTES* LPSECURITY_ATTRIBUTES;

#       ifndef CreateEvent
#           define CreateEvent  CreateEventW
#       endif
#       ifndef OpenEvent
#           define OpenEvent  OpenEventW
#       endif
#   endif

#   ifdef PTHREADS
#   include <stdio.h>
namespace hptools
{
    namespace win_emul
    {
        DWORD HP_TOOLS WaitForSingleObject(HANDLE hHandle, DWORD dwMilliseconds);
        DWORD HP_TOOLS WaitForMultipleObjects(DWORD nCount, CONST HANDLE *lpHandles, BOOL bWaitAll, DWORD dwMilliseconds);

        BOOL HP_TOOLS SetEvent(HANDLE hEvent);
        BOOL HP_TOOLS ResetEvent(HANDLE hEvent);
        BOOL HP_TOOLS PulseEvent(HANDLE hEvent);  // Used in CRsEvent.cpp

        HANDLE HP_TOOLS CreateEventW(LPSECURITY_ATTRIBUTES lpEventAttributes, BOOL bManualReset, BOOL bInitialState, LPCWSTR lpName);

        HANDLE HP_TOOLS OpenEventW(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCWSTR lpName);
        BOOL HP_TOOLS CloseHandle(HANDLE hObject);

        void HP_TOOLS Sleep(DWORD dwMilliseconds);
    }
}

#       define WaitForSingleObject(A,B) hptools::win_emul::WaitForSingleObject(A,B)
#       define WaitForMultipleObjects(A,B,C,D) hptools::win_emul::WaitForMultipleObjects(A,B,C,D)
#       define SetEvent(A) hptools::win_emul::SetEvent(A)
#       define ResetEvent(A) hptools::win_emul::ResetEvent(A)
#       define PulseEvent(A) hptools::win_emul::PulseEvent(A)
#       define CreateEventW(A,B,C,D) hptools::win_emul::CreateEventW(A,B,C,D)
#       define OpenEventW(A,B,C) hptools::win_emul::OpenEventW(A,B,C)
#       define CloseHandle(A) hptools::win_emul::CloseHandle(A)
#       define Sleep(A) hptools::win_emul::Sleep(A)
#   endif
#endif


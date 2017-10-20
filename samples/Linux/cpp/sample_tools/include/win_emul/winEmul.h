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

#       define MAX_PATH            260
#       define CALLBACK
#       define RGB(r,g,b)          ((COLORREF)(((BYTE)(r)|((WORD)((BYTE)(g))<<8))|(((DWORD)(BYTE)(b))<<16)))

typedef struct _FILETIME {
    DWORD dwLowDateTime;
    DWORD dwHighDateTime;
} FILETIME, *PFILETIME, *LPFILETIME;

#       define _int8 char
#       define _int16 short
#       define _int32 int
#       define __int64 long long
#       define _int64 long long

#       define _strdup(A) strdup(A)
#       define _wcsdup(A) wcsdup(A)
#       define memcpy_s(A,B,C,D) memcpy(A,C,D)
#       define _stricmp(A,B) strcasecmp(A,B)
#       define sprintf_s(A, B, args...) sprintf(A, ## args)
#       define fprintf_s(A, B, args...) fprintf(A, B, ## args)
#       define sscanf_s(A, B, args...) sscanf(A, B, ## args)
#       define _close(A) close(A)
#       define _read(A,B,C) read(A,B,C)
#       define _write(A,B,C) write(A,B,C)
#       define _lseek(A,B,C) lseek(A,B,C)
#       define _itoa_s(A,B,C,D) itoa(A,B,D)
#       define _vsnprintf_s(buffer,sizeOfBuffer,count,format,args) vsprintf(buffer,format,args)
#       define vfprintf_s(A,B,C) vfprintf(A,B,C)
#       define _ftime(A) ftime(A)
#       define _timeb timeb
#       define localtime_s(A,B) localtime_r(B,A)
#       define _threadid (unsigned long)0

#       define _O_BINARY 0
#       define _O_TEXT 0
#       define _O_CREAT O_CREAT
#       define _O_APPEND O_APPEND
#       define _O_EXCL O_EXCL
#       define _O_RDONLY O_RDONLY
#       define _O_WRONLY O_WRONLY
#       define _SH_DENYNO 0x666
#       define _SH_DENYRW S_IRUSR|S_IWUSR
#       define _S_IWRITE S_IWUSR //S_IWUSR|S_IWGRP|S_IWOTH
#       define _S_IREAD S_IRUSR //S_IRUSR|S_IRGRP|S_IROTH
#       define VER_SEP_DOT   .
#       define VER_STR_HELPER4(x,y,z,b,sep) #x #sep #y #sep #z #sep #b 
#       define S_OK       ((HRESULT)0x00000000L)

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
namespace sample_tools
{
    namespace win_emul
    {
        DWORD GSTOOL3 WaitForSingleObject(HANDLE hHandle, DWORD dwMilliseconds);
        DWORD GSTOOL3 WaitForMultipleObjects(DWORD nCount, CONST HANDLE *lpHandles, BOOL bWaitAll, DWORD dwMilliseconds);
        BOOL GSTOOL3 SetEvent(HANDLE hEvent);
        BOOL GSTOOL3 ResetEvent(HANDLE hEvent);
        BOOL GSTOOL3 PulseEvent(HANDLE hEvent);  // Used in CRsEvent.cpp

        HANDLE GSTOOL3 CreateEventW(LPSECURITY_ATTRIBUTES lpEventAttributes, BOOL bManualReset, BOOL bInitialState, LPCWSTR lpName);

        HANDLE GSTOOL3 OpenEventW(DWORD dwDesiredAccess, BOOL bInheritHandle, LPCWSTR lpName);
        BOOL GSTOOL3 CloseHandle(HANDLE hObject);

        DWORD GSTOOL3 GetTickCount();
        void GSTOOL3 Sleep(DWORD dwMilliseconds);

        errno_t GSTOOL3 fopen_s(FILE** pFile, const char *filename, const char *mode);
        errno_t GSTOOL3 _sopen_s(int* pfh, const char *filename, int oflag, int shflag, int pmode);
        errno_t GSTOOL3 _strlwr_s(char * str, size_t numberOfElements);
        errno_t GSTOOL3 _strupr_s(char * str, size_t numberOfElements);
        errno_t GSTOOL3 freopen_s(FILE** pFile, const char *path, const char *mode, FILE *stream);
        
        errno_t GSTOOL3 strcpy_s(char *strDest, size_t numberOfElements, const char *strSource);
        errno_t GSTOOL3 strncpy_s(char *strDest, size_t numberOfElements, const char *strSource, size_t count);
        errno_t GSTOOL3 strcat_s(char *strDest, size_t numberOfElements, const char *strSource);
    }
}

#       define WaitForSingleObject(A,B) sample_tools::win_emul::WaitForSingleObject(A,B)
#       define WaitForMultipleObjects(A,B,C,D) sample_tools::win_emul::WaitForMultipleObjects(A,B,C,D)
#       define SetEvent(A) sample_tools::win_emul::SetEvent(A)
#       define ResetEvent(A) sample_tools::win_emul::ResetEvent(A)
#       define PulseEvent(A) sample_tools::win_emul::PulseEvent(A)
#       define CreateEventW(A,B,C,D) sample_tools::win_emul::CreateEventW(A,B,C,D)
#       define OpenEventW(A,B,C) sample_tools::win_emul::OpenEventW(A,B,C)
#       define CloseHandle(A) sample_tools::win_emul::CloseHandle(A)
#       define GetTickCount() sample_tools::win_emul::GetTickCount()
#       define Sleep(A) sample_tools::win_emul::Sleep(A)
#       define fopen_s(A,B,C) sample_tools::win_emul::fopen_s(A,B,C) 
#       define _sopen_s(A,B,C,D,E) sample_tools::win_emul::_sopen_s(A,B,C,D,E)
#       define _strlwr_s(A,B) sample_tools::win_emul::_strlwr_s(A,B) 
#       define _strupr_s(A,B) sample_tools::win_emul::_strupr_s(A,B) 
#       define freopen_s(A,B,C,D) sample_tools::win_emul::freopen_s(A,B,C,D) 
#       define strcpy_s(A,B,C) sample_tools::win_emul::strcpy_s(A,B,C) 
#       define strncpy_s(A,B,C,D) sample_tools::win_emul::strncpy_s(A,B,C,D) 
#       define strcat_s(A,B,C) sample_tools::win_emul::strcat_s(A,B,C) 

#       include <stdint.h>
#       include <errno.h>
#       include <cstdio>
#       define GetLastError() errno

#       include <limits.h>
#       include <stdlib.h>
#       define GetFullPathName(A,B,C,D) realpath(A,C)

#       include <cctype>
#       include <algorithm>
#       define CharUpperBuff(A,B) { std::string __str(A,B); std::transform(__str.begin(),__str.end(),__str.begin(),::toupper); strcpy(A,__str.c_str()); }

inline BOOL SetEnvironmentVariable(LPCTSTR lpName, LPCTSTR lpValue)
{
    return setenv(lpName, lpValue, 1);
}

/* _countof helper */
#       if !defined(_countof)
#           if !defined(__cplusplus)
#               define _countof(_Array) (sizeof(_Array) / sizeof(_Array[0]))
#           else
extern "C++"
{
template <typename _CountofType, size_t _SizeOfArray>
char (*__countof_helper(_CountofType (&_Array)[_SizeOfArray]))[_SizeOfArray];
#               define _countof(_Array) sizeof(*__countof_helper(_Array))
}
#           endif
#       endif

#   endif
#endif


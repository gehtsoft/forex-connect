// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.

#ifdef WIN32
#   ifndef WINVER                    // Allow use of features specific to Windows XP or later.
#       define WINVER 0x0501         // Change this to the appropriate value to target other versions of Windows.
#   endif
#   ifndef _WIN32_WINNT              // Allow use of features specific to Windows XP or later.                   
#       define _WIN32_WINNT 0x0501   // Change this to the appropriate value to target other versions of Windows.
#   endif                        
#   ifndef _WIN32_WINDOWS            // Allow use of features specific to Windows 98 or later.
#       define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
#   endif
#   ifndef _WIN32_IE                 // Allow use of features specific to IE 6.0 or later.
#       define _WIN32_IE 0x0600      // Change this to the appropriate value to target other versions of IE.
#   endif
#   ifndef PTHREADS
#       define WIN32_LEAN_AND_MEAN     // Exclude rarely-used stuff from Windows headers
#       include <windows.h>
#   endif
#else
#   define PTHREADS
#   define PTHREADS_MUTEX
#endif

#ifdef WIN32
#   define GSTOOL3 __declspec(dllexport)
#else
    #define GSTOOL3

    #include <sys/types.h>
    #include <sys/stat.h>
    #include <unistd.h>
    #include <errno.h>
    #include <dirent.h>
#endif


// TODO: reference additional headers your program requires here

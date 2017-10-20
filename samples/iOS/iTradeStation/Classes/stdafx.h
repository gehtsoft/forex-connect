#pragma once

#define DEBUG_OUT


#include <libkern/OSAtomic.h>
#ifdef __LP64__
#define InterlockedIncrement(A) (long)OSAtomicIncrement64((volatile int64_t *)A)
#define InterlockedDecrement(A) (long)OSAtomicDecrement64((volatile int64_t *)A)
#else
#define InterlockedIncrement(A) (long)OSAtomicIncrement32((volatile int32_t *)A)
#define InterlockedDecrement(A) (long)OSAtomicDecrement32((volatile int32_t *)A)
#endif

typedef long                LONG;
typedef const char          *LPCSTR;

#define _stricmp(A,B) strcasecmp(A,B)

/*#define uni win_emul
 #include "winEmul.h"
 #ifndef CreateEvent
 #define CreateEvent CreateEventA
 #endif*/

#include <iostream>
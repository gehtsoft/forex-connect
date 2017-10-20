#pragma once

// Crossplatform interlocked functions


/** Atomic compare and exchange operation
    @param destination          A pointer to the destination value
    @param value                The value to store in the destination, if the old value is equal to comparand
    @param comparand            Value to compare with the destination
    @return                     True, if previous value of destination was equal with comparand, and exchange was made.
*/
//bool InterlockedBoolCompareExchange(volatile long *destination, long value, long comparand);

/** Atomic exchange operation
    @param destination          A pointer to the destination value
    @param value                The value to store in the destination
    @return                     True, if previous value of destination was equal with new value
*/
//bool InterlockedBoolExchange(volatile long *destination, long value);

#ifdef WIN32
#include <malloc.h>
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x500
#endif
#include <intrin.h>
#pragma intrinsic (_InterlockedExchange)

#define InterlockedBoolCompareExchange(A,B,C) (_InterlockedCompareExchange(A,B,C) == (C))
#define InterlockedBoolExchange(A,B) (_InterlockedExchange(A,B) == (B))

#else  // crossplatform

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>

#include <sched.h>
#define SwitchToThread() sched_yield()
  #if defined(__APPLE__) && defined(__MACH__)
   #ifdef __LP64__
        #define InterlockedIncrement(A) (long)OSAtomicAdd64( 1, (volatile int64_t *)A)
        #define InterlockedDecrement(A) (long)OSAtomicAdd64(-1, (volatile int64_t *)A)
   #else
        #define InterlockedIncrement(A) (long)OSAtomicAdd32( 1, (volatile int32_t *)A)
        #define InterlockedDecrement(A) (long)OSAtomicAdd32(-1, (volatile int32_t *)A)
 #endif
        #include <libkern/OSAtomic.h>
        #define InterlockedBoolCompareExchange(A,B,C) OSAtomicCompareAndSwapLong(C,B,(volatile long *)A)
        // The implementation below for Mac OS is NOT equal the Windows or Linux one!
        // It only works for boolean (0 or 1) values of B and (*A) parameters
        #define InterlockedBoolExchange(A,B) ((B)?(OSAtomicTestAndSet(7,(volatile uint32_t *)A) == (B)):(!OSAtomicTestAndClear(7,(volatile uint32_t *)A)))
  #else  // linux and similar
        #include <malloc.h>
        #define InterlockedBoolCompareExchange(A,B,C) __sync_bool_compare_and_swap(A,C,B)
        #define _InterlockedExchange(A,B) (long)__sync_lock_test_and_set(A,B)
        #define InterlockedBoolExchange(A,B) (_InterlockedExchange(A,B) == (B))
        #define InterlockedIncrement(A) (long)__sync_add_and_fetch(A, 1L)  
        #define InterlockedDecrement(A) (long)__sync_sub_and_fetch(A, 1L)  
  #endif

#endif


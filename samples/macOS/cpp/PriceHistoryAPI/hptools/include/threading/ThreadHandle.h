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

#if !defined(WIN32) && !defined(PTHREADS)
#   define PTHREADS
#endif

#ifdef PTHREADS
    #include <pthread.h>
#endif

namespace hptools
{

/**
 @class ThreadHandle
 Wrapper for platform-dependend threads identification.
*/
class HP_TOOLS ThreadHandle
{
#ifdef PTHREADS
    typedef pthread_t Handle;
#elif WIN32
    typedef unsigned long Handle;
#endif

 private:
    ThreadHandle();
    ThreadHandle(Handle handle);
    ~ThreadHandle();

 public:
    /**
     Create new instance of current thread.
     @return Return new instance of current thread.
    */
    static ThreadHandle* getCurrentThread();

    /**
     Destroy object.
    */
    void release();

    /**
     Compare thread handle with caller's thread.
     @return If current thread handle and caller's thread are the same, return true.
             If current thread handle and caller's thread are different, return false.
    */
    bool isCurrentThread() const;

 private:
    Handle mHandle;

    Handle getCurrentThreadHandle() const;
};

}

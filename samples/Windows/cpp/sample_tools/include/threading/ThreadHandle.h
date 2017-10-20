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

#if !defined(WIN32) && !defined(PTHREADS)
#   define PTHREADS
#endif

#ifdef PTHREADS
    #include <pthread.h>
#endif

namespace sample_tools
{

class AThread;

/**
 @class AThread
 Wrapper for platform-dependend threads identification.
*/
class GSTOOL3 ThreadHandle
{
#ifdef PTHREADS
    typedef pthread_t Handle;
#elif WIN32
    typedef unsigned long Handle;
#endif

    friend class AThread;

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

    /**
     Complate two thread handles.
     @param threadHandle - target thread handle.
     @return If current thread handle and target thread handle are the same, return true.
             If current thread handle and target thread handle are different, return false.
    */
    bool equals(ThreadHandle const *threadHandle) const;

    /**
     Compare current thread handle with target thread.
     @param thread - target thread.
     @return If current thread handle and the target thread handle are the same, retur true.
             If current thread handle and the target thread handle are different, return false.
    */
    bool equals(AThread const *thread) const;

 private:
    Handle getCurrentThreadHandle() const;
    Handle getHandle() const;
    void setHandle(Handle handle);

 private:
    Handle mHandle;
};

}

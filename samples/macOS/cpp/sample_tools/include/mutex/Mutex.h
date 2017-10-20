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

#if !defined(WIN32) && !defined(PTHREADS_MUTEX)
#   define PTHREADS_MUTEX
#endif

#ifdef PTHREADS_MUTEX
#   include "pthread.h"
#endif

namespace sample_tools
{
    /**
        The class implements a recursive mutex object for Windows/Linux/MacOS platform.
    */
    class GSTOOL3 Mutex
    {
    public:
        Mutex();
        ~Mutex();
        void lock();
        void unlock();

        class Lock 
        {
        public:
            Lock(Mutex& m) : mutex( &m )
            {
                mutex->lock();
            }

            Lock(Mutex* m) : mutex( m )
            {
                mutex->lock();
            }

            ~Lock()
            {
                mutex->unlock();
            }
        private:
            Mutex* mutex;
        };

    private:
    #ifdef PTHREADS_MUTEX
        pthread_mutex_t m_oMutex;
    #else
        CRITICAL_SECTION m_oCritSection;
    #endif
    };
}


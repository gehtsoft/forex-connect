/* Copyright 2013 Forex Capital Markets LLC

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

#ifdef WIN32
#   define GSTOOL3 __declspec(dllimport)
#else
#   define GSTOOL3
#   define PTHREADS
#   define PTHREADS_MUTEX
#endif

#include "win_emul/winEmul.h"
#include "date/date.h"
#include "mutex/Mutex.h"
#include "threading/Interlocked.h"
#include "threading/AThread.h"
#include "threading/ThreadHandle.h"


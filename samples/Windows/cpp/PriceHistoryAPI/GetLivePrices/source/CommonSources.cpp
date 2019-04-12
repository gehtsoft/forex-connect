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
#include "stdafx.h"

void formatDate(DATE date, char *buf)
{
    struct tm tmBuf = {0};
    CO2GDateUtils::OleTimeToCTime(date, &tmBuf);
    
    using namespace std;
    stringstream sstream;
    sstream << setw(2) << setfill('0') << tmBuf.tm_mon + 1 << "." \
            << setw(2) << setfill('0') << tmBuf.tm_mday << "." \
            << setw(4) << tmBuf.tm_year + 1900 << " " \
            << setw(2) << setfill('0') << tmBuf.tm_hour << ":" \
            << setw(2) << setfill('0') << tmBuf.tm_min << ":" \
            << setw(2) << setfill('0') << tmBuf.tm_sec;
    strcpy(buf, sstream.str().c_str());
}

bool isNaN(double value)
{
    return value != value;
}

std::string upperString(const std::string &str)
{
    std::string upper;
    std::transform(str.begin(), str.end(), std::back_inserter(upper), toupper);
    return upper;
}
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

class IQMDataCollection;

namespace util
{

typedef std::map<std::string, std::map<int, quotesmgr::int64> > SummaryInfoType;
typedef SummaryInfoType::value_type SummaryInfoPairType;
typedef SummaryInfoType::mapped_type YearSizeInfoType;

template<typename T, std::size_t size>
std::size_t getArraySize(T (&)[size])
{
    return size;
}

std::string getCachePath(const std::string &exePath);
int convertYear(double year);
bool summariseInstrumentDataSize(IQMDataCollection *collection, SummaryInfoType &instrumentsInfo);
void printInstrumentSummaryInfo(const SummaryInfoPairType &summaryInfoPair);
std::string readableSize(quotesmgr::int64 bytes);

} // namespace util

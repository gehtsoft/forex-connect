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

#include "util.h"
#include "IQMDataCollection.h"
#include "IQMData.h"

namespace util
{

/** Get the year from OLE date.

    @param date
        Date.
 */
int convertYear(double date)
{
    SYSTEMTIME systemTime = {};
    hptools::date::OleTimeToWindowsTime(date, &systemTime);
    return systemTime.wYear;
}

/** Returns path to the application's module exclude filename and extension.

    @param exePath
        Path to application module.
 */
std::string getCachePath(const std::string &exePath)
{
    const std::string cacheFolderName("History");

    const std::size_t slashPos(exePath.find_last_of("\\/"));
    if (std::string::npos == slashPos)
    {
#ifdef WIN32
        return ".\\" + cacheFolderName;
#else
        return "./" + cacheFolderName;
#endif
    }

    return exePath.substr(0, slashPos + 1) + cacheFolderName;
}

/** Get full size of available instrument's quotes.

    @param collection
        Collection of quotes manager storage data.
    @param instrumentsInfo
        [out] Instruments summary info.
    @return
        false if collection has no data.
 */
bool summariseInstrumentDataSize(IQMDataCollection *collection, SummaryInfoType &instrumentsInfo)
{
    if (!collection || collection->size() == 0)
    {
        return false;
    }

    instrumentsInfo.clear();
    const std::size_t collectionSize(collection->size());
    for (std::size_t i(0); i < collectionSize; ++i)
    {
        O2G2Ptr<IQMData> data(collection->get(i));
        const std::string instrument(data->getInstrument());
        const int year(data->getYear());

        instrumentsInfo[instrument][year] += data->getSize();
    }

    return true;
}

/** Print instrument name, year and size.

    @param date
        Date.
    @return
        Year.
 */
void printInstrumentSummaryInfo(const SummaryInfoPairType &summaryInfoPair)
{
    const YearSizeInfoType &yearSizeMap(summaryInfoPair.second);
    YearSizeInfoType::const_iterator it(yearSizeMap.begin());
    YearSizeInfoType::const_iterator end(yearSizeMap.end());

    for(; it != end; ++it)
    {
        // print "<instrument> <year> <size>"
        std::cout << "    " << summaryInfoPair.first << " " << it->first <<
            " " << readableSize(it->second) << std::endl;
    }
}

/** Get string suffix according to number size (KB, MB, GB).

    @param bytes
        Number.
    @return
        String suffix.
 */
std::string readableSize(quotesmgr::int64 bytes)
{
    const char *const units[] = { " Bytes", " KB", " MB", " GB" };
    const double bytesInKB(1024);
    
    double byteCount(static_cast<double>(bytes));
    std::size_t degree(static_cast<std::size_t>(std::log(byteCount) / std::log(bytesInKB)));
    if (degree >= getArraySize(units))
    {
        degree = 0;
    }

    std::ostringstream formatter;
    if (degree >= 1)
    {
        byteCount /= std::pow(bytesInKB, int(degree));
        formatter << std::fixed << std::setprecision(2);
    }
    formatter << byteCount << units[degree];

    return formatter.str();
}

} // namespace util

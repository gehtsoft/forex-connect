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
package removequotes;

/**
 * The implementation of the interface to the Quotes Manager Storage data
 */
class QMData implements IQMData {

    private String mInstrument;
    private String mTimeframe;
    private int mYear;
    private long mSize;
    
    /**
     * Constructor
     * 
     * @param instrument
     *            The name of the instrument<
     * @param timeframe
     *            The name of the timeframe in which the data stored.
     * @param year
     *            The year to which data belongs to
     * @param size
     *            The size of the data in bytes
     */
    QMData(String instrument, String timeframe, int year, long size) {
        mInstrument = instrument;
        mTimeframe = timeframe;
        mYear = year;
        mSize = size;
    }

    /**
     * Gets the name of the instrument
     */
    @Override
    public String getInstrument() {
        return mInstrument;
    }

    /**
     * Gets the name of the timeframe in which the data are stored in cache
     */
    @Override
    public String getTimeframe() {
        return mTimeframe;
    }

    /**
     * Gets the year to which the data belongs to
     */
    @Override
    public int getYear() {
        return mYear;
    }

    /**
     * Gets the size of the data in bytes
     */
    @Override
    public long getSize() {
        return mSize;
    }
}
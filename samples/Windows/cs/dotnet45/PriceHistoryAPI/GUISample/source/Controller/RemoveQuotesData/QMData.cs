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
using System;
using System.Collections.Generic;
using System.Text;

namespace GUISample
{
    /// <summary>
    /// The implementation of the interface to the Quotes Manager Storage data
    /// </summary>
    class QMData : IQMData
    {
        /// <summary>
        /// Gets the name of the instrument
        /// </summary>
        public string Instrument
        {
            get 
            {
                return mInstrument;
            }
        }
        private string mInstrument;

        /// <summary>
        /// Gets the name of the timeframe in which the data are stored in cache
        /// </summary>
        public string Timeframe
        {
            get 
            {
                return mTimeframe;
            }
        }
        private string mTimeframe;

        /// <summary>
        /// Gets the year to which the data belongs to
        /// </summary>
        public int Year
        {
            get 
            {
                return mYear;
            }
        }
        private int mYear;

        /// <summary>
        /// Gets the size of the data in bytes
        /// </summary>
        public long Size
        {
            get 
            {
                return mSize;
            }
        }
        private long mSize;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instrument">The name of the instrument</param>
        /// <param name="timeframe">The name of the timeframe in which the data stored</param>
        /// <param name="year">The year to which data belongs to</param>
        /// <param name="size">The size of the data in bytes</param>
        internal QMData(string instrument, string timeframe, int year, long size)
        {
            mInstrument = instrument;
            mTimeframe = timeframe;
            mYear = year;
            mSize = size;
        }
    }
}

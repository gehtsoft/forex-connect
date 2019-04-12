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
    /// Interface to the offer
    /// </summary>
    interface IOffer
    {
        /// <summary>
        /// Gets the instrument name
        /// </summary>
        string Instrument
        {
            get;
        }

        /// <summary>
        /// Gets the date/time (in UTC time zone) when the offer was updated the last time
        /// </summary>
        DateTime LastUpdate
        {
            get;
        }

        /// <summary>
        /// Gets the latest offer bid price
        /// </summary>
        double Bid
        {
            get;
        }

        /// <summary>
        /// Gets the latest offer ask price
        /// </summary>
        double Ask
        {
            get;
        }

        /// <summary>
        /// Gets the offer accumulated last minute volume
        /// </summary>
        int MinuteVolume
        {
            get;
        }

        /// <summary>
        /// Gets the number of significant digits after decimal point
        /// </summary>
        int Digits
        {
            get;
        }

        /// <summary>
        /// Makes a copy of the offer
        /// </summary>
        /// <returns></returns>
        IOffer Clone();
    }
}

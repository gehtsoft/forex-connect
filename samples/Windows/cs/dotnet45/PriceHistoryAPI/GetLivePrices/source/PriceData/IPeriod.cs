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

namespace GetLivePrices
{
    /// <summary>
    /// A price bar (candle)
    /// </summary>
    public interface IBar
    {
        /// <summary>
        /// Open (the first price) of the time period
        /// </summary>
        double Open { get; }

        /// <summary>
        /// High (the greatest price) of the time period
        /// </summary>
        double High { get; }

        /// <summary>
        /// Low (the smallest price) of the time period
        /// </summary>
        double Low { get; }

        /// <summary>
        /// Close (the latest price) of the time period
        /// </summary>
        double Close { get; }
    }

    /// <summary>
    /// The interface for the price information in the time period
    /// </summary>
    public interface IPeriod
    {
        /// <summary>
        /// Gets date/time when the period starts (In UTC time zone)
        /// </summary>
        DateTime Time { get; }

        /// <summary>
        /// Gets ask bar
        /// </summary>
        IBar Ask { get; }

        /// <summary>
        /// Gets bid bar
        /// </summary>
        IBar Bid { get; }

        /// <summary>
        /// Gets tick volume
        /// </summary>
        int Volume { get; }
    }
}

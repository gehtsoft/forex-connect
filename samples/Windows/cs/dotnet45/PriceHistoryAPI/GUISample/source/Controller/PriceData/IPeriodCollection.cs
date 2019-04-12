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
    delegate void IPeriodCollection_Updated(IPeriodCollection collection, int index);

    /// <summary>
    /// Interface to the collection of the price periods
    /// </summary>
    interface IPeriodCollection : IEnumerable<IPeriod>, IDisposable
    {
        /// <summary>
        /// The number of periods in the collection
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Get the period by its index
        /// </summary>
        /// <param name="index">The index of the period. The oldest period has index 0.</param>
        /// <returns></returns>
        IPeriod this[int index] { get; }

        /// <summary>
        /// The event raised when the collection is updated with a new tick
        /// </summary>
        event IPeriodCollection_Updated OnCollectionUpdate;

        /// <summary>
        /// Gets the instrument name of the collection
        /// </summary>
        string Instrument { get; }

        /// <summary>
        /// Gets the timeframe name of the collection
        /// </summary>
        string Timeframe { get; }

        /// <summary>
        /// Gets flag indicating that the collection is alive (i.e. is updated when a new price coming)
        /// </summary>
        bool IsAlive { get; }
    }
}

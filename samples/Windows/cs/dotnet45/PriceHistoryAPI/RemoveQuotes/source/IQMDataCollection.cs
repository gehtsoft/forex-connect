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

namespace RemoveQuotes
{
    /// <summary>
    /// The interface to the collection of Quotes Manager cache data
    /// </summary>
    public interface IQMDataCollection : IEnumerable<IQMData>
    {
        /// <summary>
        /// Gets the number of data slices in the collection
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Gets the data slice by its index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        IQMData this[int index] {get; }
    }
}

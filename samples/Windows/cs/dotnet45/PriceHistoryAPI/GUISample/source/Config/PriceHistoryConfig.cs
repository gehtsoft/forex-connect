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
    /// Configuration of the Price History API
    /// </summary>
    public class PriceHistoryConfig
    {
        private string mCachePath;

        /// <summary>
        /// The local path where price cache is located
        /// </summary>
        public string CachePath
        {
            get
            {
                return mCachePath;
            }
            set
            {
                mCachePath = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cachePath">The local path where price cache is located</param>
        internal PriceHistoryConfig(string cachePath)
        {
            mCachePath = cachePath;
        }
    }
}

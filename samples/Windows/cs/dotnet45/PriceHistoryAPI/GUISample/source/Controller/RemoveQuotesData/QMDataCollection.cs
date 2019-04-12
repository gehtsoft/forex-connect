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
    /// The implementation of the collection of quotes manager data
    /// </summary>
    class QMDataCollection : IQMDataCollection
    {
        /// <summary>
        /// The storage for data blocks
        /// </summary>
        private List<QMData> mList = new List<QMData>();

        /// <summary>
        /// Gets the number of data in the collection
        /// </summary>
        public int Count
        {
            get 
            {
                return mList.Count; 
            }
        }

        /// <summary>
        /// Gets the description of the data by its index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IQMData this[int index]
        {
            get
            {
                return mList[index];
            }
        }

        /// <summary>
        /// Gets typped enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IQMData> GetEnumerator()
        {
            return new EnumeratorHelper<QMData, IQMData>(mList.GetEnumerator());
        }

        /// <summary>
        /// Gets untypped enumerator
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        /// <summary>
        /// Adds a new data descriptor in the collection
        /// </summary>
        /// <param name="data"></param>
        internal void Add(QMData data)
        {
            mList.Add(data);
        }
    }
}

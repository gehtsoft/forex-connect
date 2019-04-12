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
    /// The helper class which turns IEnumerator of class instances into IEnumerator
    /// of interface instances.
    /// </summary>
    /// <typeparam name="T">The class implementing the interface</typeparam>
    /// <typeparam name="I">The interface</typeparam>
    class EnumeratorHelper<T, I> : IEnumerator<I> where T : I
    {
        IEnumerator<T> mEnumerator;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="enumerator">Enumerator of class instances</param>
        public EnumeratorHelper(IEnumerator<T> enumerator)
        {
            mEnumerator = enumerator;
        }

        /// <summary>
        /// Gets the current element of enumeration (typped).
        /// </summary>
        public I Current
        {
            get 
            {
                return mEnumerator.Current;
            }
        }

        /// <summary>
        /// Gets the current element of the enumeration (as an object).
        /// </summary>
        object System.Collections.IEnumerator.Current
        {
            get
            {
                return mEnumerator.Current;
            }
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            mEnumerator.Dispose();
        }

        /// <summary>
        /// Moves to the next element.
        /// </summary>
        /// <returns>false if there is no more elements</returns>
        public bool MoveNext()
        {
            return mEnumerator.MoveNext();
        }

        /// <summary>
        /// Resets the enumerator into its initial state.
        /// </summary>
        public void Reset()
        {
            mEnumerator.Reset();
        }
    }
}

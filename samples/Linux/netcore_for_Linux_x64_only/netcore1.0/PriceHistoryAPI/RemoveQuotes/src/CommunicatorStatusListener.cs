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
using System.Threading;

using Candleworks.PriceHistoryMgr;

namespace RemoveQuotes
{
    /// <summary>
    /// The Price History API communicator request status listener.
    /// </summary>
    class CommunicatorStatusListener : IPriceHistoryCommunicatorStatusListener
    {
        /// <summary>
        /// The state of the PriceHistoryCommunicator.
        /// </summary>
        private bool mReady;
        /// <summary>
        /// The event of the PriceHistoryCommunicator.
        /// </summary>
        private EventWaitHandle mSyncCommunicatorEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CommunicatorStatusListener()
        {
            mSyncCommunicatorEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        /// <summary>
        /// Gets the PriceHistoryCommunicator state.
        /// </summary>
        public bool Ready
        {
            get
            {
                return mReady;
            }
        }

        /// <summary>
        /// Resets the PriceCommunicator state.
        /// </summary>
        public void Reset()
        {
            mReady = false;
        }

        /// <summary>
        /// Waits for a PriceCommunicator event.
        /// </summary>
        public bool WaitEvents()
        {
            bool eventOcurred = mSyncCommunicatorEvent.WaitOne(30000);
            if (!eventOcurred)
                Console.WriteLine("Timeout occurred during waiting for communicator status is ready");
            return eventOcurred;
        }

        #region IPriceHistoryCommunicatorStatusListener members

        /// <summary>
        /// Listener: when the communicator initialization is failed.
        /// </summary>
        /// <param name="error"></param>
        public void onCommunicatorInitFailed(PriceHistoryError error)
        {
            Console.WriteLine("Communicator initialization error: " + error.Message);
            mSyncCommunicatorEvent.Set();
        }

        /// <summary>
        /// Listener: when the communicator status is changed.
        /// </summary>
        /// <param name="ready"></param>
        public void onCommunicatorStatusChanged(bool ready)
        {
            mReady = ready;
            mSyncCommunicatorEvent.Set();
        }

        #endregion
    }
}

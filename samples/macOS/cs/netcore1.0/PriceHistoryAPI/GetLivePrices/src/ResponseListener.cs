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
using System.Threading;

using fxcore2;
using Candleworks.PriceHistoryMgr;

namespace GetLivePrices
{
    /// <summary>
    /// The Price History API communicator request result listener.
    /// </summary>
    class ResponseListener : IPriceHistoryCommunicatorListener
    {
        /// <summary>
        /// The request to the PriceHistoryCommunicator.
        /// </summary>
        private IPriceHistoryCommunicatorRequest mRequest;
        /// <summary>
        /// The response from the PriceHistoryCommunicator.
        /// </summary>
        private IPriceHistoryCommunicatorResponse mResponse;
        /// <summary>
        /// The event of the ResponseListener.
        /// </summary>
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ResponseListener()
        {
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        /// <summary>
        /// Sets request.
        /// </summary>
        /// <param name="request"></param>
        public void SetRequest(IPriceHistoryCommunicatorRequest request)
        {
            mResponse = null;
            mRequest = request;
        }

        /// <summary>
        /// Waits for a response event.
        /// </summary>
        public bool Wait()
        {
            return mSyncResponseEvent.WaitOne();
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public IPriceHistoryCommunicatorResponse GetResponse()
        {
            return mResponse;
        }

        #region IPriceHistoryCommunicatorListener members

        /// <summary>
        /// Listener: Price history request is completed.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public void onRequestCompleted(IPriceHistoryCommunicatorRequest request,
                                       IPriceHistoryCommunicatorResponse response)
        {
            if (mRequest == request)
            {
                mResponse = response;
                mRequest = null;

                mSyncResponseEvent.Set();
            }
        }

        /// <summary>
        /// Listener: request failed.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="error"></param>
        public void onRequestFailed(IPriceHistoryCommunicatorRequest request,
                                    PriceHistoryError error)
        {
            if (mRequest == request)
            {
                Console.WriteLine("Request failed: " + error);

                mResponse = null;
                mRequest = null;

                mSyncResponseEvent.Set();
            }
        }

        /// <summary>
        /// Listener: when the request is cancelled.
        /// </summary>
        /// <param name="request"></param>
        public void onRequestCancelled(IPriceHistoryCommunicatorRequest request)
        {
            if (mRequest == request)
            {
                Console.WriteLine("Request cancelled.");

                mResponse = null;
                mRequest = null;

                mSyncResponseEvent.Set();
            }
        }

        #endregion
    }
}


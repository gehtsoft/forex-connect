using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace RemoveOrder
{
    class ResponseListener : IO2GResponseListener
    {
        private string mRequestID;
        private EventWaitHandle mSyncResponseEvent = null;

        /// <summary>
        /// ctor
        /// </summary>
        public ResponseListener()
        {
            mRequestID = string.Empty;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public void SetRequestID(string sRequestID)
        {
            mRequestID = sRequestID;
        }

        public bool WaitEvents()
        {
            return mSyncResponseEvent.WaitOne(30000);
        }

        public void StopWaiting()
        {
            mSyncResponseEvent.Set();
        }

        // Implementation of IO2GResponseListener interface public method onRequestCompleted
        public void onRequestCompleted(string sRequestID, O2GResponse response)
        {
        }

        // Implementation of IO2GResponseListener interface public method onRequestFailed
        public void onRequestFailed(string sRequestID, string sError)
        {
            if (mRequestID.Equals(sRequestID))
            {
                Console.WriteLine("Request failed: " + sError);
                StopWaiting();
            }
        }

        // Implementation of IO2GResponseListener interface public method onTablesUpdates
        public void onTablesUpdates(O2GResponse response)
        {
        }
    }
}


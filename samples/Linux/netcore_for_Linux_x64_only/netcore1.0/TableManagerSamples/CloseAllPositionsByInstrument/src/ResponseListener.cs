using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace CloseAllPositionsByInstrument
{
    class ResponseListener : IO2GResponseListener
    {
        private List<string> mRequestIDs;
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// ctor
        /// </summary>
        public ResponseListener()
        {
            mRequestIDs = new List<string>();
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public void SetRequestIDs(List<string> requestIDs)
        {
            mRequestIDs.Clear();
            foreach (string sRequestID in requestIDs)
            {
                mRequestIDs.Add(sRequestID);
            }
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
            if (mRequestIDs.Contains(sRequestID))
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


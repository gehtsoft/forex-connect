using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using fxcore2;

namespace PartialFill
{
    class ResponseListener : IO2GResponseListener
    {
        private string mRequestID;
        private O2GResponse mResponse;
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener()
        { 
            mRequestID = string.Empty;
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);          
        }

        public void SetRequestID(string sRequestID)
        {
            mResponse = null;
            mRequestID = sRequestID;
        }

        public bool WaitEvents()
        {
            return mSyncResponseEvent.WaitOne(30000);
        }

        public O2GResponse GetResponse()
        {
            return mResponse;
        }

        public void StopWaiting()
        {
            mSyncResponseEvent.Set();
        }

        #region IO2GResponseListener Members

        public void onRequestCompleted(string sRequestId, O2GResponse response)
        {
        }

        public void onRequestFailed(string sRequestID, string sError)
        {
            if (mRequestID.Equals(sRequestID))
            {
                Console.WriteLine("Request failed: " + sError);
                StopWaiting();
            }
        }

        public void onTablesUpdates(O2GResponse response)
        {
        }

        #endregion


    }
}


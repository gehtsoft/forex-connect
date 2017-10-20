using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace GetHistPrices
{
    class ResponseListener : IO2GResponseListener
    {
        private O2GSession mSession;
        private string mRequestID;
        private O2GResponse mResponse;
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener(O2GSession session)
        {
            mRequestID = string.Empty;
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            mSession = session;
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

        #region IO2GResponseListener Members

        public void onRequestCompleted(string sRequestId, O2GResponse response)
        {
            if (mRequestID.Equals(response.RequestID))
            {
                mResponse = response;
                mSyncResponseEvent.Set();
            }
        }

        public void onRequestFailed(string sRequestID, string sError)
        {
            if (mRequestID.Equals(sRequestID))
            {
                mResponse = null;
                if (string.IsNullOrEmpty(sError))
                {
                    Console.WriteLine("There is no more data");
                }
                else
                {
                    Console.WriteLine("Request failed: " + sError);
                }
                mSyncResponseEvent.Set();
            }
        }

        public void onTablesUpdates(O2GResponse data)
        {
        }

        #endregion

    }
}


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace IfThen
{
    class ResponseListener : IO2GResponseListener
    {
        private O2GSession mSession;
        private List<string> mRequestIDs;
        private O2GResponse mResponse;
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener(O2GSession session)
        {
            mRequestIDs = new List<string>();
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            mSession = session;
        }

        public void SetRequestIDs(List<string> requestIDs)
        {
            mResponse = null;
            mRequestIDs.Clear();
            foreach (string sOrderID in requestIDs)
            {
                mRequestIDs.Add(sOrderID);
            }
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
            if (mRequestIDs.Contains(response.RequestID))
            {
                mResponse = response;
                if (response.Type != O2GResponseType.CreateOrderResponse)
                {
                    mSyncResponseEvent.Set();
                }
            }
        }

        public void onRequestFailed(string sRequestID, string sError)
        {
            if (mRequestIDs.Contains(sRequestID))
            {
                Console.WriteLine("Request failed: " + sError);
                mSyncResponseEvent.Set();
            }
        }

        public void onTablesUpdates(O2GResponse data)
        {
            O2GResponseReaderFactory factory = mSession.getResponseReaderFactory();
            if (factory != null)
            {
                O2GTablesUpdatesReader reader = factory.createTablesUpdatesReader(data);
                for (int ii = 0; ii < reader.Count; ii++)
                {
                    if (reader.getUpdateTable(ii) == O2GTableType.Orders)
                    {
                        O2GOrderRow orderRow = reader.getOrderRow(ii);
                        if (reader.getUpdateType(ii) == O2GTableUpdateType.Insert)
                        {
                            if (mRequestIDs.Contains(orderRow.RequestID))
                            {
                                Console.WriteLine("The order has been added. OrderID={0}, Type={1}, BuySell={2}, Rate={3}, TimeInForce={4}",
                                    orderRow.OrderID, orderRow.Type, orderRow.BuySell, orderRow.Rate, orderRow.TimeInForce);
                                mRequestIDs.Remove(orderRow.RequestID);
                            }
                            if (mRequestIDs.Count == 0)
                            {
                                mSyncResponseEvent.Set();
                            }
                        }
                    }
                }
            }
        }

        #endregion


    }
}


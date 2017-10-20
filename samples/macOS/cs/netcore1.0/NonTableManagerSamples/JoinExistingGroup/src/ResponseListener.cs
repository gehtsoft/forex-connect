using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace JoinExistingGroup
{
    class ResponseListener : IO2GResponseListener
    {
        private O2GSession mSession;
        private string mRequestID;
        private List<string> mOrderIDs;
        private O2GResponse mResponse;
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener(O2GSession session)
        {
            mRequestID = string.Empty;
            mOrderIDs = new List<string>();
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            mSession = session;
        }

        public void SetRequestID(string sRequestID)
        {
            mResponse = null;
            mRequestID = sRequestID;
        }

        public void SetOrderIDs(List<string> orderIDs)
        {
            mOrderIDs.Clear();
            foreach (string sOrderID in orderIDs)
            {
                mOrderIDs.Add(sOrderID);
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
            if (mRequestID.Equals(response.RequestID))
            {
                mResponse = response;
                if (response.Type == O2GResponseType.GetOrders)
                {
                    mSyncResponseEvent.Set();
                }
            }
        }

        public void onRequestFailed(string sRequestID, string sError)
        {
            if (mRequestID.Equals(sRequestID))
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
                        if (reader.getUpdateType(ii) == O2GTableUpdateType.Update)
                        {
                            if (mOrderIDs.Contains(orderRow.OrderID))
                            {
                                Console.WriteLine("The order has been updated. OrderID={0}, ContingentOrderID={1}, ContingencyType={2}", orderRow.OrderID, orderRow.ContingentOrderID, orderRow.ContingencyType);
                                mOrderIDs.Remove(orderRow.OrderID);
                            }
                            if (mOrderIDs.Count == 0)
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


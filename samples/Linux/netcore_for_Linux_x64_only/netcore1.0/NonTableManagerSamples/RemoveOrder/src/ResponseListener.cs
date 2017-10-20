using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace RemoveOrder
{
    class ResponseListener : IO2GResponseListener
    {
        private O2GSession mSession;
        private string mRequestID;
        private O2GResponse mResponse;
        private string mOrderID;
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener(O2GSession session)
        {
            mRequestID = string.Empty;
            mOrderID = string.Empty;
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            mSession = session;
        }

        public void SetRequestID(string sRequestID)
        {
            mOrderID = string.Empty;
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

        public string GetOrderID()
        {
            return mOrderID;
        }

        #region IO2GResponseListener Members

        public void onRequestCompleted(string sRequestId, O2GResponse response)
        {
            if (mRequestID.Equals(response.RequestID))
            {
                mResponse = response;
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
                        if (orderRow.RequestID.Equals(mRequestID))
                        {
                            switch (reader.getUpdateType(ii))
                            {
                                case O2GTableUpdateType.Insert:
                                    if (IsLimitEntryOrder(orderRow) && string.IsNullOrEmpty(mOrderID))
                                    {
                                        mOrderID = orderRow.OrderID;
                                        Console.WriteLine("The order has been added. Order ID: {0}, Rate: {1}, Time In Force: {2}",
                                            orderRow.OrderID,
                                            orderRow.Rate,
                                            orderRow.TimeInForce);
                                        mSyncResponseEvent.Set();
                                    }
                                    break;
                                case O2GTableUpdateType.Delete:
                                    Console.WriteLine("The order has been deleted. Order ID: {0}", orderRow.OrderID);
                                    mSyncResponseEvent.Set();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        private bool IsLimitEntryOrder(O2GOrderRow order)
        {
            return order.Type.StartsWith("LE");
        }


    }
}


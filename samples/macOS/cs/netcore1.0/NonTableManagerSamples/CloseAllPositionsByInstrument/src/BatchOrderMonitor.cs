using System;
using System.Collections.Generic;
using System.Text;
using fxcore2;

namespace OrdersMonitor
{
    class BatchOrderMonitor
    {
        private List<string> mRequestIDs;
        private List<OrderMonitor> mMonitors;

        /// <summary>
        /// ctor
        /// </summary>
        public BatchOrderMonitor()
        {
            mRequestIDs = new List<string>();
            mMonitors = new List<OrderMonitor>();
        }
        
        public void SetRequestIDs(List<string> requestIDs)
        {
            mRequestIDs.Clear();
            foreach(string sRequestID in requestIDs)
                mRequestIDs.Add(sRequestID);
        }
        
        public void OnRequestCompleted(string sRequestID, O2GResponse response)
        {
            //STUB
        }
        
        public void OnRequestFailed(string sRequestID)
        {
            if (IsOwnRequest(sRequestID))
                RemoveRequestID(sRequestID);
        }
        
        public void OnTradeAdded(O2GTradeRow tradeRow)
        {
            for (int i = 0; i < mMonitors.Count; i++)
                mMonitors[i].OnTradeAdded(tradeRow);
        }
        
        public void OnOrderAdded(O2GOrderRow order)
        {
            string sRequestID = order.RequestID;
            if (IsOwnRequest(sRequestID))
            {
                if (OrderMonitor.IsClosingOrder(order) || OrderMonitor.IsOpeningOrder(order))
                {
                    AddToMonitoring(order);
                }
            }
        }
        
        public void OnOrderDeleted(O2GOrderRow order)
        {
            for (int i = 0; i < mMonitors.Count; i++)
                mMonitors[i].OnOrderDeleted(order);
        }
        
        public void OnMessageAdded(O2GMessageRow message)
        {
            for (int i = 0; i < mMonitors.Count; i++)
                mMonitors[i].OnMessageAdded(message);
        }
        
        public void OnClosedTradeAdded(O2GClosedTradeRow closedTrade)
        {
            for (int i = 0; i < mMonitors.Count; i++)
                mMonitors[i].OnClosedTradeAdded(closedTrade);
        }
        
        public bool IsBatchExecuted()
        {
            bool bAllCompleted = true;
            for (int i = 0; i < mMonitors.Count; i++)
            {
                if (!mMonitors[i].IsOrderCompleted)
                {
                    bAllCompleted = false;
                    break;
                }
            }

            bool result = mRequestIDs.Count == 0 && bAllCompleted;
            return result;
        }
        
        public List<OrderMonitor> GetMonitors()
        {
            List<OrderMonitor> result = new List<OrderMonitor>();
            foreach (OrderMonitor monitor in mMonitors)
            {
                result.Add(monitor);
            }
            return result;
        }

        public event EventHandler BatchOrderCompleted;

        
        private bool IsOwnRequest(string sRequestID)
        {
            return mRequestIDs.Contains(sRequestID);
        }
        
        private void AddToMonitoring(O2GOrderRow order)
        {
            OrderMonitor monitor = new OrderMonitor(order);
            monitor.OrderCompleted += new EventHandler(monitor_OrderCompleted);
            mMonitors.Add(monitor);
        }
        
        private void RemoveRequestID(string sRequestID)
        {
            if (mRequestIDs.Contains(sRequestID))
                mRequestIDs.Remove(sRequestID);
        }

        #region order monitor event handlers

        private void monitor_OrderCompleted(object sender, EventArgs e)
        {
            OrderMonitor monitor = (OrderMonitor)sender;
            RemoveRequestID(monitor.Order.RequestID);

            if (BatchOrderCompleted != null && IsBatchExecuted())
            {
                BatchOrderCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using fxcore2;

namespace IfThen
{
    class TableListener : IO2GTableListener
    {
        private ResponseListener mResponseListener;
        private List<string> mRequestIDs;
        private string mOrderID;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="responseListener"></param>
        public TableListener(ResponseListener responseListener)
        {
            mResponseListener = responseListener;
            mRequestIDs = new List<string>();
            mOrderID = string.Empty;
        }

        public void SetRequestIDs(List<string> requestIDs)
        {
            mRequestIDs.Clear();
            foreach (string sOrderID in requestIDs)
            {
                mRequestIDs.Add(sOrderID);
            }
        }

        public string GetOrderID()
        {
            return mOrderID;
        }

        #region IO2GTableListener Members

        // Implementation of IO2GTableListener interface public method onAdded
        public void onAdded(string sRowID, O2GRow rowData)
        {
            if (rowData.TableType == O2GTableType.Orders)
            {
                O2GOrderRow orderRow = (O2GOrderRow)rowData;
                if (mRequestIDs.Contains(orderRow.RequestID))
                {
                    Console.WriteLine("The order has been added. OrderID={0}, Type={1}, Rate={2}", orderRow.OrderID, orderRow.Type, orderRow.Rate);
                    mRequestIDs.Remove(orderRow.RequestID);
                }
                if (mRequestIDs.Count == 0)
                {
                    mResponseListener.StopWaiting();
                }
            }
        }

        // Implementation of IO2GTableListener interface public method onChanged
        public void onChanged(string sRowID, O2GRow rowData)
        {
        }

        // Implementation of IO2GTableListener interface public method onDeleted
        public void onDeleted(string sRowID, O2GRow rowData)
        {
        }

        public void onStatusChanged(O2GTableStatus status)
        {
        }

        #endregion

        public void SubscribeEvents(O2GTableManager manager)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.Orders);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
        }

        public void UnsubscribeEvents(O2GTableManager manager)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.Orders);
            ordersTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
        }

    }
}

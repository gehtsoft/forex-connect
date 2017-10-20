using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using fxcore2;

namespace RemoveOrder
{
    class TableListener : IO2GTableListener
    {
        private ResponseListener mResponseListener;
        private string mRequestID;
        private string mOrderID;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="responseListener"></param>
        public TableListener(ResponseListener responseListener)
        {
            mResponseListener = responseListener;
            mRequestID = string.Empty;
            mOrderID = string.Empty;
        }

        public void SetRequestID(string sRequestID)
        {
            mRequestID = sRequestID;
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
                if (mRequestID.Equals(orderRow.RequestID))
                {
                    if (IsLimitEntryOrder(orderRow) && string.IsNullOrEmpty(mOrderID))
                    {
                        mOrderID = orderRow.OrderID;
                        Console.WriteLine("The order has been added. Order ID: {0}, Rate: {1}, Time In Force: {2}",
                            orderRow.OrderID,
                            orderRow.Rate,
                            orderRow.TimeInForce);
                        mResponseListener.StopWaiting();
                    }
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
            if (rowData.TableType == O2GTableType.Orders)
            {
                O2GOrderRow orderRow = (O2GOrderRow)rowData;
                if (mRequestID.Equals(orderRow.RequestID))
                {
                    if (!string.IsNullOrEmpty(mOrderID))
                    {
                        Console.WriteLine("The order has been deleted. Order ID: {0}", orderRow.OrderID);
                        mResponseListener.StopWaiting();
                    }
                }
            }
        }

        public void onStatusChanged(O2GTableStatus status)
        {
        }

        #endregion

        public void SubscribeEvents(O2GTableManager manager)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.Orders);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Delete, this);
        }

        public void UnsubscribeEvents(O2GTableManager manager)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.Orders);
            ordersTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
            ordersTable.unsubscribeUpdate(O2GTableUpdateType.Delete, this);
        }

        private bool IsLimitEntryOrder(O2GOrderRow order)
        {
            return order.Type.StartsWith("LE");
        }

    }
}

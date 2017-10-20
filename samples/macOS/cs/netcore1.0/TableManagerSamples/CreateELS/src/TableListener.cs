using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using fxcore2;

namespace CreateELS
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
                    Console.WriteLine("The order has been added. OrderID={0}, Type={1}, Rate={2}", orderRow.OrderID, orderRow.Type, orderRow.Rate);
                }
                mResponseListener.StopWaiting();
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

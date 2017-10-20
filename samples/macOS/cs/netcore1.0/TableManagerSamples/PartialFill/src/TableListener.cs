using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace PartialFill
{
    class TableListener : IO2GTableListener
    {
        private ResponseListener mResponseListener;
        private string mRequestID;
        private string mOrderID;

        /// <summary>
        /// ctor
        /// </summary>
        public TableListener(ResponseListener responseListener)
        {
            mResponseListener = responseListener;
            mRequestID = string.Empty;
            mOrderID = string.Empty;
        }

        public void SetRequestID(string sRequestID)
        {
            mRequestID = sRequestID;
            mOrderID = string.Empty;
        }

        #region IO2GTableListener Members

        public void onAdded(string sRowID, O2GRow rowData)
        {
            O2GTableType type = rowData.TableType;
            switch (type)
            {
                case O2GTableType.Orders:
                    O2GOrderRow orderRow = (O2GOrderRow)rowData;
                    if (mRequestID.Equals(orderRow.RequestID))
                    {
                        mOrderID = orderRow.OrderID;
                        PrintOrder("New order is added", orderRow);
                    }
                    break;
                case O2GTableType.Trades:
                    O2GTradeRow tradeRow = (O2GTradeRow)rowData;
                    Console.WriteLine("Position is opened: TradeID='{0}', TradeIDOrigin='{1}'",
                            tradeRow.TradeID, tradeRow.TradeIDOrigin);
                    break;
                case O2GTableType.ClosedTrades:
                    O2GClosedTradeRow closedTradeRow = (O2GClosedTradeRow)rowData;
                    Console.WriteLine("Position is closed: TradeID='{0}'",
                            closedTradeRow.TradeID);
                    break;
                case O2GTableType.Messages:
                    O2GMessageRow messageRow = (O2GMessageRow)rowData;
                    string text = messageRow.Text;
                    int findPos = text.IndexOf(mOrderID);
                    if (findPos >= 0)
                    {
                        Console.WriteLine("Feature='{0}', Message='{1}'",
                                messageRow.Feature, text);
                    }
                    break;
            }
        }

        public void onChanged(string sRowID, O2GRow rowData)
        {
            if (rowData.TableType == O2GTableType.Accounts)
            {
                O2GAccountTableRow account = (O2GAccountTableRow)rowData;
                Console.WriteLine("Balance: {0}, Equity: {1}", account.Balance, account.Equity);
            }
            if (rowData.TableType == O2GTableType.Orders)
            {
                O2GOrderRow orderRow = (O2GOrderRow)rowData;
                PrintOrder("An order is changed", orderRow);
            }
        }

        public void onDeleted(string sRowID, O2GRow rowData)
        {
            if (rowData.TableType == O2GTableType.Orders)
            {
                O2GOrderRow orderRow = (O2GOrderRow)rowData;
                if (mRequestID.Equals(orderRow.RequestID))
                {
                    string sStatus = orderRow.Status;
                    if (sStatus.Equals("R"))
                    {
                        PrintOrder("An order has been rejected", orderRow);
                    }
                    else
                    {
                        PrintOrder("An order is going to be removed", orderRow);
                    }
                    mResponseListener.StopWaiting();
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
            O2GTradesTable tradesTable = (O2GTradesTable)manager.getTable(O2GTableType.Trades);
            O2GMessagesTable messagesTable = (O2GMessagesTable)manager.getTable(O2GTableType.Messages);
            O2GClosedTradesTable closedTradesTable = (O2GClosedTradesTable)manager.getTable(O2GTableType.ClosedTrades);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Update, this);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Delete, this);
            tradesTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
            tradesTable.subscribeUpdate(O2GTableUpdateType.Update, this);
            closedTradesTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
            messagesTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
        }

        public void UnsubscribeEvents(O2GTableManager manager)
        {
            O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.Orders);
            O2GTradesTable tradesTable = (O2GTradesTable)manager.getTable(O2GTableType.Trades);
            O2GMessagesTable messagesTable = (O2GMessagesTable)manager.getTable(O2GTableType.Messages);
            O2GClosedTradesTable closedTradesTable = (O2GClosedTradesTable)manager.getTable(O2GTableType.ClosedTrades);
            ordersTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Update, this);
            ordersTable.unsubscribeUpdate(O2GTableUpdateType.Delete, this);
            tradesTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
            tradesTable.unsubscribeUpdate(O2GTableUpdateType.Update, this);
            closedTradesTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
            messagesTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
        }

        private void PrintOrder(string sCaption, O2GOrderRow orderRow)
        {
            Console.WriteLine("{0}: OrderID='{1}', TradeID='{2}', Status='{3}', " +
                    "Amount='{4}', OriginAmount='{5}', FilledAmount='{6}'",
                    sCaption, orderRow.OrderID, orderRow.TradeID, orderRow.Status,
                    orderRow.Amount, orderRow.OriginAmount, orderRow.FilledAmount);
        }
    }
}

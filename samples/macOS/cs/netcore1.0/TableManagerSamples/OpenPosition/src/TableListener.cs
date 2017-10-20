using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using fxcore2;
using OrdersMonitor;

namespace OpenPosition
{
    class TableListener : IO2GTableListener
    {
        private OrderMonitor mOrderMonitor;
        private ResponseListener mResponseListener;
        private string mRequestID;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="responseListener"></param>
        public TableListener(ResponseListener responseListener)
        {
            mOrderMonitor = null;
            mResponseListener = responseListener;
            mRequestID = string.Empty;
        }

        public void SetRequestID(string sRequestID)
        {
            mRequestID = sRequestID;
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
                        if ((OrderMonitor.IsClosingOrder(orderRow) || OrderMonitor.IsOpeningOrder(orderRow)) &&
                                mOrderMonitor == null)
                        {
                            Console.WriteLine("The order has been added. Order ID: {0}, Rate: {1}, Time In Force: {2}",
                                orderRow.OrderID,
                                orderRow.Rate,
                                orderRow.TimeInForce);
                            mOrderMonitor = new OrderMonitor(orderRow);
                        }
                    }
                    break;
                case O2GTableType.Trades:
                    O2GTradeRow tradeRow = (O2GTradeRow)rowData;
                    if (mOrderMonitor != null)
                    {
                        mOrderMonitor.OnTradeAdded(tradeRow);
                        if (mOrderMonitor.IsOrderCompleted)
                        {
                            PrintResult();
                            mResponseListener.StopWaiting();
                        }
                    }
                    break;
                case O2GTableType.ClosedTrades:
                    O2GClosedTradeRow closedTradeRow = (O2GClosedTradeRow)rowData;
                    if (mOrderMonitor != null)
                    {
                        mOrderMonitor.OnClosedTradeAdded(closedTradeRow);
                        if (mOrderMonitor.IsOrderCompleted)
                        {
                            PrintResult();
                            mResponseListener.StopWaiting();
                        }
                    }
                    break;
                case O2GTableType.Messages:
                    O2GMessageRow messageRow = (O2GMessageRow)rowData;
                    if (mOrderMonitor != null)
                    {
                        mOrderMonitor.OnMessageAdded(messageRow);
                        if (mOrderMonitor.IsOrderCompleted)
                        {
                            PrintResult();
                            mResponseListener.StopWaiting();
                        }
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
        }

        public void onDeleted(string sRowID, O2GRow rowData)
        {
            if (rowData.TableType == O2GTableType.Orders)
            {
                O2GOrderRow orderRow = (O2GOrderRow)rowData;
                if (mRequestID.Equals(orderRow.RequestID))
                {
                    Console.WriteLine("The order has been deleted. Order ID: {0}", orderRow.OrderID);
                    mOrderMonitor.OnOrderDeleted(orderRow);
                    if (mOrderMonitor != null)
                    {
                        if (mOrderMonitor.IsOrderCompleted)
                        {
                            PrintResult();
                            mResponseListener.StopWaiting();
                        }
                    }
                }
            }
        }

        public void onStatusChanged(O2GTableStatus status)
        {
        }

        #endregion

        private void PrintResult()
        {
            if (mOrderMonitor != null)
            {
                OrderMonitor.ExecutionResult result = mOrderMonitor.Result;
                ReadOnlyCollection<O2GTradeRow> trades;
                ReadOnlyCollection<O2GClosedTradeRow> closedTrades;
                O2GOrderRow order = mOrderMonitor.Order;
                string sOrderID = order.OrderID;
                trades = mOrderMonitor.Trades;
                closedTrades = mOrderMonitor.ClosedTrades;

                switch (result)
                {
                    case OrderMonitor.ExecutionResult.Canceled:
                        if (trades.Count > 0)
                        {
                            PrintTrades(trades, sOrderID);
                            PrintClosedTrades(closedTrades, sOrderID);
                            Console.WriteLine("A part of the order has been canceled. Amount = {0}", mOrderMonitor.RejectAmount);
                        }
                        else
                        {
                            Console.WriteLine("The order: OrderID = {0}  has been canceled.", sOrderID);
                            Console.WriteLine("The cancel amount = {0}.", mOrderMonitor.RejectAmount);
                        }
                        break;
                    case OrderMonitor.ExecutionResult.FullyRejected:
                        Console.WriteLine("The order has been rejected. OrderID = {0}", sOrderID);
                        Console.WriteLine("The rejected amount = {0}", mOrderMonitor.RejectAmount);
                        Console.WriteLine("Rejection cause: {0}", mOrderMonitor.RejectMessage);
                        break;
                    case OrderMonitor.ExecutionResult.PartialRejected:
                        PrintTrades(trades, sOrderID);
                        PrintClosedTrades(closedTrades, sOrderID);
                        Console.WriteLine("A part of the order has been rejected. Amount = {0}", mOrderMonitor.RejectAmount);
                        Console.WriteLine("Rejection cause: {0} ", mOrderMonitor.RejectMessage);
                        break;
                    case OrderMonitor.ExecutionResult.Executed:
                        PrintTrades(trades, sOrderID);
                        PrintClosedTrades(closedTrades, sOrderID);
                        break;
                }
            }
        }

        private void PrintTrades(ReadOnlyCollection<O2GTradeRow> trades, string sOrderID)
        {
            if (trades.Count == 0)
                return;
            Console.WriteLine("For the order: OrderID = {0} the following positions have been opened:", sOrderID);

            for (int i = 0; i < trades.Count; i++)
            {
                O2GTradeRow trade = trades[i];
                string sTradeID = trade.TradeID;
                int iAmount = trade.Amount;
                double dRate = trade.OpenRate;
                Console.WriteLine("Trade ID: {0}; Amount: {1}; Rate: {2}", sTradeID, iAmount, dRate);
            }
        }

        private void PrintClosedTrades(ReadOnlyCollection<O2GClosedTradeRow> closedTrades, string sOrderID)
        {
            if (closedTrades.Count == 0)
                return;
            Console.WriteLine("For the order: OrderID = {0} the following positions have been closed: ", sOrderID);

            for (int i = 0; i < closedTrades.Count; i++)
            {
                O2GClosedTradeRow closedTrade = closedTrades[i];
                string sTradeID = closedTrade.TradeID;
                int iAmount = closedTrade.Amount;
                double dRate = closedTrade.CloseRate;
                Console.WriteLine("Closed Trade ID: {0}; Amount: {1}; Closed Rate: {2}", sTradeID, iAmount, dRate);
            }
        }

        public void SubscribeEvents(O2GTableManager manager)
        {
            O2GAccountsTable accountsTable = (O2GAccountsTable)manager.getTable(O2GTableType.Accounts);
            O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.Orders);
            O2GTradesTable tradesTable = (O2GTradesTable)manager.getTable(O2GTableType.Trades);
            O2GMessagesTable messagesTable = (O2GMessagesTable)manager.getTable(O2GTableType.Messages);
            O2GClosedTradesTable closedTradesTable = (O2GClosedTradesTable)manager.getTable(O2GTableType.ClosedTrades);
            accountsTable.subscribeUpdate(O2GTableUpdateType.Update, this);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
            ordersTable.subscribeUpdate(O2GTableUpdateType.Delete, this);
            tradesTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
            tradesTable.subscribeUpdate(O2GTableUpdateType.Update, this);
            closedTradesTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
            messagesTable.subscribeUpdate(O2GTableUpdateType.Insert, this);
        }

        public void UnsubscribeEvents(O2GTableManager manager)
        {
            O2GAccountsTable accountsTable = (O2GAccountsTable)manager.getTable(O2GTableType.Accounts);
            O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.Orders);
            O2GTradesTable tradesTable = (O2GTradesTable)manager.getTable(O2GTableType.Trades);
            O2GMessagesTable messagesTable = (O2GMessagesTable)manager.getTable(O2GTableType.Messages);
            O2GClosedTradesTable closedTradesTable = (O2GClosedTradesTable)manager.getTable(O2GTableType.ClosedTrades);
            accountsTable.unsubscribeUpdate(O2GTableUpdateType.Update, this);
            ordersTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
            ordersTable.unsubscribeUpdate(O2GTableUpdateType.Delete, this);
            tradesTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
            tradesTable.unsubscribeUpdate(O2GTableUpdateType.Update, this);
            closedTradesTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
            messagesTable.unsubscribeUpdate(O2GTableUpdateType.Insert, this);
        }
        
    }
}

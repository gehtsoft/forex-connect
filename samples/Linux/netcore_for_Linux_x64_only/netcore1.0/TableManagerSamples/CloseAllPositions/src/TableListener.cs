using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using fxcore2;
using OrdersMonitor;

namespace CloseAllPositions
{
    class TableListener : IO2GTableListener
    {
        private ResponseListener mResponseListener;
        private BatchOrderMonitor mBatchOrderMonitor;
        private List<string> mRequestIDs;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="responseListener"></param>
        public TableListener(ResponseListener responseListener)
        {
            mResponseListener = responseListener;
            mBatchOrderMonitor = null;
            mRequestIDs = new List<string>();
        }

        public void SetRequestIDs(List<string> requestIDs)
        {
            mRequestIDs.Clear();
            foreach (string sRequestID in requestIDs)
            {
                mRequestIDs.Add(sRequestID);
            }
            mBatchOrderMonitor = new BatchOrderMonitor();
            mBatchOrderMonitor.SetRequestIDs(mRequestIDs);
        }

        #region IO2GTableListener Members

        public void onAdded(string sRowID, O2GRow rowData)
        {
            O2GTableType type = rowData.TableType;
            switch (type)
            {
                case O2GTableType.Orders:
                    O2GOrderRow orderRow = (O2GOrderRow)rowData;
                    if (mBatchOrderMonitor != null && mRequestIDs.Contains(orderRow.RequestID))
                    {
                        Console.WriteLine("The order has been added. Order ID: {0}, Rate: {1}, Time In Force: {2}",
                            orderRow.OrderID,
                            orderRow.Rate,
                            orderRow.TimeInForce);
                        mBatchOrderMonitor.OnOrderAdded(orderRow);
                    }
                    break;
                case O2GTableType.Trades:
                    O2GTradeRow tradeRow = (O2GTradeRow)rowData;
                    if (mBatchOrderMonitor != null)
                    {
                        mBatchOrderMonitor.OnTradeAdded(tradeRow);
                        if (mBatchOrderMonitor.IsBatchExecuted())
                        {
                            PrintResult();
                            mResponseListener.StopWaiting();
                        }
                    }
                    break;
                case O2GTableType.ClosedTrades:
                    O2GClosedTradeRow closedTradeRow = (O2GClosedTradeRow)rowData;
                    if (mBatchOrderMonitor != null)
                    {
                        mBatchOrderMonitor.OnClosedTradeAdded(closedTradeRow);
                        if (mBatchOrderMonitor.IsBatchExecuted())
                        {
                            PrintResult();
                            mResponseListener.StopWaiting();
                        }
                    }
                    break;
                case O2GTableType.Messages:
                    O2GMessageRow messageRow = (O2GMessageRow)rowData;
                    if (mBatchOrderMonitor != null)
                    {
                        mBatchOrderMonitor.OnMessageAdded(messageRow);
                        if (mBatchOrderMonitor.IsBatchExecuted())
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
                if (mRequestIDs.Contains(orderRow.RequestID))
                {
                    Console.WriteLine("The order has been deleted. Order ID: {0}", orderRow.OrderID);
                    if (mBatchOrderMonitor != null)
                    {
                        mBatchOrderMonitor.OnOrderDeleted(orderRow);
                        if (mBatchOrderMonitor.IsBatchExecuted())
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
            foreach (OrderMonitor monitor in mBatchOrderMonitor.GetMonitors())
            {
                Console.WriteLine("Result for OrderID {0}:", monitor.Order.OrderID);
                PrintMonitorResult(monitor);
            }
        }

        private void PrintMonitorResult(OrderMonitor orderMonitor)
        {
            if (orderMonitor != null)
            {
                OrderMonitor.ExecutionResult result = orderMonitor.Result;
                ReadOnlyCollection<O2GTradeRow> trades;
                ReadOnlyCollection<O2GClosedTradeRow> closedTrades;
                O2GOrderRow order = orderMonitor.Order;
                String orderID = order.OrderID;
                trades = orderMonitor.Trades;
                closedTrades = orderMonitor.ClosedTrades;

                switch (result)
                {
                    case OrderMonitor.ExecutionResult.Canceled:
                        if (trades.Count > 0)
                        {
                            PrintTrades(trades, orderID);
                            PrintClosedTrades(closedTrades, orderID);
                            Console.WriteLine("A part of the order has been canceled. Amount = {0}", orderMonitor.RejectAmount);
                        }
                        else
                        {
                            Console.WriteLine("The order: OrderID = {0}  has been canceled.", orderID);
                            Console.WriteLine("The cancel amount = {0}.", orderMonitor.RejectAmount);
                        }
                        break;
                    case OrderMonitor.ExecutionResult.FullyRejected:
                        Console.WriteLine("The order has been rejected. OrderID = {0}", orderID);
                        Console.WriteLine("The rejected amount = {0}", orderMonitor.RejectAmount);
                        Console.WriteLine("Rejection cause: {0}", orderMonitor.RejectMessage);
                        break;
                    case OrderMonitor.ExecutionResult.PartialRejected:
                        PrintTrades(trades, orderID);
                        PrintClosedTrades(closedTrades, orderID);
                        Console.WriteLine("A part of the order has been rejected. Amount = {0}", orderMonitor.RejectAmount);
                        Console.WriteLine("Rejection cause: {0} ", orderMonitor.RejectMessage);
                        break;
                    case OrderMonitor.ExecutionResult.Executed:
                        PrintTrades(trades, orderID);
                        PrintClosedTrades(closedTrades, orderID);
                        break;
                }
            }
        }

        private void PrintTrades(ReadOnlyCollection<O2GTradeRow> trades, string orderID)
        {
            if (trades.Count == 0)
                return;
            Console.WriteLine("For the order: OrderID = {0} the following positions have been opened:", orderID);

            for (int i = 0; i < trades.Count; i++)
            {
                O2GTradeRow trade = trades[i];
                String tradeID = trade.TradeID;
                int amount = trade.Amount;
                double rate = trade.OpenRate;
                Console.WriteLine("Trade ID: {0}; Amount: {1}; Rate: {2}", tradeID, amount, rate);
            }
        }

        private void PrintClosedTrades(ReadOnlyCollection<O2GClosedTradeRow> closedTrades, string orderID)
        {
            if (closedTrades.Count == 0)
                return;
            Console.WriteLine("For the order: OrderID = {0} the following positions have been closed: ", orderID);

            for (int i = 0; i < closedTrades.Count; i++)
            {
                O2GClosedTradeRow closedTrade = closedTrades[i];
                String tradeID = closedTrade.TradeID;
                int amount = closedTrade.Amount;
                double rate = closedTrade.CloseRate;
                Console.WriteLine("Closed Trade ID: {0}; Amount: {1}; Closed Rate: {2}", tradeID, amount, rate);
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using fxcore2;
using OrdersMonitor;

namespace OpenPosition
{
    class ResponseListener : IO2GResponseListener
    {
        private O2GSession mSession;
        private string mRequestID;
        private O2GResponse mResponse;
        private EventWaitHandle mSyncResponseEvent;
        private OrderMonitor mOrderMonitor;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener(O2GSession session)
        {
            mRequestID = string.Empty;
            mResponse = null;
            mOrderMonitor = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            mSession = session;            
        }

        public void SetRequestID(string sRequestID)
        {
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

        #region IO2GResponseListener Members

        public void onRequestCompleted(string sRequestId, O2GResponse response)
        {
            if (mRequestID.Equals(response.RequestID))
            {
                mResponse = response;
                if (response.Type != O2GResponseType.CreateOrderResponse)
                {
                    mSyncResponseEvent.Set();
                }
            }
            //real order execution is processed by Order monitor
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
                    switch (reader.getUpdateTable(ii))
                    {
                        case O2GTableType.Accounts:
                            O2GAccountRow account = reader.getAccountRow(ii);
                            //Show balance updates
                            Console.WriteLine("Balance: {0}", account.Balance);
                            break;
                        case O2GTableType.Orders:
                            O2GOrderRow order = reader.getOrderRow(ii);
                            switch (reader.getUpdateType(ii))
                            {
                                case O2GTableUpdateType.Insert:
                                    if ((OrderMonitor.IsClosingOrder(order) || OrderMonitor.IsOpeningOrder(order)) &&
                                            mOrderMonitor == null)
                                    {
                                        Console.WriteLine("The order has been added. Order ID: {0}, Rate: {1}, Time In Force: {2}",
                                            order.OrderID,
                                            order.Rate,
                                            order.TimeInForce);
                                        mOrderMonitor = new OrderMonitor(order);
                                    }
                                    break;
                                case O2GTableUpdateType.Delete:
                                    if (mOrderMonitor != null)
                                    {
                                        Console.WriteLine("The order has been deleted. Order ID: {0}", order.OrderID);
                                        mOrderMonitor.OnOrderDeleted(order);
                                        if (mOrderMonitor.IsOrderCompleted)
                                        {
                                            PrintResult();
                                            mSyncResponseEvent.Set();
                                        }
                                    }
                                    break;
                            }
                            break;
                        case O2GTableType.Trades:
                            O2GTradeRow trade = reader.getTradeRow(ii);
                            if (reader.getUpdateType(ii) == O2GTableUpdateType.Insert)
                            {
                                if (mOrderMonitor != null)
                                {
                                    mOrderMonitor.OnTradeAdded(trade);
                                    if (mOrderMonitor.IsOrderCompleted)
                                    {
                                        PrintResult();
                                        mSyncResponseEvent.Set();
                                    }
                                }
                            }
                            break;
                        case O2GTableType.ClosedTrades:
                            O2GClosedTradeRow closedTrade = reader.getClosedTradeRow(ii);
                            if (reader.getUpdateType(ii) == O2GTableUpdateType.Insert)
                            {
                                if (mOrderMonitor != null)
                                {
                                    mOrderMonitor.OnClosedTradeAdded(closedTrade);
                                    if (mOrderMonitor.IsOrderCompleted)
                                    {
                                        PrintResult();
                                        mSyncResponseEvent.Set();
                                    }
                                }
                            }
                            break;
                        case O2GTableType.Messages:
                            O2GMessageRow message = reader.getMessageRow(ii);
                            if (reader.getUpdateType(ii) == O2GTableUpdateType.Insert)
                            {
                                if (mOrderMonitor != null)
                                {
                                    mOrderMonitor.OnMessageAdded(message);
                                    if (mOrderMonitor.IsOrderCompleted)
                                    {
                                        PrintResult();
                                        mSyncResponseEvent.Set();
                                    }
                                }
                            }
                            break;
                    }
                }
            }
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
    }
}


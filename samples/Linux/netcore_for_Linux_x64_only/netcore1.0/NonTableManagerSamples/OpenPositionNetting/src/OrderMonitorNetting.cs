using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using fxcore2;

namespace OrdersMonitor
{
    /// <summary>
    /// Helper class for monitoring creation open positions using a open order.
    /// On no dealing desk more than one position can be create. It is depends on
    /// liquidity on forex market, The class stores all open positions 
    /// </summary>
    internal class OrderMonitorNetting
    {
        static public bool IsOpeningOrder(O2GOrderRow order)
        {
            return order.Type.StartsWith("O");
        }

        static public bool IsClosingOrder(O2GOrderRow order)
        {
            return order.Type.StartsWith("C");
        }


        private enum OrderState
        {
            OrderExecuting,
            OrderExecuted,
            OrderCanceled,
            OrderRejected
        }
        private volatile OrderState mState;

        private List<O2GTradeRow> mTrades;
        private List<O2GTradeRow> mUpdatedTrades;
        private List<O2GClosedTradeRow> mClosedTrades;
        private volatile int mTotalAmount;
        private volatile int mRejectAmount;
        private O2GOrderRow mOrder;
        private string mRejectMessage;
        private int mInitialAmount;

        public enum ExecutionResult
        {
            Executing,
            Executed,
            PartialRejected,
            FullyRejected,
            Canceled
        };


        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="order">Order for monitoring of execution</param>
        public OrderMonitorNetting(O2GOrderRow order) : this(order, 0)
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="order">Order for monitoring of execution</param>
        /// <param name="iNetPositionAmount">Initial amount of trade</param>
        public OrderMonitorNetting(O2GOrderRow order, int iNetPositionAmount)
        {
            mOrder = order;
            mTrades = new List<O2GTradeRow>();
            mUpdatedTrades = new List<O2GTradeRow>();
            mClosedTrades = new List<O2GClosedTradeRow>();
            mState = OrderState.OrderExecuting;
            mResult = ExecutionResult.Executing;
            mInitialAmount = iNetPositionAmount;
            mTotalAmount = 0;
            mRejectAmount = 0;
            mRejectMessage = "";
        }

        /// <summary>
        /// Process trade adding during order execution
        /// </summary>
        public void OnTradeAdded(O2GTradeRow tradeRow)
        {
            string sTradeOrderID = tradeRow.OpenOrderID;
            string sOrderID = mOrder.OrderID;
            if (sTradeOrderID.Equals(sOrderID))
            {
                mTrades.Add(tradeRow);

                if (mState == OrderState.OrderExecuted ||
                    mState == OrderState.OrderRejected ||
                    mState == OrderState.OrderCanceled)
                {
                    if (IsAllTradesReceived())
                        SetResult(true);
                }
            }
        }

        /// <summary>
        /// Process order data changing during execution
        /// </summary>
        public void OnOrderChanged(O2GOrderRow orderRow)
        {
            //STUB
        }

        /// <summary>
        /// Process order deletion as result of execution
        /// </summary>
        public void OnOrderDeleted(O2GOrderRow orderRow)
        {
            string sDeletedOrderID = orderRow.OrderID;
            string sOrderID = mOrder.OrderID;
            if (sDeletedOrderID.Equals(sOrderID))
            {
                // Store Reject amount
                if (OrderRowStatus.Rejected.Equals(orderRow.Status))
                {
                    mState = OrderState.OrderRejected;
                    mRejectAmount = orderRow.Amount;
                    mTotalAmount = orderRow.OriginAmount - mRejectAmount;
                    if (!string.IsNullOrEmpty(mRejectMessage) && IsAllTradesReceived())
                        SetResult(true);
                }
                else if (OrderRowStatus.Canceled.Equals(orderRow.Status))
                {
                    mState = OrderState.OrderCanceled;
                    mRejectAmount = orderRow.Amount;
                    mTotalAmount = orderRow.OriginAmount - mRejectAmount;
                    if (IsAllTradesReceived())
                        SetResult(false);
                }
                else
                {
                    mRejectAmount = 0;
                    mTotalAmount = orderRow.OriginAmount;
                    mState = OrderState.OrderExecuted;
                    if (IsAllTradesReceived())
                        SetResult(true);
                }
            }
        }

        /// <summary>
        /// Process reject message as result of order execution
        /// </summary>
        public void OnMessageAdded(O2GMessageRow messageRow)
        {
            if (mState == OrderState.OrderRejected ||
                mState == OrderState.OrderExecuting)
            {
                bool IsRejectMessage = CheckAndStoreMessage(messageRow);
                if (mState == OrderState.OrderRejected && IsRejectMessage)
                    SetResult(true);
            }
        }

        /// <summary>
        /// Process trade updating during order execution
        /// </summary>
        public void OnTradeUpdated(O2GTradeRow tradeRow)
        {
            string sTradeOrderID = tradeRow.OpenOrderID;
            string sOrderID = mOrder.OrderID;
            if (sTradeOrderID.Equals(sOrderID))
            {
                mUpdatedTrades.Add(tradeRow);
                if (mState == OrderState.OrderExecuted ||
                        mState == OrderState.OrderRejected ||
                        mState == OrderState.OrderCanceled)
                {
                    if (IsAllTradesReceived())
                    {
                        SetResult(true);
                    }
                }
            }
        }

        /// <summary>
        /// Process trade closing during order execution
        /// </summary>
        public void OnClosedTradeAdded(O2GClosedTradeRow closedTradeRow)
        {
            string sOrderID = mOrder.OrderID;
            string sClosedTradeOrderID = closedTradeRow.CloseOrderID;
            if (sClosedTradeOrderID.Equals(sOrderID))
            {
                mClosedTrades.Add(closedTradeRow);

                if (mState == OrderState.OrderExecuted ||
                    mState == OrderState.OrderRejected ||
                    mState == OrderState.OrderCanceled)
                {
                    if (IsAllTradesReceived())
                        SetResult(true);
                }
            }
        }

        /// <summary>
        /// Event about order execution is completed and all affected trades as opened/closed, all reject/cancel processed
        /// </summary>
        public event EventHandler OrderCompleted;

        /// <summary>
        /// Result of Order execution
        /// </summary>
        public ExecutionResult Result
        {
            get
            {
                return mResult;
            }
        }
        private volatile ExecutionResult mResult;

        /// <summary>
        /// Order execution is completed (with any result)
        /// </summary>
        public bool IsOrderCompleted
        {
            get
            {
                return (mResult != ExecutionResult.Executing);
            }
        }

        /// <summary>
        /// Monitored order
        /// </summary>
        public O2GOrderRow Order
        {
            get
            {
                return mOrder;
            }
        }

        /// <summary>
        /// List of Trades which were opened as effects of order execution
        /// </summary>
        public ReadOnlyCollection<O2GTradeRow> Trades
        {
            get
            {
                return mTrades.AsReadOnly();
            }
        }

        /// <summary>
        /// List of Trades which were updated as effects of order execution
        /// </summary>
        public ReadOnlyCollection<O2GTradeRow> UpdatedTrades
        {
            get
            {
                return mUpdatedTrades.AsReadOnly();
            }
        }

        /// <summary>
        /// List of Trades which were closed as effects of order execution
        /// </summary>
        public ReadOnlyCollection<O2GClosedTradeRow> ClosedTrades
        {
            get
            {
                return mClosedTrades.AsReadOnly();
            }
        }

        /// <summary>
        /// Amount of rejected part of order
        /// </summary>
        public int RejectAmount
        {
            get
            {
                return mRejectAmount;
            }
        }

        /// <summary>
        /// Info message with a reason of reject
        /// </summary>
        public string RejectMessage
        {
            get
            {
                return mRejectMessage;
            }
        }


        private void SetResult(bool bSuccess)
        {
            if (bSuccess)
            {
                if (mRejectAmount == 0)
                    mResult = ExecutionResult.Executed;
                else
                    mResult = (mTrades.Count == 0 && mClosedTrades.Count == 0) ?  ExecutionResult.FullyRejected : ExecutionResult.PartialRejected;
            }
            else
                mResult = ExecutionResult.Canceled;

            if (OrderCompleted != null)
                OrderCompleted(this, EventArgs.Empty);

        }

        private bool IsAllTradesReceived()
        {
            if (mState == OrderState.OrderExecuting)
                return false;
            int iCurrentTotalAmount = 0;
            for (int i = 0; i < mTrades.Count; i++)
            {
                iCurrentTotalAmount += mTrades[i].Amount;
            }
            for (int i = 0; i < mUpdatedTrades.Count; i++)
            {
                iCurrentTotalAmount += mUpdatedTrades[i].Amount;
            }
            for (int i = 0; i < mClosedTrades.Count; i++)
            {
                iCurrentTotalAmount += mClosedTrades[i].Amount;
            }
            return Math.Abs(iCurrentTotalAmount - mInitialAmount) == mTotalAmount;
        }

        private bool CheckAndStoreMessage(O2GMessageRow message)
        {
            string sFeature = message.Feature;
            if (MessageFeature.MarketCondition.Equals(sFeature))
            {
                string sText = message.Text;
                int findPos = sText.IndexOf(mOrder.OrderID);
                if (findPos > -1)
                {
                    mRejectMessage = sText;
                    return true;
                }
            }
            return false;
        }

    }

    internal class OrderRowStatus
    {
        public static string Rejected = "R";
        public static string Canceled = "C";
        public static string Executed = "F";
        //...
    }

    internal class MessageFeature
    {
        public static String MarketCondition = "5";
        //...
    }

}

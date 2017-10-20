using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using fxcore2;

namespace PartialFill
{
    class ResponseListener : IO2GResponseListener
    {
        private O2GSession mSession;
        private string mRequestID;
        private string mOrderID;
        private O2GResponse mResponse;
        private EventWaitHandle mSyncResponseEvent;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener(O2GSession session)
        {
            mSession = session;  
            mRequestID = string.Empty;
            mOrderID = string.Empty;
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);          
        }

        public void SetRequestID(string sRequestID)
        {
            mResponse = null;
            mOrderID = string.Empty;
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

        public void onTablesUpdates(O2GResponse response)
        {
            O2GResponseReaderFactory factory = mSession.getResponseReaderFactory();
            if (factory != null)
            {
                O2GTablesUpdatesReader reader = factory.createTablesUpdatesReader(response);
                for (int ii = 0; ii < reader.Count; ii++)
                {
                    switch (reader.getUpdateTable(ii))
                    {
                        case O2GTableType.Accounts:
                            O2GAccountRow account = reader.getAccountRow(ii);
                            // Show balance updates
                            Console.WriteLine("Balance: {0}", account.Balance);
                            break;
                        case O2GTableType.Orders:
                            O2GOrderRow order = reader.getOrderRow(ii);
                            switch (reader.getUpdateType(ii))
                            {
                                case O2GTableUpdateType.Insert:
                                    if(mRequestID.Equals(order.RequestID))
                                    {
                                        mOrderID = order.OrderID;
                                        printOrder("New order is added", order);
                                    }
                                    break;
                                case O2GTableUpdateType.Update:
                                    printOrder("An order is changed", order);
                                    break;
                                case O2GTableUpdateType.Delete:
                                    if (mRequestID.Equals(order.RequestID))
                                    {
                                        string sStatus = order.Status;
                                        if (sStatus.Equals("R"))
                                        {
                                            printOrder("An order has been rejected", order);
                                        }
                                        else
                                        {
                                            printOrder("An order is going to be removed", order);
                                        }
                                        mSyncResponseEvent.Set();
                                    }
                                    break;
                            }
                            break;
                        case O2GTableType.Trades:
                            O2GTradeRow trade = reader.getTradeRow(ii);
                            if (reader.getUpdateType(ii) == O2GTableUpdateType.Insert)
                            {
                                Console.WriteLine("Position is opened: TradeID='{0}', TradeIDOrigin='{1}'",
                                trade.TradeID, trade.TradeIDOrigin);
                            }
                            break;
                        case O2GTableType.ClosedTrades:
                            O2GClosedTradeRow closedTrade = reader.getClosedTradeRow(ii);
                            if (reader.getUpdateType(ii) == O2GTableUpdateType.Insert)
                            {
                                Console.WriteLine("Position is closed: TradeID='{0}'",
                                closedTrade.TradeID);
                            }
                            break;
                        case O2GTableType.Messages:
                            O2GMessageRow message = reader.getMessageRow(ii);
                            if (reader.getUpdateType(ii) == O2GTableUpdateType.Insert)
                            {
                                string text = message.Text;
                                int findPos = text.IndexOf(mOrderID);
                                if (findPos >= 0)
                                {
                                    Console.WriteLine("Feature='{0}', Message='{1}'",
                                            message.Feature, text);
                                }
                            }
                            break;
                    }
                }
            }
        }

        #endregion

        private void printOrder(string sCaption, O2GOrderRow orderRow)
        {
            Console.WriteLine("{0}: OrderID='{1}', TradeID='{2}', Status='{3}', " +
                    "Amount='{4}', OriginAmount='{5}', FilledAmount='{6}'",
                    sCaption, orderRow.OrderID, orderRow.TradeID, orderRow.Status,
                    orderRow.Amount, orderRow.OriginAmount, orderRow.FilledAmount);
        }
    }
}


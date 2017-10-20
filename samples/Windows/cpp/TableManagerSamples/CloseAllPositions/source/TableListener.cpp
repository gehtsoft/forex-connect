#include "stdafx.h"
#include <algorithm>

#include "BatchOrderMonitor.h"
#include "OrderMonitor.h"
#include "ResponseListener.h"
#include "TableListener.h"

TableListener::TableListener(ResponseListener *responseListener)
{
    mRefCount = 1;
    mBatchOrderMonitor = NULL;
    mResponseListener = responseListener;
    mResponseListener->addRef();
    std::cout.precision(2);
}

TableListener::~TableListener(void)
{
    if (mResponseListener)
        mResponseListener->release();
    if (mBatchOrderMonitor)
        delete mBatchOrderMonitor;
}

long TableListener::addRef()
{
    return InterlockedIncrement(&mRefCount);
}

long TableListener::release()
{
    InterlockedDecrement(&mRefCount);
    if(mRefCount == 0)
        delete this;
    return mRefCount;
}

/** Set request. */
void TableListener::setRequestIDs(std::vector<std::string> &requestIDs)
{
    mRequestIDs.resize(requestIDs.size());
    std::copy(requestIDs.begin(), requestIDs.end(), mRequestIDs.begin());
    mBatchOrderMonitor = new BatchOrderMonitor();
    mBatchOrderMonitor->setRequestIDs(mRequestIDs);
}

void TableListener::onAdded(const char *rowID, IO2GRow *row)
{
    O2GTable type = row->getTableType();
    switch(type)
    {
    case Orders:
    {
        IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
        if(mBatchOrderMonitor)
        {
            std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                    << "Rate='" << order->getRate() << "', "
                    << "TimeInForce='" << order->getTimeInForce() << "'"
                    << std::endl;
            mBatchOrderMonitor->onOrderAdded(order);
        }
    }
    break;
    case Trades:
    {
        IO2GTradeRow *trade = static_cast<IO2GTradeRow *>(row);
        if (mBatchOrderMonitor)
        {
            mBatchOrderMonitor->onTradeAdded(trade);
            if (mBatchOrderMonitor->isBatchExecuted())
            {
                printResult();
                mResponseListener->stopWaiting();
            }
        }
    }
    break;
    case ClosedTrades:
    {
        IO2GClosedTradeRow *closedTrade = static_cast<IO2GClosedTradeRow *>(row);
        if (mBatchOrderMonitor)
        {
            mBatchOrderMonitor->onClosedTradeAdded(closedTrade);
            if (mBatchOrderMonitor->isBatchExecuted())
            {
                printResult();
                mResponseListener->stopWaiting();
            }
        }
    }
    break;
    case Messages:
    {
        IO2GMessageRow *message = static_cast<IO2GMessageRow *>(row);
        if (mBatchOrderMonitor)
        {
            mBatchOrderMonitor->onMessageAdded(message);
            if (mBatchOrderMonitor->isBatchExecuted())
            {
                printResult();
                mResponseListener->stopWaiting();
            }
        }
    }
    break;
    }
}

void TableListener::onChanged(const char *rowID, IO2GRow *row)
{
    O2GTable type = row->getTableType();
    if (type == Accounts)
    {
        IO2GAccountTableRow *account = (IO2GAccountTableRow *)row;
        std::cout << "The balance has been changed. AccountID=" << account->getAccountID() << ", "
                << "Balance=" << std::fixed << account->getBalance() << ", "
                << "Equity=" << std::fixed << account->getEquity()
                << std::endl;
    }
}

void TableListener::onDeleted(const char *rowID, IO2GRow *row)
{
    if (row->getTableType() == Orders)
    {
        IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
        if (std::find(mRequestIDs.begin(), mRequestIDs.end(), order->getRequestID()) != mRequestIDs.end())
        {
            if (mBatchOrderMonitor)
            {
                std::cout << "The order has been deleted. OrderID='" << order->getOrderID() << "'"
                        << std::endl;
                mBatchOrderMonitor->onOrderDeleted(order);
                if (mBatchOrderMonitor->isBatchExecuted())
                {
                    printResult();
                    mResponseListener->stopWaiting();
                }
            }
        }
    }
}

void TableListener::onStatusChanged(O2GTableStatus status)
{
}

void TableListener::printResult()
{
    if (mBatchOrderMonitor)
    {
        std::vector<OrderMonitor *> orderMonitors;
        mBatchOrderMonitor->getMonitors(orderMonitors);
        for (size_t i= 0; i < orderMonitors.size(); ++i)
            printOrderMonitor(orderMonitors[i]);
    }
}

void TableListener::printOrderMonitor(OrderMonitor *monitor)
{
    if (monitor)
    {
        OrderMonitor::ExecutionResult result = monitor->getResult();
        std::vector<IO2GTradeRow*> trades;
        std::vector<IO2GClosedTradeRow*> closedTrades;
        O2G2Ptr<IO2GOrderRow> order = monitor->getOrder();
        std::string orderID = order->getOrderID();
        monitor->getTrades(trades);
        monitor->getClosedTrades(closedTrades);

        switch (result)
        {
        case OrderMonitor::Canceled:
        {
            if (trades.size() > 0)
            {
                printTrades(trades, orderID);
                printClosedTrades(closedTrades, orderID);
                std::cout << "A part of the order has been canceled. "
                        << "Amount = " << monitor->getRejectAmount() << std::endl;
            }
            else
            {
                std::cout << "The order: OrderID = " << orderID << " has been canceled"  << std::endl;
                std::cout << "The cancel amount = " << monitor->getRejectAmount() << std::endl;
            }
        }
        break;
        case OrderMonitor::FullyRejected:
        {
            std::cout << "The order has been rejected. OrderID = " << orderID << std::endl;
            std::cout << "The rejected amount = " << monitor->getRejectAmount() << std::endl;;
            std::cout << "Rejection cause: " << monitor->getRejectMessage() << std::endl;
        }
        break;
        case OrderMonitor::PartialRejected:
        {
            printTrades(trades, orderID);
            printClosedTrades(closedTrades, orderID);
            std::cout << "A part of the order has been rejected. "
                    << "Amount = " << monitor->getRejectAmount() << std::endl;
            std::cout << "Rejection cause: " << monitor->getRejectMessage() << std::endl;
        }
        break;
        case OrderMonitor::Executed:
        {
            printTrades(trades, orderID);
            printClosedTrades(closedTrades, orderID);
        }
        break;
        }
    }
}

void TableListener::printTrades(std::vector<IO2GTradeRow*> &trades, std::string &sOrderID)
{
    if (trades.size() == 0)
        return;
    std::cout << "For the order: OrderID=" << sOrderID << " the following positions have been opened: " << std::endl;
    for (size_t i = 0; i < trades.size(); ++i)
    {
        O2G2Ptr<IO2GTradeRow> trade = trades[i];
        std::string tradeID = trade->getTradeID();
        int amount = trade->getAmount();
        double rate = trade->getOpenRate();
        std::cout << "Trade ID: " << tradeID << ", "
                << "Amount: " << amount << ", "
                << "Rate: " << rate << std::endl;
    }
}

void TableListener::printClosedTrades(std::vector<IO2GClosedTradeRow*> &closedTrades, std::string &sOrderID)
{
    if (closedTrades.size() == 0)
        return;
    std::cout << "For the order: OrderID=" << sOrderID << " the following positions have been closed: " << std::endl;
    for (size_t i = 0; i < closedTrades.size(); ++i)
    {
        IO2GClosedTradeRow *closedTrade = closedTrades[i];
        std::string tradeID = closedTrade->getTradeID();
        int amount = closedTrade->getAmount();
        double rate = closedTrade->getCloseRate();
        std::cout << "Closed Trade ID: " << tradeID << ", "
                << "Amount: " << amount << ", "
                << "Closed Rate: " << rate << std::endl;
        closedTrade->release();
    }
}

void TableListener::subscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GAccountsTable> accountsTable = (IO2GAccountsTable *)manager->getTable(Accounts);
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);
    O2G2Ptr<IO2GTradesTable> tradesTable = (IO2GTradesTable *)manager->getTable(Trades);
    O2G2Ptr<IO2GMessagesTable> messagesTable = (IO2GMessagesTable *)manager->getTable(Messages);
    O2G2Ptr<IO2GClosedTradesTable> closedTradesTable = (IO2GClosedTradesTable *)manager->getTable(ClosedTrades);

    accountsTable->subscribeUpdate(Update, this);
    ordersTable->subscribeUpdate(Insert, this);
    ordersTable->subscribeUpdate(Delete, this);
    tradesTable->subscribeUpdate(Insert, this);
    tradesTable->subscribeUpdate(Update, this);
    closedTradesTable->subscribeUpdate(Insert, this);
    messagesTable->subscribeUpdate(Insert, this);
}

void TableListener::unsubscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GAccountsTable> accountsTable = (IO2GAccountsTable *)manager->getTable(Accounts);
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);
    O2G2Ptr<IO2GTradesTable> tradesTable = (IO2GTradesTable *)manager->getTable(Trades);
    O2G2Ptr<IO2GMessagesTable> messagesTable = (IO2GMessagesTable *)manager->getTable(Messages);
    O2G2Ptr<IO2GClosedTradesTable> closedTradesTable = (IO2GClosedTradesTable *)manager->getTable(ClosedTrades);

    accountsTable->unsubscribeUpdate(Update, this);
    ordersTable->unsubscribeUpdate(Insert, this);
    ordersTable->unsubscribeUpdate(Delete, this);
    tradesTable->unsubscribeUpdate(Insert, this);
    tradesTable->unsubscribeUpdate(Update, this);
    closedTradesTable->unsubscribeUpdate(Insert, this);
    messagesTable->unsubscribeUpdate(Insert, this);
}

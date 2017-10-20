#include "stdafx.h"
#include <math.h>
#include <algorithm>

#include <sstream>
#include <iomanip>
#include "OrderMonitor.h"
#include "BatchOrderMonitor.h"
#include "ResponseListener.h"

ResponseListener::ResponseListener(IO2GSession *session)
{
    mSession = session;
    mSession->addRef();
    mRefCount = 1;
    mResponseEvent = CreateEvent(0, FALSE, FALSE, 0);
    mResponse = NULL;
    mBatchOrderMonitor = NULL;
    std::cout.precision(2);
}

ResponseListener::~ResponseListener()
{
    if (mResponse)
        mResponse->release();
    if (mBatchOrderMonitor)
        delete mBatchOrderMonitor;
    mSession->release();
    CloseHandle(mResponseEvent);
}

/** Increase reference counter. */
long ResponseListener::addRef()
{
    return InterlockedIncrement(&mRefCount);
}

/** Decrease reference counter. */
long ResponseListener::release()
{
    long rc = InterlockedDecrement(&mRefCount);
    if (rc == 0)
        delete this;
    return rc;
}

/** Set request. */
void ResponseListener::setRequestID(const char *sRequestID)
{
    mRequestID = sRequestID;
    if (mResponse)
    {
        mResponse->release();
        mResponse = NULL;
    }
    ResetEvent(mResponseEvent);
}

void ResponseListener::setRequestIDs(std::vector<std::string> &requestIDs)
{
    mRequestIDs.resize(requestIDs.size());
    std::copy(requestIDs.begin(), requestIDs.end(), mRequestIDs.begin());
    if (mResponse)
    {
        mResponse->release();
        mResponse = NULL;
    }
    mBatchOrderMonitor = new BatchOrderMonitor();
    mBatchOrderMonitor->setRequestIDs(mRequestIDs);
    ResetEvent(mResponseEvent);
}

bool ResponseListener::waitEvents()
{
    return WaitForSingleObject(mResponseEvent, _TIMEOUT) == 0;
}

/** Gets response.*/
IO2GResponse *ResponseListener::getResponse()
{
    if (mResponse)
        mResponse->addRef();
    return mResponse;
}

/** Request execution completed data handler. */
void ResponseListener::onRequestCompleted(const char *requestId, IO2GResponse *response)
{
    if (response && (mRequestID == requestId ||
            std::find(mRequestIDs.begin(), mRequestIDs.end(), requestId) != mRequestIDs.end()))
    {
        mResponse = response;
        mResponse->addRef();
        if (response->getType() != CreateOrderResponse)
            SetEvent(mResponseEvent);
    }
}

/** Request execution failed data handler. */
void ResponseListener::onRequestFailed(const char *requestId , const char *error)
{
    if (mRequestID == requestId ||
        std::find(mRequestIDs.begin(), mRequestIDs.end(), requestId) != mRequestIDs.end())
    {
        std::cout << "The request has been failed. ID: " << requestId << " : " << error << std::endl;
        SetEvent(mResponseEvent);
    }
}

/** Request update data received data handler. */
void ResponseListener::onTablesUpdates(IO2GResponse *data)
{
    if (data)
    {
        O2G2Ptr<IO2GResponseReaderFactory> factory = mSession->getResponseReaderFactory();
        if (factory)
        {
            O2G2Ptr<IO2GTablesUpdatesReader> reader = factory->createTablesUpdatesReader(data);
            if (reader)
            {
                for (int i = 0; i < reader->size(); ++i)
                {
                    switch (reader->getUpdateTable(i))
                    {
                    case Orders:
                    {
                        O2G2Ptr<IO2GOrderRow> order = reader->getOrderRow(i);
                        if (reader->getUpdateType(i) == Insert)
                        {
                            if (mBatchOrderMonitor)
                            {
                                std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                                        << "Rate='" << order->getRate() << "', "
                                        << "TimeInForce='" << order->getTimeInForce() << "'"
                                        << std::endl;
                                mBatchOrderMonitor->onOrderAdded(order);
                            }
                        }
                        else if (reader->getUpdateType(i) == Delete)
                        {
                            if (mBatchOrderMonitor)
                            {
                                std::cout << "The order has been deleted. OrderID='" << order->getOrderID() << "'"
                                        << std::endl;
                                mBatchOrderMonitor->onOrderDeleted(order);
                                if (mBatchOrderMonitor->isBatchExecuted())
                                {
                                    printResult();
                                    SetEvent(mResponseEvent);
                                }
                            }
                        }
                    }
                    break;
                    case Trades:
                    {
                        if (reader->getUpdateType(i) == Insert)
                        {
                            O2G2Ptr<IO2GTradeRow> trade = reader->getTradeRow(i);
                            if (mBatchOrderMonitor)
                            {
                                mBatchOrderMonitor->onTradeAdded(trade);
                                if (mBatchOrderMonitor->isBatchExecuted())
                                {
                                    printResult();
                                    SetEvent(mResponseEvent);
                                }
                            }
                        }
                    }
                    break;
                    case ClosedTrades:
                    {
                        if (reader->getUpdateType(i) == Insert)
                        {
                            O2G2Ptr<IO2GClosedTradeRow> closedTrade = reader->getClosedTradeRow(i);
                            if (mBatchOrderMonitor)
                            {
                                mBatchOrderMonitor->onClosedTradeAdded(closedTrade);
                                if (mBatchOrderMonitor->isBatchExecuted())
                                {
                                    printResult();
                                    SetEvent(mResponseEvent);
                                }
                            }
                        }
                    }
                    break;
                    case Messages:
                    {
                        if (reader->getUpdateType(i) == Insert)
                        {
                            O2G2Ptr<IO2GMessageRow> message = reader->getMessageRow(i);
                            if (mBatchOrderMonitor)
                            {
                                mBatchOrderMonitor->onMessageAdded(message);
                                if (mBatchOrderMonitor->isBatchExecuted())
                                {
                                    printResult();
                                    SetEvent(mResponseEvent);
                                }
                            }
                        }
                    }
                    break;
                    case Accounts:
                    {
                        if (reader->getUpdateType(i) == Update && reader->getUpdateTable(i) == Accounts)
                        {
                            O2G2Ptr<IO2GAccountRow> account = reader->getAccountRow(i);
                            std::cout << "The balance has been changed. AccountID=" << account->getAccountID() << ", "
                                    << "Balance=" << std::fixed << account->getBalance() << std::endl;
                        }
                    }
                    break;
                    }
                }
            }
        }
    }
}

void ResponseListener::printResult()
{
    if (mBatchOrderMonitor)
    {
        std::vector<OrderMonitor *> orderMonitors;
        mBatchOrderMonitor->getMonitors(orderMonitors);
        for (size_t i= 0; i < orderMonitors.size(); ++i)
            printOrderMonitor(orderMonitors[i]);
    }
}

void ResponseListener::printOrderMonitor(OrderMonitor *monitor)
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

void ResponseListener::printTrades(std::vector<IO2GTradeRow*> &trades, std::string &sOrderID)
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

void ResponseListener::printClosedTrades(std::vector<IO2GClosedTradeRow*> &closedTrades, std::string &sOrderID)
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


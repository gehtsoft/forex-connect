#include "stdafx.h"
#include <math.h>

#include <sstream>
#include <iomanip>
#include "OrderMonitor.h"
#include "ResponseListener.h"

ResponseListener::ResponseListener(IO2GSession *session)
{
    mSession = session;
    mSession->addRef();
    mRefCount = 1;
    mResponseEvent = CreateEvent(0, FALSE, FALSE, 0);
    mResponse = NULL;
    mOrderMonitor = NULL;
    std::cout.precision(2);
}

ResponseListener::~ResponseListener()
{
    if (mResponse)
        mResponse->release();
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
    if (response && mRequestID == requestId)
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
    if (mRequestID == requestId)
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
                        if (mRequestID != order->getRequestID())
                            break;
                        if (reader->getUpdateType(i) == Insert)
                        {
                            if ((isClosingOrder(order) || isOpeningOrder(order)) &&
                                mOrderMonitor == NULL)
                            {
                                std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                                        << "Rate='" << order->getRate() << "', "
                                        << "TimeInForce='" << order->getTimeInForce() << "'"
                                        << std::endl;
                                mOrderMonitor = new OrderMonitor(order);
                            }
                        }
                        else if (reader->getUpdateType(i) == Delete)
                        {
                            if (mOrderMonitor)
                            {
                                std::cout << "The order has been deleted. OrderID='" << order->getOrderID() << "'"
                                        << std::endl;
                                mOrderMonitor->onOrderDeleted(order);
                                if (mOrderMonitor->isOrderCompleted())
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
                            if (mOrderMonitor)
                            {
                                mOrderMonitor->onTradeAdded(trade);
                                if (mOrderMonitor->isOrderCompleted())
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
                            if (mOrderMonitor)
                            {
                                mOrderMonitor->onClosedTradeAdded(closedTrade);
                                if (mOrderMonitor->isOrderCompleted())
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
                            if (mOrderMonitor)
                            {
                                mOrderMonitor->onMessageAdded(message);
                                if (mOrderMonitor->isOrderCompleted())
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
    if (mOrderMonitor)
    {
        OrderMonitor::ExecutionResult result = mOrderMonitor->getResult();
        std::vector<IO2GTradeRow*> trades;
        std::vector<IO2GClosedTradeRow*> closedTrades;
        O2G2Ptr<IO2GOrderRow> order = mOrderMonitor->getOrder();
        std::string orderID = order->getOrderID();
        mOrderMonitor->getTrades(trades);
        mOrderMonitor->getClosedTrades(closedTrades);

        switch (result)
        {
        case OrderMonitor::Canceled:
        {
            if (trades.size() > 0)
            {
                printTrades(trades, orderID);
                printClosedTrades(closedTrades, orderID);
                std::cout << "A part of the order has been canceled. "
                        << "Amount = " << mOrderMonitor->getRejectAmount() << std::endl;
            }
            else
            {
                std::cout << "The order: OrderID = " << orderID << " has been canceled"  << std::endl;
                std::cout << "The cancel amount = " << mOrderMonitor->getRejectAmount() << std::endl;
            }
        }
        break;
        case OrderMonitor::FullyRejected:
        {
            std::cout << "The order has been rejected. OrderID = " << orderID << std::endl;
            std::cout << "The rejected amount = " << mOrderMonitor->getRejectAmount() << std::endl;;
            std::cout << "Rejection cause: " << mOrderMonitor->getRejectMessage() << std::endl;
        }
        break;
        case OrderMonitor::PartialRejected:
        {
            printTrades(trades, orderID);
            printClosedTrades(closedTrades, orderID);
            std::cout << "A part of the order has been rejected. "
                    << "Amount = " << mOrderMonitor->getRejectAmount() << std::endl;
            std::cout << "Rejection cause: " << mOrderMonitor->getRejectMessage() << std::endl;
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


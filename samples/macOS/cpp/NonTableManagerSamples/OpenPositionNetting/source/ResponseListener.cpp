#include "stdafx.h"
#include <math.h>

#include <sstream>
#include <iomanip>
#include "OrderMonitorNetting.h"
#include "ResponseListener.h"

ResponseListener::ResponseListener(IO2GSession *session)
{
    mSession = session;
    mSession->addRef();
    mRefCount = 1;
    mResponseEvent = CreateEvent(0, FALSE, FALSE, 0);
    mRequestID = "";
    mResponse = NULL;
    mTradesTable = NULL;
    mOrderMonitorNetting = NULL;
    std::cout.precision(2);
}

ResponseListener::~ResponseListener()
{
    if (mResponse)
        mResponse->release();
    if (mTradesTable)
        mTradesTable->release();
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

/** Store initial trades state. */
void ResponseListener::setTradesTable(IO2GTradesTableResponseReader *tradesTable)
{
    mTradesTable = tradesTable;
    mTradesTable->addRef();
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
                        switch (reader->getUpdateType(i))
                        {
                        case Insert:
                            if ((isClosingOrder(order) || isOpeningOrder(order)) &&
                                mOrderMonitorNetting == NULL)
                            {
                                std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                                        << "Rate='" << order->getRate() << "', "
                                        << "TimeInForce='" << order->getTimeInForce() << "'"
                                        << std::endl;
                                O2G2Ptr<IO2GTradeRow> trade = NULL;
                                std::string sTradeID = std::string(order->getTradeID());
                                if (mTradesTable)
                                {
                                    for (int j = 0; j < mTradesTable->size(); ++j)
                                    {
                                        if (sTradeID == mTradesTable->getRow(j)->getTradeID())
                                        {
                                            trade = mTradesTable->getRow(j);
                                            break;
                                        }
                                    }
                                }
                                if (trade)
                                    mOrderMonitorNetting = new OrderMonitorNetting(order, trade->getAmount());
                                else
                                    mOrderMonitorNetting = new OrderMonitorNetting(order, 0);
                            }
                            break;
                        case Delete:
                            if (mOrderMonitorNetting)
                            {
                                std::cout << "The order has been deleted. OrderID='" << order->getOrderID() << "'"
                                        << std::endl;
                                mOrderMonitorNetting->onOrderDeleted(order);
                                if (mOrderMonitorNetting->isOrderCompleted())
                                {
                                    printResult();
                                    SetEvent(mResponseEvent);
                                }
                            }
                            break;
                        }
                    }
                    break;
                    case Trades:
                    {
                        O2G2Ptr<IO2GTradeRow> trade = reader->getTradeRow(i);
                        switch (reader->getUpdateType(i))
                        {
                        case Insert:
                        {
                            if (mOrderMonitorNetting)
                            {
                                mOrderMonitorNetting->onTradeAdded(trade);
                                if (mOrderMonitorNetting->isOrderCompleted())
                                {
                                    printResult();
                                    SetEvent(mResponseEvent);
                                }
                            }
                        }
                        break;
                        case Update:
                        {
                            if (mOrderMonitorNetting)
                            {
                                mOrderMonitorNetting->onTradeUpdated(trade);
                                if (mOrderMonitorNetting->isOrderCompleted())
                                {
                                    printResult();
                                    SetEvent(mResponseEvent);
                                }
                            }
                        }
                        break;
                        }
                    }
                    break;
                    case ClosedTrades:
                    {
                        if (reader->getUpdateType(i) == Insert)
                        {
                            O2G2Ptr<IO2GClosedTradeRow> closedTrade = reader->getClosedTradeRow(i);
                            if (mOrderMonitorNetting)
                            {
                                mOrderMonitorNetting->onClosedTradeAdded(closedTrade);
                                if (mOrderMonitorNetting->isOrderCompleted())
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
                            if (mOrderMonitorNetting)
                            {
                                mOrderMonitorNetting->onMessageAdded(message);
                                if (mOrderMonitorNetting->isOrderCompleted())
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
    if (mOrderMonitorNetting)
    {
        OrderMonitorNetting::ExecutionResult result = mOrderMonitorNetting->getResult();
        std::vector<IO2GTradeRow*> trades;
        std::vector<IO2GTradeRow*> updatedTrades;
        std::vector<IO2GClosedTradeRow*> closedTrades;
        O2G2Ptr<IO2GOrderRow> order = mOrderMonitorNetting->getOrder();
        std::string orderID = order->getOrderID();
        mOrderMonitorNetting->getTrades(trades);
        mOrderMonitorNetting->getUpdatedTrades(updatedTrades);
        mOrderMonitorNetting->getClosedTrades(closedTrades);

        switch (result)
        {
        case OrderMonitorNetting::Canceled:
        {
            if (trades.size() > 0)
            {
                printTrades(trades, orderID);
                printUpdatedTrades(updatedTrades, orderID);
                printClosedTrades(closedTrades, orderID);
                std::cout << "A part of the order has been canceled. "
                        << "Amount = " << mOrderMonitorNetting->getRejectAmount() << std::endl;
            }
            else
            {
                std::cout << "The order: OrderID=" << orderID << " has been canceled"  << std::endl;
                std::cout << "The cancel amount = " << mOrderMonitorNetting->getRejectAmount() << std::endl;
            }
        }
        break;
        case OrderMonitorNetting::FullyRejected:
        {
            std::cout << "The order has been rejected. OrderID = " << orderID << std::endl;
            std::cout << "The rejected amount = " << mOrderMonitorNetting->getRejectAmount() << std::endl;;
            std::cout << "Rejection cause: " << mOrderMonitorNetting->getRejectMessage() << std::endl;
        }
        break;
        case OrderMonitorNetting::PartialRejected:
        {
            printTrades(trades, orderID);
            printUpdatedTrades(updatedTrades, orderID);
            printClosedTrades(closedTrades, orderID);
            std::cout << "A part of the order has been rejected. "
                    << "Amount = " << mOrderMonitorNetting->getRejectAmount() << std::endl;
            std::cout << "Rejection cause: " << mOrderMonitorNetting->getRejectMessage() << std::endl;
        }
        break;
        case OrderMonitorNetting::Executed:
        {
            printTrades(trades, orderID);
            printUpdatedTrades(updatedTrades, orderID);
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

void ResponseListener::printUpdatedTrades(std::vector<IO2GTradeRow*> &updatedTrades, std::string &sOrderID)
{
    if (updatedTrades.size() == 0)
        return;
    std::cout << "For the order: OrderID=" << sOrderID << " the following positions have been updated: " << std::endl;
    for (size_t i = 0; i < updatedTrades.size(); ++i)
    {
        O2G2Ptr<IO2GTradeRow> trade = updatedTrades[i];
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


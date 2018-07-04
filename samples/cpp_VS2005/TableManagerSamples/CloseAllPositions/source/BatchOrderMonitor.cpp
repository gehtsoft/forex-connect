#include "stdafx.h"
#include "OrderMonitor.h"
#include "BatchOrderMonitor.h"
#include <algorithm>

BatchOrderMonitor::BatchOrderMonitor()
{
}

BatchOrderMonitor::~BatchOrderMonitor()
{
    for(size_t i=0; i < mMonitors.size(); ++i)
    {
        delete mMonitors[i];
    }
    mMonitors.clear();
}

void BatchOrderMonitor::setRequestIDs(std::vector<std::string> &requestIDs)
{
    mRequestIDs.resize(requestIDs.size());
    std::copy(requestIDs.begin(), requestIDs.end(), mRequestIDs.begin());
}

void BatchOrderMonitor::onRequestCompleted(const char *requestID, IO2GResponse *response)
{
}

void BatchOrderMonitor::removeRequestID(const std::string &sRequestID)
{
    std::vector<std::string>::iterator iter = std::find(mRequestIDs.begin(), mRequestIDs.end(), sRequestID);
    if (iter != mRequestIDs.end())
        mRequestIDs.erase(iter);
}

void BatchOrderMonitor::onRequestFailed(const char *requestId , const char *error)
{
    if (isOwnRequest(requestId))
        removeRequestID(requestId);
}

void BatchOrderMonitor::onTradeAdded(IO2GTradeRow *trade)
{
    for (size_t i = 0; i < mMonitors.size(); ++i)
        mMonitors[i]->onTradeAdded(trade);
}

void BatchOrderMonitor::onOrderAdded(IO2GOrderRow *order)
{
    std::string orderID = order->getOrderID();
    std::cout << "Order Added " << orderID << std::endl;
    std::string requestID = order->getRequestID();
    if (isOwnRequest(requestID))
    {
        if (isOpeningOrder(order) || isClosingOrder(order))
        {
            addToMonitoring(order);
        }
    }
}

void BatchOrderMonitor::onOrderDeleted(IO2GOrderRow *order)
{
    for (size_t i = 0; i < mMonitors.size(); ++i)
        mMonitors[i]->onOrderDeleted(order);
}

void BatchOrderMonitor::onMessageAdded(IO2GMessageRow *message)
{
    for (size_t i = 0; i < mMonitors.size(); ++i)
        mMonitors[i]->onMessageAdded(message);
}

void BatchOrderMonitor::onClosedTradeAdded(IO2GClosedTradeRow *closedTrade)
{
    for (size_t i = 0; i < mMonitors.size(); ++i)
        mMonitors[i]->onClosedTradeAdded(closedTrade);
}

bool BatchOrderMonitor::isBatchExecuted()
{
    bool allCompleted = true;
    for (size_t i = 0; i < mMonitors.size(); ++i)
    {
        if (mMonitors[i]->isOrderCompleted())
        {
            removeRequestID(mMonitors[i]->getOrder()->getRequestID());
        }
        else
        {
            allCompleted = false;
        }
    }
    bool result = mRequestIDs.size() == 0 && allCompleted;
    return  result;
}

bool BatchOrderMonitor::isOwnRequest(const std::string &sRequestID)
{
    return std::find(mRequestIDs.begin(), mRequestIDs.end(), sRequestID) != mRequestIDs.end();
}

void BatchOrderMonitor::addToMonitoring(IO2GOrderRow *order)
{
    OrderMonitor *monitor = new OrderMonitor(order);
    mMonitors.push_back(monitor);
}

void BatchOrderMonitor::getMonitors(std::vector<OrderMonitor*> &monitors)
{
    monitors.clear();
    monitors.resize(mMonitors.size());
    std::copy(mMonitors.begin(), mMonitors.end(), monitors.begin());
}


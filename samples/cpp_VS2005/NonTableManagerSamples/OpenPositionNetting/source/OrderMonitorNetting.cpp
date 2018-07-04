#include "stdafx.h"
#include <vector>
#include <cmath>
#include "OrderMonitorNetting.h"

#define MarketCondition "5"
bool isOpeningOrder(IO2GOrderRow *order)
{
    const char* type = order->getType();
    return type[0] == 'O';
}
bool isClosingOrder(IO2GOrderRow *order)
{
    const char* type = order->getType();
    return type[0] == 'C';
}

OrderMonitorNetting::OrderMonitorNetting(IO2GOrderRow *order, int iNetPositionAmount)
{
    mOrder = order;
    mOrder->addRef();
    mInitialAmount = iNetPositionAmount;
    mTotalAmount = 0;
    mRejectAmount = 0;
    mRejectMessage = "";
    mState = OrderExecuting;
    mResult = Executing;
}

OrderMonitorNetting::~OrderMonitorNetting()
{
    for (size_t i = 0; i < mTrades.size(); ++i)
        mTrades[i]->release();
    for (size_t i = 0; i < mUpdatedTrades.size(); ++i)
        mUpdatedTrades[i]->release();
    for (size_t i = 0; i < mClosedTrades.size(); ++i)
        mClosedTrades[i]->release();
    mOrder->release();
}

OrderMonitorNetting::ExecutionResult OrderMonitorNetting::getResult()
{
    return mResult;
}

void OrderMonitorNetting::onTradeAdded(IO2GTradeRow *trade)
{
    std::string tradeOrderID = trade->getOpenOrderID();
    std::string orderID = mOrder->getOrderID();
    if (tradeOrderID == orderID)
    {
        trade->addRef();
        mTrades.push_back(trade);

        if (mState == OrderExecuted ||
            mState == OrderRejected ||
            mState == OrderCanceled)
        {
            if (isAllTradesReceived())
                setResult(true);
        }
    }
}

void OrderMonitorNetting::onTradeUpdated(IO2GTradeRow *updatedTrade)
{
    std::string tradeOrderID = updatedTrade->getOpenOrderID();
    std::string orderID = mOrder->getOrderID();
    if (tradeOrderID == orderID)
    {
        updatedTrade->addRef();
        mUpdatedTrades.push_back(updatedTrade);

        if (mState == OrderExecuted ||
            mState == OrderRejected ||
            mState == OrderCanceled)
        {
            if (isAllTradesReceived())
                setResult(true);
        }
    }
}

void OrderMonitorNetting::onClosedTradeAdded(IO2GClosedTradeRow *closedTrade)
{
    std::string orderID = mOrder->getOrderID();
    std::string closedTradeOrderID = closedTrade->getCloseOrderID();
    if (orderID == closedTradeOrderID)
    {
        closedTrade->addRef();
        mClosedTrades.push_back(closedTrade);

        if (mState == OrderExecuted ||
            mState == OrderRejected ||
            mState == OrderCanceled)
        {
            if (isAllTradesReceived())
                setResult(true);
        }
    }
}

void OrderMonitorNetting::onOrderDeleted(IO2GOrderRow *order)
{
    std::string deletedOrderID = order->getOrderID();
    std::string orderID = mOrder->getOrderID();

    if (deletedOrderID == orderID)
    {
        // Store Reject amount
        if (*(order->getStatus()) == 'R')
        {
            mState = OrderRejected;
            mRejectAmount = order->getAmount();
            mTotalAmount = order->getOriginAmount() - mRejectAmount;

            if (!mRejectMessage.empty() && isAllTradesReceived())
                setResult(true);
        }
        else if (*(order->getStatus()) == 'C')
        {
            mState = OrderCanceled;
            mRejectAmount = order->getAmount();
            mTotalAmount = order->getOriginAmount() - mRejectAmount;
            if (isAllTradesReceived())
                setResult(false);
        }
        else
        {
            mRejectAmount = 0;
            mTotalAmount = order->getOriginAmount();
            mState = OrderExecuted;
            if (isAllTradesReceived())
                setResult(true);
        }
    }
}

void OrderMonitorNetting::onMessageAdded(IO2GMessageRow *message)
{
    if (mState == OrderRejected ||
        mState == OrderExecuting)
    {
        bool isRejectMessage = checkAndStoreMessage(message);
        if (mState == OrderRejected && isRejectMessage)
            setResult(true);
    }
}

bool OrderMonitorNetting::checkAndStoreMessage(IO2GMessageRow *message)
{
    std::string feature;
    feature = message->getFeature();
    if (feature == MarketCondition)
    {
        std::string text = message->getText();
        size_t findPos = text.find(mOrder->getOrderID());
        if (findPos != std::string::npos)
        {
            mRejectMessage = message->getText();
            return true;
        }
    }
    return false;
}

IO2GOrderRow *OrderMonitorNetting::getOrder()
{
    mOrder->addRef();
    return mOrder;
}

void OrderMonitorNetting::getTrades(std::vector<IO2GTradeRow*> &trades)
{
    trades.clear();
    trades.resize(mTrades.size());
    std::copy(mTrades.begin(), mTrades.end(), trades.begin());
    for (size_t i = 0; i < mTrades.size(); ++i)
        mTrades[i]->addRef();
}

void OrderMonitorNetting::getUpdatedTrades(std::vector<IO2GTradeRow*> &updatedTrades)
{
    updatedTrades.clear();
    updatedTrades.resize(mUpdatedTrades.size());
    std::copy(mUpdatedTrades.begin(), mUpdatedTrades.end(), updatedTrades.begin());
    for (size_t i = 0; i < mUpdatedTrades.size(); ++i)
        mUpdatedTrades[i]->addRef();
}

void OrderMonitorNetting::getClosedTrades(std::vector<IO2GClosedTradeRow*> &closedTrades)
{
    closedTrades.clear();
    closedTrades.resize(mClosedTrades.size());
    std::copy(mClosedTrades.begin(), mClosedTrades.end(), closedTrades.begin());
    for (size_t i = 0; i < mClosedTrades.size(); ++i)
        mClosedTrades[i]->addRef();
}

int OrderMonitorNetting::getRejectAmount()
{
    return mRejectAmount;
}

std::string OrderMonitorNetting::getRejectMessage()
{
    return mRejectMessage;
}

bool OrderMonitorNetting::isAllTradesReceived()
{
    if (mState == OrderExecuting)
        return false;
    int currentTotalAmount = 0;
    for (size_t i = 0; i < mTrades.size(); ++i)
        currentTotalAmount += mTrades[i]->getAmount();

    for (size_t i = 0; i < mUpdatedTrades.size(); ++i)
        currentTotalAmount += mUpdatedTrades[i]->getAmount();

    for (size_t i = 0; i < mClosedTrades.size(); ++i)
        currentTotalAmount += mClosedTrades[i]->getAmount();

    return abs(currentTotalAmount - mInitialAmount) == mTotalAmount;
}

void OrderMonitorNetting::setResult(bool bSuccess)
{
    if (bSuccess)
    {
        if (mRejectAmount == 0)
            mResult = Executed;
        else
            mResult = (mTrades.size() == 0 && mClosedTrades.size()==0) ? FullyRejected : PartialRejected;
    }
    else
        mResult = Canceled;
}

bool OrderMonitorNetting::isOrderCompleted()
{
    return mResult !=  Executing;
}

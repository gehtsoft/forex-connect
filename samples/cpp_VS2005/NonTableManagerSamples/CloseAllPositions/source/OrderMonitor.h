#pragma once

bool isOpeningOrder(IO2GOrderRow *order);
bool isClosingOrder(IO2GOrderRow *order);
/** Helper class for monitoring creation open positions using a open order.
    On no dealing desk more than one position can be create. It is depends on
    liquidity on forex market, The class stores all open positions 
*/
class OrderMonitor
{
 public:
    enum ExecutionResult
    {
        Executing,
        Executed,
        PartialRejected,
        FullyRejected,
        Canceled
    };

    OrderMonitor(IO2GOrderRow *order);
    ~OrderMonitor();
    void onTradeAdded(IO2GTradeRow *trade);
    void onOrderDeleted(IO2GOrderRow *order);
    void onMessageAdded(IO2GMessageRow *message);
    void onClosedTradeAdded(IO2GClosedTradeRow *closedTrade);

    ExecutionResult getResult();
    bool isOrderCompleted();
    IO2GOrderRow *getOrder();
    void getTrades(std::vector<IO2GTradeRow*> &trades);
    void getClosedTrades(std::vector<IO2GClosedTradeRow*> &closedTrades);
    int getRejectAmount();
    std::string getRejectMessage();
 private:
    enum OrderState
    {
        OrderExecuting,
        OrderExecuted,
        OrderCanceled,
        OrderRejected
    };
    /** Set result.*/
    void setResult(bool bSuccess);
    bool isAllTradesReceived();
    bool checkAndStoreMessage(IO2GMessageRow *message);
    OrderState mState;
    std::string mRejectMessage;
    std::vector<IO2GTradeRow*> mTrades;
    std::vector<IO2GClosedTradeRow*> mClosedTrades;
    int mTotalAmount;
    int mRejectAmount;
    IO2GOrderRow *mOrder;
    ExecutionResult mResult;
};


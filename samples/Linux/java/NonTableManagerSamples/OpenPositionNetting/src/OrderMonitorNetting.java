package common;

import java.util.ArrayList;
import java.util.List;

import com.fxcore2.*;

public class OrderMonitorNetting {

    public enum ExecutionResult {
        Executing, Executed, PartialRejected, FullyRejected, Canceled
    }

    private enum OrderState {
        OrderExecuting, OrderExecuted, OrderCanceled, OrderRejected
    }

    private final String MarketCondition = "5";

    private O2GOrderRow mOrder;
    private List<O2GTradeRow> mTrades;
    private List<O2GTradeRow> mUpdatedTrades;
    private List<O2GClosedTradeRow> mClosedTrades;
    private OrderState mState;
    private ExecutionResult mResult;
    private int mTotalAmount;
    private int mRejectAmount;
    private String mRejectMessage;
    private int mInitialAmount;

    public OrderMonitorNetting(O2GOrderRow order) {
        this(order, 0);
    }
    
    public OrderMonitorNetting(O2GOrderRow order, int iNetPositionAmount) {
        mOrder = order;
        mTrades = new ArrayList<O2GTradeRow>();
        mUpdatedTrades = new ArrayList<O2GTradeRow>();
        mClosedTrades = new ArrayList<O2GClosedTradeRow>();
        mState = OrderState.OrderExecuting;
        mResult = ExecutionResult.Executing;
        mTotalAmount = 0;
        mRejectAmount = 0;
        mRejectMessage = "";
        mInitialAmount = iNetPositionAmount;
    }

    public static boolean isOpeningOrder(O2GOrderRow order) {
        return order.getType().startsWith("O");
    }

    public static boolean isClosingOrder(O2GOrderRow order) {
        return order.getType().startsWith("C");
    }

    // Process trade adding during order execution
    public void onTradeAdded(O2GTradeRow trade) {
        String tradeOrderID = trade.getOpenOrderID();
        String orderID = mOrder.getOrderID();
        if (tradeOrderID.equals(orderID)) {
            mTrades.add(trade);
            if (mState == OrderState.OrderExecuted ||
                    mState == OrderState.OrderRejected ||
                    mState == OrderState.OrderCanceled) {
                if (isAllTradesReceived()) {
                    setResult(true);
                }
            }
        }
    }
    
    // Process trade updating during order execution
    public void onTradeUpdated(O2GTradeRow tradeRow) {
        String sTradeOrderID = tradeRow.getOpenOrderID();
        String sOrderID = mOrder.getOrderID();
        if (sTradeOrderID.equals(sOrderID)) {
            mUpdatedTrades.add(tradeRow);
            if (mState == OrderState.OrderExecuted ||
                    mState == OrderState.OrderRejected ||
                    mState == OrderState.OrderCanceled) {
                if (isAllTradesReceived()) {
                    setResult(true);
                }
            }
        }
    }

    // Process trade closing during order execution
    public void onClosedTradeAdded(O2GClosedTradeRow closedTrade) {
        String orderID = mOrder.getOrderID();
        String closedTradeOrderID = closedTrade.getCloseOrderID();
        if (orderID.equals(closedTradeOrderID)) {
            mClosedTrades.add(closedTrade);
            if (mState == OrderState.OrderExecuted ||
                    mState == OrderState.OrderRejected ||
                    mState == OrderState.OrderCanceled) {
                if (isAllTradesReceived()) {
                    setResult(true);
                }
            }
        }
    }

    // Process order deletion as result of execution
    public void onOrderDeleted(O2GOrderRow order) {
        String deletedOrderID = order.getOrderID();
        String orderID = mOrder.getOrderID();
        if (deletedOrderID.equals(orderID)) {
            // Store Reject amount
            if (order.getStatus().startsWith("R")) {
                mState = OrderState.OrderRejected;
                mRejectAmount = order.getAmount();
                mTotalAmount = order.getOriginAmount() - mRejectAmount;
                if (!mRejectMessage.isEmpty() && isAllTradesReceived()) {
                    setResult(true);
                }
            } else if (order.getStatus().startsWith("C")) {
                mState = OrderState.OrderCanceled;
                mRejectAmount = order.getAmount();
                mTotalAmount = order.getOriginAmount() - mRejectAmount;
                if (isAllTradesReceived()) {
                    setResult(false);
                }
            } else {
                mRejectAmount = 0;
                mTotalAmount = order.getOriginAmount();
                mState = OrderState.OrderExecuted;
                if (isAllTradesReceived()) {
                    setResult(true);
                }
            }
        }
    }

    public void onMessageAdded(O2GMessageRow message) {
        if (mState == OrderState.OrderRejected
                || mState == OrderState.OrderExecuting) {
            boolean isRejectMessage = checkAndStoreMessage(message);
            if (mState == OrderState.OrderRejected && isRejectMessage) {
                setResult(true);
            }
        }
    }

    public O2GOrderRow getOrder() {
        return mOrder;
    }

    public List<O2GTradeRow> getTrades() {
        return mTrades;
    }
    
    public List<O2GTradeRow> getUpdatedTrades() {
        return mUpdatedTrades;
    }

    public List<O2GClosedTradeRow> getClosedTrades() {
        return mClosedTrades;
    }

    public int getRejectAmount() {
        return mRejectAmount;
    }

    public String getRejectMessage() {
        return mRejectMessage;
    }

    public ExecutionResult getResult() {
        return mResult;
    }

    public boolean isOrderCompleted() {
        return mResult != ExecutionResult.Executing;
    }

    private boolean checkAndStoreMessage(O2GMessageRow message) {
        String feature;
        feature = message.getFeature();
        if (feature.equals(MarketCondition)) {
            String text = message.getText();
            int findPos = text.indexOf(mOrder.getOrderID());
            if (findPos >= 0) {
                mRejectMessage = message.getText();
                return true;
            }
        }
        return false;
    }

    private boolean isAllTradesReceived() {
        if (mState == OrderState.OrderExecuting)
            return false;
        int iCurrentTotalAmount = 0;
        for (int i = 0; i < mTrades.size(); i++)
        {
            iCurrentTotalAmount += mTrades.get(i).getAmount();
        }
        for (int i = 0; i < mUpdatedTrades.size(); i++)
        {
            iCurrentTotalAmount += mUpdatedTrades.get(i).getAmount();
        }
        for (int i = 0; i < mClosedTrades.size(); i++)
        {
            iCurrentTotalAmount += mClosedTrades.get(i).getAmount();
        }
        return Math.abs(iCurrentTotalAmount - mInitialAmount) == mTotalAmount;
    }

    private void setResult(boolean bSuccess) {
        if (bSuccess) {
            if (mRejectAmount == 0) {
                mResult = ExecutionResult.Executed;
            } else {
                mResult = (mTrades.size() == 0 && mClosedTrades.size() == 0) ? ExecutionResult.FullyRejected
                        : ExecutionResult.PartialRejected;
            }
        } else {
            mResult = ExecutionResult.Canceled;
        }
    }
}

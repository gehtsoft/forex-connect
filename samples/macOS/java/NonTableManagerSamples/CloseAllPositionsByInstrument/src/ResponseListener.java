package closeallpositionsbyinstrument;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;
import common.BatchOrderMonitor;
import common.OrderMonitor;

public class ResponseListener implements IO2GResponseListener {

    private O2GSession mSession;
    private String mRequestID;
    private List<String> mRequestIDs;
    private O2GResponse mResponse;
    private Semaphore mSemaphore;
    private BatchOrderMonitor mBatchOrderMonitor;

    // ctor
    public ResponseListener(O2GSession session) {
        mSession = session;
        mRequestID = "";
        mRequestIDs = new ArrayList<String>();
        mResponse = null;
        mSemaphore = new Semaphore(0);
        mBatchOrderMonitor = null;
    }

    public void setRequestID(String sRequestID) {
        mResponse = null;
        mRequestID = sRequestID;
    }

    public void setRequestIDs(List<String> requestIDs) {
        mRequestIDs.clear();
        for (String sRequestID : requestIDs) {
            mRequestIDs.add(sRequestID);
        }
        mBatchOrderMonitor = new BatchOrderMonitor();
        mBatchOrderMonitor.setRequestIDs(requestIDs);
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    public O2GResponse getResponse() {
        return mResponse;
    }

    public void onRequestCompleted(String sRequestID, O2GResponse response) {
        if (mRequestID.equals(response.getRequestId())) {
            mResponse = response;
            if (response.getType() != O2GResponseType.CREATE_ORDER_RESPONSE) {
                mSemaphore.release();
            }
        }
        // real order execution is processed by Order monitor
    }

    public void onRequestFailed(String sRequestID, String sError) {
        System.out.println("Request failed: " + sError);
        if (mRequestID.equals(sRequestID) || mRequestIDs.contains(sRequestID)) {
            if (mBatchOrderMonitor != null) {
                mBatchOrderMonitor.onRequestFailed(sRequestID);
                if (mBatchOrderMonitor.isBatchExecuted()) {
                    printResult();
                }
            }
            mSemaphore.release();
        }
    }

    public void onTablesUpdates(O2GResponse response) {
        O2GResponseReaderFactory factory = mSession.getResponseReaderFactory();
        if (factory != null) {
            O2GTablesUpdatesReader reader = factory.createTablesUpdatesReader(response);
            for (int ii = 0; ii < reader.size(); ii++) {
                switch (reader.getUpdateTable(ii)) {
                case ACCOUNTS:
                    O2GAccountRow account = reader.getAccountRow(ii);
                    //Show balance updates
                    System.out.println(String.format("Balance: %.2f", account.getBalance()));
                    break;
                case ORDERS:
                    O2GOrderRow order = reader.getOrderRow(ii);
                    switch (reader.getUpdateType(ii)) {
                    case INSERT:
                        if (mBatchOrderMonitor != null) {
                            System.out.println(String.format("The order has been added. Order ID: %s, Rate: %s, Time In Force: %s",
                                    order.getOrderID(),
                                    order.getRate(),
                                    order.getTimeInForce()));
                            mBatchOrderMonitor.onOrderAdded(order);
                        }
                        break;
                    case DELETE:
                        if (mBatchOrderMonitor != null) {
                            System.out.println(String.format("The order has been deleted. Order ID: %s",
                                    order.getOrderID()));
                            mBatchOrderMonitor.onOrderDeleted(order);
                            if (mBatchOrderMonitor.isBatchExecuted()) {
                                printResult();
                                mSemaphore.release();
                            }
                        }
                        break;
                    }
                    break;
                case TRADES:
                    O2GTradeRow trade = reader.getTradeRow(ii);
                    if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                        if (mBatchOrderMonitor != null) {
                            mBatchOrderMonitor.onTradeAdded(trade);
                            if (mBatchOrderMonitor.isBatchExecuted()) {
                                printResult();
                                mSemaphore.release();
                            }
                        }
                    }
                    break;
                case CLOSED_TRADES:
                    O2GClosedTradeRow closedTrade = reader.getClosedTradeRow(ii);
                    if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                        if (mBatchOrderMonitor != null) {
                            mBatchOrderMonitor.onClosedTradeAdded(closedTrade);
                            if (mBatchOrderMonitor.isBatchExecuted()) {
                                printResult();
                                mSemaphore.release();
                            }
                        }
                    }
                    break;
                case MESSAGES:
                    O2GMessageRow message = reader.getMessageRow(ii);
                    if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                        if (mBatchOrderMonitor != null) {
                            mBatchOrderMonitor.onMessageAdded(message);
                            if (mBatchOrderMonitor.isBatchExecuted()) {
                                printResult();
                                mSemaphore.release();
                            }
                        }
                    }
                    break;
                }
            }
        }
    }

    private void printResult() {
        for (OrderMonitor monitor : mBatchOrderMonitor.getMonitors()) {
            System.out.println(String.format("Result for OrderID %s:", monitor.getOrder().getOrderID()));
            printMonitorResult(monitor);
        }
    }

    private void printMonitorResult(OrderMonitor orderMonitor) {
        if (orderMonitor != null) {
            OrderMonitor.ExecutionResult result = orderMonitor.getResult();
            List<O2GTradeRow> trades;
            List<O2GClosedTradeRow> closedTrades;
            O2GOrderRow order = orderMonitor.getOrder();
            String sOrderID = order.getOrderID();
            trades = orderMonitor.getTrades();
            closedTrades = orderMonitor.getClosedTrades();

            switch (result) {
            case Canceled:
                if (trades.size() > 0) {
                    printTrades(trades, sOrderID);
                    printClosedTrades(closedTrades, sOrderID);
                    System.out.println(String.format("A part of the order has been canceled. Amount = %s",
                            orderMonitor.getRejectAmount()));
                } else {
                    System.out.println(String.format(
                            "The order: OrderID = %s has been canceled",
                            sOrderID));
                    System.out.println(String.format(
                            "The cancel amount = %s",
                            orderMonitor.getRejectAmount()));
                }
                break;
            case FullyRejected:
                System.out.println(String.format(
                        "The order has been rejected. OrderID = %s",
                        sOrderID));
                System.out.println(String.format("The rejected amount = %s",
                        orderMonitor.getRejectAmount()));
                System.out.println(String.format("Rejection cause: %s",
                        orderMonitor.getRejectMessage()));
                break;
            case PartialRejected:
                printTrades(trades, sOrderID);
                printClosedTrades(closedTrades, sOrderID);
                System.out.println(String.format(
                        "A part of the order has been rejected. Amount = %s",
                        orderMonitor.getRejectAmount()));
                System.out.println(String.format("Rejection cause: %s",
                        orderMonitor.getRejectMessage()));
                break;
            case Executed:
                printTrades(trades, sOrderID);
                printClosedTrades(closedTrades, sOrderID);
                break;
            }
        }
    }

    private void printTrades(List<O2GTradeRow> trades, String sOrderID) {
        if (trades.size() == 0)
            return;
        System.out.println(String.format("For the order: OrderID = %s the following positions have been opened:",
                sOrderID));

        for (int i = 0; i < trades.size(); i++) {
            O2GTradeRow trade = trades.get(i);
            String sTradeID = trade.getTradeID();
            int iAmount = trade.getAmount();
            double dRate = trade.getOpenRate();
            System.out.println(String.format(
                    "Trade ID: %s; Amount: %s; Rate: %s",
                    sTradeID, iAmount, dRate));
        }
    }

    private void printClosedTrades(List<O2GClosedTradeRow> closedTrades, String sOrderID) {
        if (closedTrades.size() == 0)
            return;
        System.out.println(String.format("For the order: OrderID = %s the following positions have been closed: ",
                sOrderID));

        for (int i = 0; i < closedTrades.size(); i++) {
            O2GClosedTradeRow closedTrade = closedTrades.get(i);
            String sTradeID = closedTrade.getTradeID();
            int iAmount = closedTrade.getAmount();
            double dRate = closedTrade.getCloseRate();
            System.out.println(String.format(
                    "Closed Trade ID: %s; Amount: %s; Closed Rate: %s",
                    sTradeID, iAmount, dRate));
        }
    }
}

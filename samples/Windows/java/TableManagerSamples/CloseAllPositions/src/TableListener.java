package closeallpositions;

import java.util.ArrayList;
import java.util.List;

import com.fxcore2.*;
import common.BatchOrderMonitor;
import common.OrderMonitor;

public class TableListener implements IO2GTableListener {
    private ResponseListener mResponseListener;
    private BatchOrderMonitor mBatchOrderMonitor;
    private List<String> mRequestIDs;

    // ctor
    public TableListener(ResponseListener responseListener) {
        mResponseListener = responseListener;
        mBatchOrderMonitor = null;
        mRequestIDs = new ArrayList<String>();
    }

    public void setRequestIDs(List<String> requestIDs) {
        mRequestIDs.clear();
        for (String sRequestID : requestIDs) {
            mRequestIDs.add(sRequestID);
        }
        mBatchOrderMonitor = new BatchOrderMonitor();
        mBatchOrderMonitor.setRequestIDs(mRequestIDs);
    }

    public void onAdded(String sRowID, O2GRow rowData) {
        O2GTableType type = rowData.getTableType();
        switch (type) {
        case ORDERS:
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            if (mBatchOrderMonitor != null && mRequestIDs.contains(orderRow.getRequestID())) {
                System.out.println(String.format("The order has been added. Order ID: %s, Rate: %s, Time In Force: %s",
                        orderRow.getOrderID(),
                        orderRow.getRate(),
                        orderRow.getTimeInForce()));
                mBatchOrderMonitor.onOrderAdded(orderRow);
            }
            break;
        case TRADES:
            O2GTradeRow tradeRow = (O2GTradeRow)rowData;
            if (mBatchOrderMonitor != null) {
                mBatchOrderMonitor.onTradeAdded(tradeRow);
                if (mBatchOrderMonitor.isBatchExecuted()) {
                    printResult();
                    mResponseListener.stopWaiting();
                }
            }
            break;
        case CLOSED_TRADES:
            O2GClosedTradeRow closedTradeRow = (O2GClosedTradeRow)rowData;
            if (mBatchOrderMonitor != null) {
                mBatchOrderMonitor.onClosedTradeAdded(closedTradeRow);
                if (mBatchOrderMonitor.isBatchExecuted()) {
                    printResult();
                    mResponseListener.stopWaiting();
                }
            }
            break;
        case MESSAGES:
            O2GMessageRow messageRow = (O2GMessageRow)rowData;
            if (mBatchOrderMonitor != null) {
                mBatchOrderMonitor.onMessageAdded(messageRow);
                if (mBatchOrderMonitor.isBatchExecuted()) {
                    printResult();
                    mResponseListener.stopWaiting();
                }
            }
            break;
        }
    }

    public void onChanged(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.ACCOUNTS) {
            O2GAccountTableRow account = (O2GAccountTableRow)rowData;
            System.out.println(String.format("Balance: %.2f, Equity: %.2f", account.getBalance(), account.getEquity()));
        }
    }

    public void onDeleted(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.ORDERS) {
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            if (mRequestIDs.contains(orderRow.getRequestID())) {
                if (mBatchOrderMonitor != null) {
                    System.out.println(String.format("The order has been deleted. Order ID: %s", orderRow.getOrderID()));
                    mBatchOrderMonitor.onOrderDeleted(orderRow);
                    if (mBatchOrderMonitor.isBatchExecuted()) {
                        printResult();
                        mResponseListener.stopWaiting();
                    }
                }
            }
        }
    }

    public void onStatusChanged(O2GTableStatus status) {
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
                            "The order: OrderID = %s  has been canceled.",
                            sOrderID));
                    System.out.println(String.format(
                            "The cancel amount = %s.",
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
                System.out.println(String.format("Rejection cause: %s ",
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
        System.out.println(String.format("For the order: OrderID = %s the following positions have been opened:", sOrderID));

        for (int i = 0; i < trades.size(); i++) {
            O2GTradeRow trade = trades.get(i);
            String sTradeID = trade.getTradeID();
            int iAmount = trade.getAmount();
            double dRate = trade.getOpenRate();
            System.out.println(String.format("Trade ID: %s; Amount: %s; Rate: %s", sTradeID, iAmount, dRate));
        }
    }

    private void printClosedTrades(List<O2GClosedTradeRow> closedTrades, String sOrderID) {
        if (closedTrades.size() == 0)
            return;
        System.out.println(String.format("For the order: OrderID = %s the following positions have been closed: ", sOrderID));

        for (int i = 0; i < closedTrades.size(); i++) {
            O2GClosedTradeRow closedTrade = closedTrades.get(i);
            String sTradeID = closedTrade.getTradeID();
            int iAmount = closedTrade.getAmount();
            double dRate = closedTrade.getCloseRate();
            System.out.println(String.format("Closed Trade ID: %s; Amount: %s; Closed Rate: %s", sTradeID, iAmount, dRate));
        }
    }

    public void subscribeEvents(O2GTableManager manager) {
        O2GAccountsTable accountsTable = (O2GAccountsTable)manager.getTable(O2GTableType.ACCOUNTS);
        O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.ORDERS);
        O2GTradesTable tradesTable = (O2GTradesTable)manager.getTable(O2GTableType.TRADES);
        O2GMessagesTable messagesTable = (O2GMessagesTable)manager.getTable(O2GTableType.MESSAGES);
        O2GClosedTradesTable closedTradesTable = (O2GClosedTradesTable)manager.getTable(O2GTableType.CLOSED_TRADES);
        accountsTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        ordersTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        ordersTable.subscribeUpdate(O2GTableUpdateType.DELETE, this);
        tradesTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        tradesTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        closedTradesTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        messagesTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
    }

    public void unsubscribeEvents(O2GTableManager manager) {
        O2GAccountsTable accountsTable = (O2GAccountsTable)manager.getTable(O2GTableType.ACCOUNTS);
        O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.ORDERS);
        O2GTradesTable tradesTable = (O2GTradesTable)manager.getTable(O2GTableType.TRADES);
        O2GMessagesTable messagesTable = (O2GMessagesTable)manager.getTable(O2GTableType.MESSAGES);
        O2GClosedTradesTable closedTradesTable = (O2GClosedTradesTable)manager.getTable(O2GTableType.CLOSED_TRADES);
        accountsTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.DELETE, this);
        tradesTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        tradesTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        closedTradesTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        messagesTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
    }
}

package partialfill;

import com.fxcore2.*;

public class TableListener implements IO2GTableListener {
    private ResponseListener mResponseListener;
    private String mRequestID;
    private String mOrderID;

    // ctor
    public TableListener(ResponseListener responseListener) {
        mResponseListener = responseListener;
        mRequestID = "";
        mOrderID = "";
    }

    public void setRequestID(String sRequestID) {
        mRequestID = sRequestID;
        mOrderID = "";
    }

    public void onAdded(String sRowID, O2GRow rowData) {
        O2GTableType type = rowData.getTableType();
        switch (type) {
        case ORDERS:
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            if (mRequestID.equals(orderRow.getRequestID())) {
                mOrderID = orderRow.getOrderID();
                printOrder("New order is added", orderRow);
            }
            break;
        case TRADES:
            O2GTradeRow tradeRow = (O2GTradeRow)rowData;
            System.out.println(String.format("Position is opened: TradeID='%s', TradeIDOrigin='%s'",
                    tradeRow.getTradeID(), tradeRow.getTradeIDOrigin()));
            break;
        case CLOSED_TRADES:
            O2GClosedTradeRow closedTradeRow = (O2GClosedTradeRow)rowData;
            System.out.println(String.format("Position is closed: TradeID='%s'",
                    closedTradeRow.getTradeID()));
            break;
        case MESSAGES:
            O2GMessageRow messageRow = (O2GMessageRow)rowData;
            String text = messageRow.getText();
            int findPos = text.indexOf(mOrderID);
            if (findPos >= 0) {
                System.out.println(String.format("Feature='%s', Message='%s'",
                        messageRow.getFeature(), text));
            }
            break;
        }
    }

    public void onChanged(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.ACCOUNTS) {
            O2GAccountTableRow account = (O2GAccountTableRow)rowData;
            System.out.println(String.format("Balance: %.2f, Equity: %.2f", account.getBalance(), account.getEquity()));
        }
        if (rowData.getTableType() == O2GTableType.ORDERS) {
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            printOrder("An order is changed", orderRow);
        }
    }

    public void onDeleted(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.ORDERS) {
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            if (mRequestID.equals(orderRow.getRequestID())) {
                String sStatus = orderRow.getStatus();
                if (sStatus.equals("R")) {
                    printOrder("An order has been rejected", orderRow);
                } else {
                    printOrder("An order is going to be removed", orderRow);
                }
                mResponseListener.stopWaiting();
            }
        }
    }

    public void onStatusChanged(O2GTableStatus status) {
    }

    private void printOrder(String sCaption, O2GOrderRow orderRow) {
        System.out.println(String.format("%s: OrderID='%s', TradeID='%s', Status='%s', " +
                "Amount='%s', OriginAmount='%s', FilledAmount='%s'",
                sCaption, orderRow.getOrderID(), orderRow.getTradeID(), orderRow.getStatus(),
                orderRow.getAmount(), orderRow.getOriginAmount(), orderRow.getFilledAmount()));
    }

    public void subscribeEvents(O2GTableManager manager) {
        O2GAccountsTable accountsTable = (O2GAccountsTable)manager.getTable(O2GTableType.ACCOUNTS);
        O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.ORDERS);
        O2GTradesTable tradesTable = (O2GTradesTable)manager.getTable(O2GTableType.TRADES);
        O2GMessagesTable messagesTable = (O2GMessagesTable)manager.getTable(O2GTableType.MESSAGES);
        O2GClosedTradesTable closedTradesTable = (O2GClosedTradesTable)manager.getTable(O2GTableType.CLOSED_TRADES);
        accountsTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        ordersTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        ordersTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
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
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.DELETE, this);
        tradesTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        tradesTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        closedTradesTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        messagesTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
    }
}

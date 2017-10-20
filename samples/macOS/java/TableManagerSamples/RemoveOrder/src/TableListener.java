package removeorder;

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
    }

    public String getOrderID() {
        return mOrderID;
    }

    // Implementation of IO2GTableListener interface public method onAdded
    public void onAdded(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.ORDERS) {
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            if (mRequestID.equals(orderRow.getRequestID())) {
                if (isLimitEntryOrder(orderRow) && mOrderID.isEmpty()) {
                    mOrderID = orderRow.getOrderID();
                    System.out.println(String.format("The order has been added. OrderID=%s, Type=%s, BuySell=%s, Rate=%s, TimeInForce=%s",
                            orderRow.getOrderID(), orderRow.getType(), orderRow.getBuySell(), orderRow.getRate(), orderRow.getTimeInForce()));
                    mResponseListener.stopWaiting();
                }
            }
        }
    }

    // Implementation of IO2GTableListener interface public method onChanged
    public void onChanged(String sRowID, O2GRow rowData) {
    }

    // Implementation of IO2GTableListener interface public method onDeleted
    public void onDeleted(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.ORDERS) {
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            if (mRequestID.equals(orderRow.getRequestID())) {
                if (!mOrderID.isEmpty()) {
                    System.out.println(String.format("The order has been deleted. Order ID: %s", orderRow.getOrderID()));
                    mResponseListener.stopWaiting();
                }
            }
        }
    }

    public void onStatusChanged(O2GTableStatus status) {
    }

    public void subscribeEvents(O2GTableManager manager) {
        O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.ORDERS);
        ordersTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        ordersTable.subscribeUpdate(O2GTableUpdateType.DELETE, this);
    }

    public void unsubscribeEvents(O2GTableManager manager) {
        O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.ORDERS);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.DELETE, this);
    }

    private boolean isLimitEntryOrder(O2GOrderRow order)
    {
        return order.getType().startsWith("LE");
    }
}

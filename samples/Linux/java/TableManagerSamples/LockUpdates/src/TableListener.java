package lockupdates;

import com.fxcore2.*;

public class TableListener implements IO2GTableListener {
    private ResponseListener mResponseListener;
    private String mRequestID;

    // ctor
    public TableListener(ResponseListener responseListener) {
        mResponseListener = responseListener;
        mRequestID = "";
    }

    public void setRequestID(String sRequestID) {
        mRequestID = sRequestID;
    }

    // Implementation of IO2GTableListener interface public method onAdded
    public void onAdded(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.ORDERS) {
            O2GOrderRow orderRow = (O2GOrderRow)rowData;
            if (mRequestID.equals(orderRow.getRequestID())) {
                System.out.println(String.format("The order has been added. OrderID=%s, Type=%s, BuySell=%s, Rate=%s, TimeInForce=%s",
                        orderRow.getOrderID(), orderRow.getType(), orderRow.getBuySell(), orderRow.getRate(), orderRow.getTimeInForce()));
                mResponseListener.stopWaiting();
            }
        }
    }

    // Implementation of IO2GTableListener interface public method onChanged
    public void onChanged(String sRowID, O2GRow rowData) {
    }

    // Implementation of IO2GTableListener interface public method onDeleted
    public void onDeleted(String sRowID, O2GRow rowData) {
    }

    public void onStatusChanged(O2GTableStatus status) {
    }

    public void subscribeEvents(O2GTableManager manager) {
        O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.ORDERS);
        ordersTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        ordersTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        ordersTable.subscribeUpdate(O2GTableUpdateType.DELETE, this);
    }

    public void unsubscribeEvents(O2GTableManager manager) {
        O2GOrdersTable ordersTable = (O2GOrdersTable)manager.getTable(O2GTableType.ORDERS);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        ordersTable.unsubscribeUpdate(O2GTableUpdateType.DELETE, this);
    }
}

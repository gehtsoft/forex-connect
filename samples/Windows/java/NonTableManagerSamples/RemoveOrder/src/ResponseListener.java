package removeorder;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class ResponseListener implements IO2GResponseListener {

    private O2GSession mSession;
    private String mRequestID;
    private Semaphore mSemaphore;
    private String mOrderID;

    public ResponseListener(O2GSession session) {
        mSession = session;
        mRequestID = "";
        mSemaphore = new Semaphore(0);
        mOrderID = "";
    }

    public String getOrderID() {
        return mOrderID;
    }

    public void setRequestID(String sRequestID) {
        mRequestID = sRequestID;
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    // Implementation of IO2GResponseListener interface public method onRequestCompleted
    public void onRequestCompleted(String sRequestID, O2GResponse response) {
    }

    // Implementation of IO2GResponseListener interface public method onRequestFailed
    public void onRequestFailed(String sRequestID, String sError) {
        if (mRequestID.equals(sRequestID)) {
            System.out.println("Request failed: " + sError);
            mSemaphore.release();
        }
    }

    // Implementation of IO2GResponseListener interface public method onTablesUpdates
    public void onTablesUpdates(O2GResponse response) {
        O2GResponseReaderFactory factory = mSession.getResponseReaderFactory();
        if (factory != null) {
            O2GTablesUpdatesReader reader = factory.createTablesUpdatesReader(response);
            for (int i = 0; i < reader.size(); i++) {
                if (reader.getUpdateTable(i) == O2GTableType.ORDERS) {
                    O2GOrderRow orderRow = reader.getOrderRow(i);
                    if (mRequestID.equals(orderRow.getRequestID())) {
                        switch (reader.getUpdateType(i)) {
                        case INSERT:
                            if (isLimitEntryOrder(orderRow) && mOrderID.isEmpty()) {
                                mOrderID = orderRow.getOrderID();
                                System.out.println(String.format("The order has been added. OrderID=%s, Type=%s, BuySell=%s, Rate=%s, TimeInForce=%s",
                                        orderRow.getOrderID(), orderRow.getType(), orderRow.getBuySell(), orderRow.getRate(), orderRow.getTimeInForce()));
                                mSemaphore.release();
                            }
                            break;
                        case DELETE:
                            if (!mOrderID.isEmpty()) {
                                System.out.println("The order has been deleted. Order ID: " + orderRow.getOrderID());
                                mSemaphore.release();
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    private boolean isLimitEntryOrder(O2GOrderRow order) {
        return order.getType().startsWith("LE");
    }
}

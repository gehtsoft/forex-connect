package twoconnections;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class ResponseListener implements IO2GResponseListener {
    private O2GSession mSession;
    private String mRequestID;
    private O2GResponse mResponse;
    private String mOrderID;
    private Semaphore mSemaphore;

    // ctor
    public ResponseListener(O2GSession session) {
        mSession = session;
        mRequestID = "";
        mResponse = null;
        mOrderID = "";
        mSemaphore = new Semaphore(0);
    }

    public void setRequestID(String sRequestID) {
        mResponse = null;
        mRequestID = sRequestID;
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    public O2GResponse getResponse() {
        return mResponse;
    }

    public String getOrderID() {
        return mOrderID;
    }

    public void onRequestCompleted(String sRequestId, O2GResponse response) {
        if (mRequestID.equals(response.getRequestId())) {
            mResponse = response;
            mSemaphore.release();
        }
    }

    public void onRequestFailed(String sRequestID, String sError) {
        if (mRequestID.equals(sRequestID)) {
            System.out.println("Request failed: " + sError);
            mSemaphore.release();
        }
    }

    public void onTablesUpdates(O2GResponse response) {
        O2GResponseReaderFactory factory = mSession.getResponseReaderFactory();
        if (factory != null) {
            O2GTablesUpdatesReader reader = factory.createTablesUpdatesReader(response);
            for (int ii = 0; ii < reader.size(); ii++) {
                if (reader.getUpdateTable(ii) == O2GTableType.ORDERS) {
                    O2GOrderRow orderRow = reader.getOrderRow(ii);
                    if (mRequestID.equals(orderRow.getRequestID())) {
                        if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                            mOrderID = orderRow.getOrderID();
                            System.out.println(String.format("The order has been added. OrderID=%s, Type=%s, BuySell=%s, Rate=%s, TimeInForce=%s",
                                    orderRow.getOrderID(), orderRow.getType(), orderRow.getBuySell(), orderRow.getRate(), orderRow.getTimeInForce()));
                            mSemaphore.release();
                            break;
                        }
                    }
                }
            }
        }
    }
}

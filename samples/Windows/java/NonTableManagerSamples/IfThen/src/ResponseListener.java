package ifthen;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class ResponseListener implements IO2GResponseListener {
    private O2GSession mSession;
    private List<String> mRequestIDs;
    private O2GResponse mResponse;
    private Semaphore mSemaphore;

    // ctor
    public ResponseListener(O2GSession session) {
        mRequestIDs = new ArrayList<String>();
        mResponse = null;
        mSemaphore = new Semaphore(0);
        mSession = session;
    }

    public void setRequestIDs(List<String> requestIDs) {
        mResponse = null;
        mRequestIDs.clear();
        for (String sOrderID : requestIDs) {
            mRequestIDs.add(sOrderID);
        }
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    public O2GResponse getResponse() {
        return mResponse;
    }

    public void onRequestCompleted(String sRequestID, O2GResponse response) {
        if (mRequestIDs.contains(response.getRequestId())) {
            mResponse = response;
            if (response.getType() != O2GResponseType.CREATE_ORDER_RESPONSE) {
                mSemaphore.release();
            }
        }
    }

    public void onRequestFailed(String sRequestID, String sError) {
        if (mRequestIDs.contains(sRequestID)) {
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
                    if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                        if (mRequestIDs.contains(orderRow.getRequestID())) {
                            System.out.println(String.format("The order has been added. OrderID=%s, Type=%s, BuySell=%s, Rate=%s, TimeInForce=%s",
                                    orderRow.getOrderID(), orderRow.getType(), orderRow.getBuySell(), orderRow.getRate(), orderRow.getTimeInForce()));
                            mRequestIDs.remove(orderRow.getRequestID());
                        }
                        if (mRequestIDs.size() == 0) {
                            mSemaphore.release();
                        }
                    }
                }
            }
        }
    }
}

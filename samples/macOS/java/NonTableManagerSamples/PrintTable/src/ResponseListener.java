package printtable;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class ResponseListener implements IO2GResponseListener {
    private String mRequestID;
    private O2GResponse mResponse;
    private Semaphore mSemaphore;

    // ctor
    public ResponseListener() {
        mRequestID = "";
        mResponse = null;
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
    }
}

package lockupdates;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class ResponseListener implements IO2GResponseListener {
    private String mRequestID;
    private Semaphore mSemaphore;

    // ctor
    public ResponseListener() {
        mRequestID = "";
        mSemaphore = new Semaphore(0);
    }

    public void setRequestID(String sRequestID) {
        mRequestID = sRequestID;
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    public void stopWaiting() {
        mSemaphore.release();
    }

    // Implementation of IO2GResponseListener interface public method onRequestCompleted
    public void onRequestCompleted(String sRequestID, O2GResponse response) {
    }

    // Implementation of IO2GResponseListener interface public method onRequestFailed
    public void onRequestFailed(String sRequestID, String sError) {
        if (mRequestID.equals(sRequestID)) {
            System.out.println("Request failed: " + sError);
            stopWaiting();
        }
    }

    // Implementation of IO2GResponseListener interface public method onTablesUpdates
    public void onTablesUpdates(O2GResponse response) {
    }
}

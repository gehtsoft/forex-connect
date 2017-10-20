package printtablelistener;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class TableManagerListener implements IO2GTableManagerListener {
    O2GTableManagerStatus mLastStatus;
    private boolean mLoaded;
    private boolean mError;
    private Semaphore mSemaphore;

    public boolean isLoaded() {
        return mLoaded;
    }

    public boolean hasError() {
        return mError;
    }

    // ctor
    public TableManagerListener() {
        reset();
        mSemaphore = new Semaphore(0);
    }
    
    public void reset()
    {
        mLastStatus = O2GTableManagerStatus.TABLES_LOADING;
        mLoaded = false;
        mError = false;
    }

    public boolean waitEvents() throws Exception {
        if (mLastStatus == O2GTableManagerStatus.TABLES_LOADING)
            return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
        return true;
    }

    @Override
    public void onStatusChanged(O2GTableManagerStatus status, O2GTableManager manager) {
        mLastStatus = status;
        switch (status) {
        case TABLES_LOADED:
            mLoaded = true;
            mError = false;
            mSemaphore.release();
            break;
        case TABLES_LOAD_FAILED:
            mLoaded = false;
            mError = true;
            mSemaphore.release();
            break;
        }
    }
}

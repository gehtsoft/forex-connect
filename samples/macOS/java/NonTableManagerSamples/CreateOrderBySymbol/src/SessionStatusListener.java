package common;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class SessionStatusListener implements IO2GSessionStatus {
    private O2GSession mSession;
    private String mSessionID;
    private String mPin;
    private boolean mConnected;
    private boolean mDisconnected;
    private boolean mError;
    private Semaphore mSemaphore;

    // ctor
    public SessionStatusListener(O2GSession session, String sSessionID, String sPin) {
        mSession = session;
        mSessionID = sSessionID;
        mPin = sPin;
        reset();
        mSemaphore = new Semaphore(0);
    }

    public boolean isConnected() {
        return mConnected;
    }

    public boolean isDisconnected() {
        return mDisconnected;
    }

    public boolean hasError() {
        return mError;
    }

    public void reset() {
        mConnected = false;
        mDisconnected = false;
        mError = false;
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    public void onSessionStatusChanged(O2GSessionStatusCode status) {
        System.out.println("Status: " + status.toString());
        switch (status) {
        case TRADING_SESSION_REQUESTED:
            if (mSessionID.isEmpty()) {
                System.out.println("Argument for trading session ID is missing");
            } else {
                mSession.setTradingSession(mSessionID, mPin);
            }
            break;
        case CONNECTED:
            mConnected = true;
            mDisconnected = false;
            mSemaphore.release();
            break;
        case DISCONNECTED:
            mConnected = false;
            mDisconnected = true;
            mSemaphore.release();
            break;
        }
    }

    public void onLoginFailed(String sError) {
        System.out.println("Login error: " + sError);
        mError = true;
    }
}

package calculatetradingcommissions;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class SessionStatusListener implements IO2GSessionStatus {
    private O2GSession mSession;
    private boolean mConnected;
    private boolean mDisconnected;
    private boolean mError;
    private Semaphore mSemaphore;

    // ctor
    public SessionStatusListener(O2GSession session) {
        mSession = session;
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
            O2GSessionDescriptorCollection descs = mSession.getTradingSessionDescriptors();
            System.out.println("Session descriptors:");
            System.out.println("id, name, description, requires pin");
            for (O2GSessionDescriptor desc : descs) {
                System.out.println(String.format("'%s' '%s' '%s' %s", desc.getId(), desc.getName(), desc.getDescription(), desc.isPinRequired()));
            }
            
            InputStreamReader converter = new InputStreamReader(System.in);
            BufferedReader in = new BufferedReader(converter);
            System.out.println("Please enter trading session ID and press \'Enter\'");
            String sSessionID = "";
            try {
                sSessionID = in.readLine();
            } catch (IOException e) {
            }
            System.out.println("Please enter pin (if required). Then press \'Enter\'");
            String sPin = "";
            try {
                sPin = in.readLine();
            } catch (IOException e) {
            }

            mSession.setTradingSession(sSessionID.trim(), sPin.trim());
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

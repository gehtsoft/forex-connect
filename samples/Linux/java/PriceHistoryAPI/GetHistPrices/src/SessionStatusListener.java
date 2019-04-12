/* Copyright 2019 FXCM Global Services, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use these files except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
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

    public void reset() {
        mConnected = false;
        mDisconnected = false;
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    @Override
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

    @Override
    public void onLoginFailed(String sError) {
        System.out.println("Login error: " + sError);
        mSemaphore.release();
    }
}

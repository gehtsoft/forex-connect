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

import com.candleworks.pricehistorymgr.*;

/**
 * The Price History API communicator request status listener.
 */
public class CommunicatorStatusListener implements IPriceHistoryCommunicatorStatusListener {
    
    // Data fields
    private boolean mReady;
    private Semaphore mSemaphore;

    // ctor
    public CommunicatorStatusListener() {
        mSemaphore = new Semaphore(0);
    }

    /** Gets the PriceHistoryCommunicator state. */
    public boolean isReady() {
        return mReady;
    }

    /** Resets the PriceCommunicator state. */
    public void reset() {
        mReady = false;
    }

    /** Waits for a PriceCommunicator event. */
    public boolean waitEvents() throws Exception {
        if (mSemaphore.tryAcquire(30, TimeUnit.SECONDS)) {
            return true;
        }

        System.out.println("Timeout occurred during waiting for communicator status is ready");
        return false;
    }

    /** IPriceHistoryCommunicatorStatusListener method. */
    @Override
    public void onCommunicatorStatusChanged(boolean ready) {
        mReady = ready;
        mSemaphore.release();
    }

    /** IPriceHistoryCommunicatorStatusListener method. */
    @Override
    public void onCommunicatorInitFailed(PriceHistoryError error) {
        System.out.println("Communicator initialization error: " + error.getMessage());
        mSemaphore.release();
    }
}

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
package getliveprices;

import java.util.concurrent.Semaphore;
import com.candleworks.pricehistorymgr.*;

/**
 * The Price History API communicator request result listener.
 */
public class ResponseListener implements IPriceHistoryCommunicatorListener {

    // Data fields
    private IPriceHistoryCommunicatorRequest mRequest;
    private IPriceHistoryCommunicatorResponse mResponse;
    private Semaphore mSemaphore;

    // ctor
    public ResponseListener() {
        mResponse = null;
        mSemaphore = new Semaphore(0);
    }

    /** Sets request. */
    public void setRequest(IPriceHistoryCommunicatorRequest request) {
        mResponse = null;
        mRequest = request;
    }

    /** Waits for a response event. */
    public void waitEvents() throws Exception {
        mSemaphore.acquire();
    }

    /** Gets the response. */
    public IPriceHistoryCommunicatorResponse getResponse() {
        return mResponse;
    }

    /** IPriceHistoryCommunicatorListener method. */
    @Override
    public void onRequestCompleted(IPriceHistoryCommunicatorRequest request, IPriceHistoryCommunicatorResponse response) {
        if (mRequest == request) {
            mResponse = response;
            mRequest = null;

            mSemaphore.release();
        }
    }

    /** IPriceHistoryCommunicatorListener method. */
    @Override
    public void onRequestFailed(IPriceHistoryCommunicatorRequest request, PriceHistoryError error) {
        if (mRequest == request) {
            System.out.println("Request failed: " + error.getMessage());

            mResponse = null;
            mRequest = null;

            mSemaphore.release();
        }
    }

    /** IPriceHistoryCommunicatorListener method. */
    @Override
    public void onRequestCancelled(IPriceHistoryCommunicatorRequest request) {
        if (mRequest == request) {
            System.out.println("Request cancelled.");

            mResponse = null;
            mRequest = null;

            mSemaphore.release();
        }
    }
}

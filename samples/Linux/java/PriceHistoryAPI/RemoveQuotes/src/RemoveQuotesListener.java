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
package removequotes;

import java.util.concurrent.Semaphore;

import com.candleworks.quotesmgr.*;

/**
 * The listener for QuotesManager remove quotes task.
 */
class RemoveQuotesListener implements IRemoveQuotesCallback {

    /**
     * The event of the QuotesManager.
     */
    private Semaphore mSemaphore;

    /**
     * Constructor.
     */
    public RemoveQuotesListener() {
        mSemaphore = new Semaphore(0);
    }

    /**
     * Waits for a QuotesManaget event.
     */
    public void waitEvents() throws Exception {
        mSemaphore.acquire();
    }

    /**
     * Listener: when the remove task is cancelled.
     */
    @Override
    public void onTaskCanceled(RemoveQuotesTask task) {
        System.out.println("Remove task was cancelled.");
        mSemaphore.release();
    }

    /**
     * Listener: when the remove task is completed. 
     */
    @Override
    public void onTaskCompleted(RemoveQuotesTask task) {
        System.out.println("Quotes removed successfully.");
        mSemaphore.release();
    }

    /**
     * Listener: when the remove task status is changed.
     */
    @Override
    public void update(RemoveQuotesTask task, QuotesManagerError error) {
        if (error != null) {
            String errorMessage = String.format("%i(%i) : %s", error.getCode(), error.getSubCode(),
                error.getMessage());
            System.out.println("Error occurred : " + errorMessage);
            mSemaphore.release();
        }
    }

}

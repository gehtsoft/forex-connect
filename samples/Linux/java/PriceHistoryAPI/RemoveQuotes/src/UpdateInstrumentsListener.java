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
 * The listener for QuotesManager instruments update task.
 */
class UpdateInstrumentsListener implements IUpdateInstrumentsCallback {

    /**
     * The event of the QuotesManager.
     */
    private Semaphore mSemaphore;

    /**
     * Constructor.
     */
    public UpdateInstrumentsListener() {
        mSemaphore = new Semaphore(0);
    }

    /**
     * Waits for a QuotesManaget event.
     * @throws Exception 
     */
    public void waitEvents() throws Exception {
        mSemaphore.acquire();
    }

    /**
     * Listener: when the instruments update task is cancelled.
     */
    @Override
    public void onTaskCanceled(UpdateInstrumentsTask task) {
        System.out.println("Update instruments task was cancelled.");
        mSemaphore.release();
    }

    /**
     * Listener: when the remove task is completed.
     */
    @Override
    public void onTaskCompleted(UpdateInstrumentsTask task) {
        System.out.println("Update instruments task was completed.");
        mSemaphore.release();
    }

    /**
     * Listener: when the remove task is failed.
     */
    @Override
    public void onTaskFailed(UpdateInstrumentsTask task, QuotesManagerError error) {
        if (error != null) {
            String errorMessage = String.format("%i(%i) : %s", error.getCode(), error.getSubCode(),
                error.getMessage());
            System.out.println("Error occurred : " + errorMessage);
            mSemaphore.release();
        }
    }
}
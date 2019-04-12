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

import java.util.Calendar;

import getliveprices.pricedata.IOffer;

/**
 * Interface of a price update controller which notifies an event when a new
 * price (tick) of some instrument is received.
 *
 * It also contains some helper time conversion methods.
 */
public interface IPriceUpdateController {

    /**
     * Interface of a listener to update prices.
     */
    interface IListener {
        /**
         * Event: When the price is updated.
         */
        void onCollectionUpdate(IOffer offer);
    }

    /**
     * Converts NYT to UTC
     * 
     * @param time
     *            
     * @return
     */
    Calendar estToUtc(Calendar time);

    /**
     * Converts UTC to NYT
     * 
     * @param time
     *            
     * @return
     */
    Calendar utcToEst(Calendar time);

    /**
     * Gets the trading day offset
     * 
     * @return
     */
    int getTradingDayOffset();

    /**
     * Add listener.
     */
    boolean addListener(IListener listener);

    /**
     * Remove listener.
     */
    void removeListener(IListener listener);
}

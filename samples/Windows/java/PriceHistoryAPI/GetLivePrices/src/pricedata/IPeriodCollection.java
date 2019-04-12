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
package getliveprices.pricedata;

/**
 * Interface to the collection of the price periods
 */
public interface IPeriodCollection extends Iterable<IPeriod> {

    /** 
     * The listener for period collection updates.
     */
    interface IListener {
        /**
         * The event is called when the collection is updated by a new tick (for alive collections).
         */
        void onCollectionUpdate(IPeriodCollection collection, int index);
    }

    /**
     * The number of periods in the collection
     */
    int size();

    /**
     * Get the period by its index
     * 
     * @param index
     *            The index of the period. The oldest period has index 0.
     */
    IPeriod get(int index);

    /**
     * Gets the instrument name of the collection
     */
    String getInstrument();

    /**
     * Gets the timeframe name of the collection
     */
    String getTimeframe();

    /**
     * Gets flag indicating that the collection is alive (i.e. is updated when a
     * new price coming)
     */
    boolean isAlive();

    /**
     * Add listener.
     */
    boolean addListener(IListener listener);

    /**
     * Remove listener.
     */
    void removeListener(IListener listener);
}

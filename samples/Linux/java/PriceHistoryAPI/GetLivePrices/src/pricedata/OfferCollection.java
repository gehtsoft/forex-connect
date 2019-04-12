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

import java.util.ArrayList;
import java.util.Calendar;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

/**
 * Implementation of the offers collection
 */
class OfferCollection implements IOfferCollection {
    /**
     * Storage for the offers
     */
    private List<IOffer> mOffers = new ArrayList<IOffer>();

    /**
     * The dictionary to search the offer by the offer id
     */
    private Map<String, Offer> mIdToOffer = new HashMap<String, Offer>();

    /**
     * The dictionary to search the offer by the instrument name
     */
    private Map<String, Offer> mInstrumentToOffer = new HashMap<String, Offer>();

    /**
     * Adds a new offer
     * 
     * @param offerid
     *            OfferId
     * @param instrument
     *            The name of the instrument
     * @param lastChange
     *            The date/time of the last change of the offer
     * @param bid
     *            The current bid price
     * @param ask
     *            The current ask price
     * @param minuteVolume
     *            The current accumulated minute tick volume
     * @param digits
     *            The precision
     */
    public void add(String offerid, String instrument, Calendar lastChange, double bid, double ask,
        int minuteVolume, int digits) {
        Offer offer = new Offer(instrument, lastChange, bid, ask, minuteVolume, digits);
        mOffers.add(offer);
        mIdToOffer.put(offerid, offer);
        mInstrumentToOffer.put(instrument, offer);
    }

    /**
     * Removes all offers from the collection
     */
    public void clear() {
        mOffers.clear();
        mIdToOffer.clear();
        mInstrumentToOffer.clear();
    }

    /**
     * Get offer by instrument or ID
     * 
     * @param key
     * @return
     */
    public Offer get(String key) {
        Offer offer = null;

        offer = mInstrumentToOffer.get(key);
        if (offer != null) {
            return offer;
        }

        offer = mIdToOffer.get(key);
        if (offer != null) {
            return offer;
        }

        return null;
    }

    @Override
    public int size() {
        return mOffers.size();
    }

    @Override
    public IOffer get(int index) {
        return mOffers.get(index);
    }

    @Override
    public Iterator<IOffer> iterator() {
        return mOffers.iterator();
    }
}
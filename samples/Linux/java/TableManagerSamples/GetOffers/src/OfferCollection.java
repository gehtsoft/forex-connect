package getoffers;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

public class OfferCollection implements Iterable<Offer> {

    private List<Offer> mOffers;
    private Map<String, Offer> mIDsAndOffers;

    // ctor
    public OfferCollection() {
        mOffers = new ArrayList<Offer>();
        mIDsAndOffers = new HashMap<String,Offer>();
    }

    // Add offer to collection
    public synchronized void addOffer(Offer offer) {
        mIDsAndOffers.put(offer.getOfferID(), offer);
        mOffers.clear();
        mOffers = new ArrayList<Offer>(mIDsAndOffers.values());
    }

    // Find offer by id
    public synchronized Offer findOffer(String sOfferID) {
        Offer offer = null;
        if (mIDsAndOffers.containsKey(sOfferID)) {
            offer = mIDsAndOffers.get(sOfferID);
        }
        return offer;
    }

    // Get offer by index
    public synchronized Offer getOffer(int index) {
        Offer offer = null;
        if (index >= 0 && index < mOffers.size()) {
            offer = mOffers.get(index);
        }
        return offer;
    }

    // Get number of offers
    public synchronized int size() {
        return mOffers.size();
    }

    // Clear collection
    public synchronized void clear() {
        mOffers.clear();
        mIDsAndOffers.clear();
    }
    
    @Override
    public Iterator<Offer> iterator() {
        return mOffers.iterator();
    }

}

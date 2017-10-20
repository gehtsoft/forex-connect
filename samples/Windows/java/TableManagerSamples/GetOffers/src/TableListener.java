package getoffers;

import com.fxcore2.*;

public class TableListener implements IO2GTableListener {
    private String mInstrument;
    private OfferCollection mOffers;

    // ctor
    public TableListener() {
        mOffers = new OfferCollection();
    }

    public void setInstrument(String sInstrument) {
        mInstrument = sInstrument;
    }

    // Implementation of IO2GTableListener interface public method onAdded
    public void onAdded(String sRowID, O2GRow rowData) {
    }

    // Implementation of IO2GTableListener interface public method onChanged
    public void onChanged(String sRowID, O2GRow rowData) {
        if (rowData.getTableType() == O2GTableType.OFFERS) {
            printOffer((O2GOfferTableRow)rowData, mInstrument);
        }
    }

    // Implementation of IO2GTableListener interface public method onDeleted
    public void onDeleted(String sRowID, O2GRow rowData) {
    }

    public void onStatusChanged(O2GTableStatus status) {
    }

    public void subscribeEvents(O2GTableManager manager) {
        O2GOffersTable offersTable = (O2GOffersTable)manager.getTable(O2GTableType.OFFERS);
        offersTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
    }

    public void unsubscribeEvents(O2GTableManager manager) {
        O2GOffersTable offersTable = (O2GOffersTable)manager.getTable(O2GTableType.OFFERS);
        offersTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
    }

    public void printOffers(O2GOffersTable offers, String sInstrument) {
        O2GOfferTableRow offerRow = null;
        O2GTableIterator iterator = new O2GTableIterator();
        offerRow = offers.getNextRow(iterator);
        while (offerRow != null) {
            printOffer(offerRow, sInstrument);
            offerRow = offers.getNextRow(iterator);
        }
    }

    public void printOffer(O2GOfferTableRow offerRow, String sInstrument) {
        Offer offer = mOffers.findOffer(offerRow.getOfferID());
        if (offer != null) {
            if (offerRow.isTimeValid() && offerRow.isBidValid() && offerRow.isAskValid()) {
                offer.setDate(offerRow.getTime());
                offer.setBid(offerRow.getBid());
                offer.setAsk(offerRow.getAsk());
            }
        } else {
            offer = new Offer(offerRow.getOfferID(), offerRow.getInstrument(),
                             offerRow.getDigits(), offerRow.getPointSize(),
                             offerRow.getTime(), offerRow.getBid(), offerRow.getAsk());
            mOffers.addOffer(offer);
        }
        if (sInstrument.isEmpty() || offerRow.getInstrument().equals(sInstrument)) {
            System.out.println(String.format("%s, %s, Bid=%s, Ask=%s", offer.getOfferID(), offer.getInstrument(), offer.getBid(), offer.getAsk()));
        }
    }
}

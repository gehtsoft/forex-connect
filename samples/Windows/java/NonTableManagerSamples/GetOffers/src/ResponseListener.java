package getoffers;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class ResponseListener implements IO2GResponseListener {
    private O2GSession mSession;
    private String mRequestID;
    private String mInstrument;
    private O2GResponse mResponse;
    private Semaphore mSemaphore;
    private OfferCollection mOffers;

    // ctor
    public ResponseListener(O2GSession session) {
        mSession = session;
        mRequestID = "";
        mInstrument = "";
        mResponse = null;
        mSemaphore = new Semaphore(0);
        mOffers = new OfferCollection();
    }

    public void setRequestID(String sRequestID) {
        mResponse = null;
        mRequestID = sRequestID;
    }

    public void setInstrument(String sInstrument) {
        mInstrument = sInstrument;
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    public O2GResponse getResponse() {
        return mResponse;
    }

    public void onRequestCompleted(String sRequestId, O2GResponse response) {
        if (mRequestID.equals(response.getRequestId())) {
            mResponse = response;
            mSemaphore.release();
        }
    }

    public void onRequestFailed(String sRequestID, String sError) {
        if (mRequestID.equals(sRequestID)) {
            System.out.println("Request failed: " + sError);
            mSemaphore.release();
        }
    }

    public void onTablesUpdates(O2GResponse response) {
        try {
            if (response.getType() == O2GResponseType.TABLES_UPDATES)
                printOffers(mSession, response, mInstrument);
        } catch (Exception e) {
            System.out.println(e.toString());
        }
    }

    // Store offers data from response and print it
    public void printOffers(O2GSession session, O2GResponse response, String sInstrument) throws Exception {
        O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
        if (readerFactory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GOffersTableResponseReader responseReader = readerFactory.createOffersTableReader(response);
        for (int i = 0; i < responseReader.size(); i++) {
            O2GOfferRow offerRow = responseReader.getRow(i);
            Offer offer = mOffers.findOffer(offerRow.getOfferID());
            if (offer != null) {
                if (offerRow.isTimeValid() && offerRow.isBidValid() && offerRow.isAskValid()) {
                    offer.setDate(offerRow.getTime());
                    offer.setBid(offerRow.getBid());
                    offer.setAsk(offerRow.getAsk());
                }
            } else {
                offer = new Offer(offerRow.getOfferID(), offerRow.getInstrument(),
                         offerRow.getDigits(), offerRow.getPointSize(), offerRow.getTime(),
                         offerRow.getBid(), offerRow.getAsk());
                mOffers.addOffer(offer);
            }
            if (sInstrument.isEmpty() || offerRow.getInstrument().equals(sInstrument)) {
                System.out.println(String.format("%s, %s, Bid=%s, Ask=%s", offer.getOfferID(), offer.getInstrument(), offer.getBid(), offer.getAsk()));
            }
        }
    }
}

package getoffers;

import java.util.Calendar;

public class Offer {
    public Calendar getDate() {
        return mDate;
    }
    public void setDate(Calendar dtDate) {
        mDate = dtDate;
    }
    private Calendar mDate;

    public double getBid() {
        return mBid;
    }
    public void setBid(double dBid) {
        mBid = dBid;
    }
    private double mBid;

    public double getAsk() {
        return mAsk;
    }
    public void setAsk(double dAsk)
    {
        mAsk = dAsk;
    }
    private double mAsk;

    public String getOfferID() {
        return mOfferID;
    }
    public void setOfferID(String sOfferID) {
        mOfferID = sOfferID;
    }
    private String mOfferID;

    public String getInstrument() {
        return mInstrument;
    }
    public void setInstrument(String sInstrument) {
        mInstrument = sInstrument;
    }
    private String mInstrument;

    public int getPrecision() {
        return mPrecision;
    }
    public void setPrecision(int iPrecision) {
        mPrecision = iPrecision;
    }
    private int mPrecision;

    public double getPipSize() {
        return mPipSize;
    }
    public void setPipSize(double dPipSize) {
        mPipSize = dPipSize;
    }
    private double mPipSize;

    // ctor
    public Offer(String sOfferID, String sInstrument, int iPrecision, double dPipSize, Calendar dtDate, double dBid, double dAsk) {
        mOfferID = sOfferID;
        mInstrument = sInstrument;
        mPrecision = iPrecision;
        mPipSize = dPipSize;
        mDate = dtDate;
        mBid = dBid;
        mAsk = dAsk;
    }
}

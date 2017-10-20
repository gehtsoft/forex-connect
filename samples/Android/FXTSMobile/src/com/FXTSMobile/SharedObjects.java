package com.FXTSMobile;

import java.math.BigDecimal;
import java.util.Map;

import android.util.Pair;

import com.fxcore2.O2GOfferRow;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GTransport;

public class SharedObjects {
    
    private O2GSession mSession;
    private Map<String, Pair<BigDecimal, BigDecimal>> mActualPrices;
    private O2GOfferRow mSelectedOfferRow;
    private String mAccountID;
    
    private SharedObjects() {
        mSession = O2GTransport.createSession();
        mActualPrices = null;
        mSelectedOfferRow = null;
    }
    
    private static SharedObjects mInstance;
    
    public static SharedObjects getInstance() {
        if (mInstance == null) {
            mInstance = new SharedObjects();
        }
        return mInstance;
    }
    
    public O2GSession getSession() {
        return mSession;
    }
    
    public void setActualPrices(Map<String, Pair<BigDecimal, BigDecimal>> actualPrices) {
        mActualPrices = actualPrices;
    }
    
    public Map<String, Pair<BigDecimal, BigDecimal>> getActualPrices() {
        return mActualPrices;
    }
    
    public void setSelectedOffer(O2GOfferRow offerRow) {
        mSelectedOfferRow = offerRow;
    }
    
    public O2GOfferRow getSelectedOffer() {
        return mSelectedOfferRow;
    }
    
    public void setAccountID(String sAccountID) {
        mAccountID = sAccountID;
    }
    
    public String getAccountID() {
        return mAccountID;
    }
}
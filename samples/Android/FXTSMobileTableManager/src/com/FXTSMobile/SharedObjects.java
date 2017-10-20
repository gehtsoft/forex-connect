package com.FXTSMobile;

import com.fxcore2.O2GSession;
import com.fxcore2.O2GTableManagerMode;
import com.fxcore2.O2GTransport;

public class SharedObjects {
    
    private O2GSession mSession;
    
    private SharedObjects() {
        mSession = O2GTransport.createSession();
        mSession.useTableManager(O2GTableManagerMode.YES, null);
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
}
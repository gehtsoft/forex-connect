package com.FXTSMobile;

import com.fxcore2.*;

import java.util.Map;

public class CommissionsCalculator {

    private final String COMMISSIONED_UI_ENABLED_PROP = "COMMISSIONED_UI_ENABLED";

    private O2GCommissionsProvider mCommissionsProvider;
    private O2GTradingSettingsProvider mSettingsProvider;

    private O2GAccountsTable mAccountsTable;
    private O2GAccountTableRow mAccountTableRow;

    private boolean mIsCommEnabled;

    public CommissionsCalculator(O2GSession session) {
        this(session, null);

        if (mAccountsTable.size() > 0) {
            mAccountTableRow = mAccountsTable.getRow(0);
        }
    }

    public CommissionsCalculator(O2GSession session, O2GAccountTableRow accountTableRow) {
        if (session.getSessionStatus() != O2GSessionStatusCode.CONNECTED)
            throw new IllegalStateException("session_not_ready");

        mAccountTableRow = accountTableRow;

        initializeTables();
        initializeCommProvider(session);

        mIsCommEnabled = detectCommissionsSupport(session);
    }

    private void initializeTables() {
        mAccountsTable = (O2GAccountsTable) SharedObjects.getInstance()
                .getSession().getTableManager().getTable(O2GTableType.ACCOUNTS);
    }

    private void initializeCommProvider(O2GSession session) {
        mCommissionsProvider = session.getCommissionsProvider();

        while (mCommissionsProvider.getStatus() != O2GCommissionStatusCode.READY &&
                mCommissionsProvider.getStatus() != O2GCommissionStatusCode.FAIL &&
                mCommissionsProvider.getStatus() != O2GCommissionStatusCode.DISABLED) {
            try {
                Thread.sleep(50);
            } catch (InterruptedException e) {
                throw new IllegalStateException("commissions_provider_not_ready", e);
            }
        }

        if (mCommissionsProvider.getStatus() != O2GCommissionStatusCode.READY)
            throw new IllegalStateException("commissions_provider_not_ready");
    }

    private boolean detectCommissionsSupport(O2GSession session) {
        O2GLoginRules loginRules = session.getLoginRules();
        mSettingsProvider = loginRules.getTradingSettingsProvider();
        O2GResponse response = loginRules.getSystemPropertiesResponse();

        O2GSystemPropertiesReader reader = session.getResponseReaderFactory().createSystemPropertiesReader(response);
        if (reader == null)
            return false;

        Map<String, String> properties = reader.getProperties();
        if (properties == null)
            return false;

        if (properties.containsKey(COMMISSIONED_UI_ENABLED_PROP)) {
            String commPropValue = properties.get(COMMISSIONED_UI_ENABLED_PROP);
            return commPropValue != null && commPropValue.equals("Y");
        }

        return false;
    }

    private int calcAmount(O2GOfferTableRow offersTableRow, int lots) {
        int baseUnitSize = mSettingsProvider.getBaseUnitSize(offersTableRow.getInstrument(), mAccountTableRow);
        return baseUnitSize * lots;
    }

    // public
    //

    public double calcOpenCommission(O2GOfferTableRow offersTableRow, int lots, boolean isBuy) {
        if (mAccountTableRow == null || offersTableRow == null)
            return Double.NaN;

        int amount = calcAmount(offersTableRow, lots);
        String buySell = isBuy ? "B" : "S";
        return mCommissionsProvider.calcOpenCommission(offersTableRow, mAccountTableRow, amount, buySell, 0);
    }

    public double calcCloseCommission(O2GOfferTableRow offersTableRow, int lots, boolean isBuy) {
        if (mAccountTableRow == null || offersTableRow == null)
            return Double.NaN;

        int amount = calcAmount(offersTableRow, lots);
        String buySell = isBuy ? "B" : "S";
        return mCommissionsProvider.calcCloseCommission(offersTableRow, mAccountTableRow, amount, buySell, 0);
    }

    public double calcTotalCommission(O2GOfferTableRow offersTableRow, int lots, boolean isBuy) {
        if (mAccountTableRow == null || offersTableRow == null)
            return Double.NaN;

        int amount = calcAmount(offersTableRow, lots);
        String buySell = isBuy ? "B" : "S";
        return mCommissionsProvider.calcTotalCommission(offersTableRow, mAccountTableRow, amount, buySell, 0, 0);
    }

    public boolean isCommEnabled() {
        return mIsCommEnabled;
    }
}



















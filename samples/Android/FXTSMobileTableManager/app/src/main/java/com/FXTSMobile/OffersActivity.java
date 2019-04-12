package com.FXTSMobile;

import java.text.DecimalFormat;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.graphics.Color;
import android.os.Bundle;
import android.os.Handler;
import android.view.Gravity;
import android.view.View;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;

import com.fxcore2.IO2GTableListener;
import com.fxcore2.O2GOfferTableRow;
import com.fxcore2.O2GOffersTable;
import com.fxcore2.O2GRow;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GTableStatus;
import com.fxcore2.O2GTableType;
import com.fxcore2.O2GTableUpdateType;
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSessionStatusCode;

public class OffersActivity extends Activity implements IO2GTableListener, IO2GSessionStatus {
    private final int STANDARD_MARGIN = 5;
    private final int COLOR_UP = Color.rgb(99, 255, 99);
    private final int COLOR_DOWN = Color.rgb(255, 99, 99);
    private Handler mHandler = new Handler();
    private TableLayout mTableLayout;
    private Map<String, TableRow> mOfferTableRows;
    private DecimalFormat[] mDecimalFormats = null;
    private O2GOffersTable mOffersTable;

    private boolean mActive = false;
    private O2GSession mSession;

    private final int mLots = 1;
    private final boolean isBuy = true;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.offers);
        mTableLayout = (TableLayout) findViewById(R.id.tablelayout);
        mOfferTableRows = new HashMap<String, TableRow>();
        initializePredefinedDecimalFormats();
        initializeOffersTable();

        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeSessionStatus(this);
    }

    @Override
    protected void onStart() {
        initializeTableLayout();
        super.onStart();
    }

    private void initializeTableLayout() {
        CommissionsCalculator commCalc = SharedObjects.getInstance().getCommissionsCalculator();

        if (commCalc.isCommEnabled()) {
            findViewById(R.id.tvCommOpen).setVisibility(View.VISIBLE);
            findViewById(R.id.tvCommClose).setVisibility(View.VISIBLE);
            findViewById(R.id.tvCommTotal).setVisibility(View.VISIBLE);
        }
    }

    private void initializePredefinedDecimalFormats() {
        mDecimalFormats = new DecimalFormat[10];
        
        for (int i = 0; i < 10; i++) {
            DecimalFormat decimalFormat = new DecimalFormat();
            decimalFormat.setDecimalSeparatorAlwaysShown(false);
            decimalFormat.setMaximumFractionDigits(i);
            decimalFormat.setMinimumFractionDigits(i);
            mDecimalFormats[i] = decimalFormat;
        }
    }

    private void initializeOffersTable() {
        mOffersTable = (O2GOffersTable) SharedObjects.getInstance().getSession().getTableManager().getTable(
                O2GTableType.OFFERS);

        int iSize = mOffersTable.size();
        List<O2GOfferTableRow> offerRows = new ArrayList<O2GOfferTableRow>();
        for (int i = 0; i < iSize; i++) {
            offerRows.add(mOffersTable.getRow(i));
        }
        
        Collections.sort(offerRows, new Comparator<O2GOfferTableRow>() {

            @Override
            public int compare(O2GOfferTableRow r1, O2GOfferTableRow r2) {
                Integer iOfferID1 = Integer.parseInt(r1.getOfferID());
                Integer iOfferID2 = Integer.parseInt(r2.getOfferID());
                return iOfferID1.compareTo(iOfferID2);
            }
        }
        );
        
        for (int i = 0; i < iSize; i++) {
            this.initializeTableRowView(offerRows.get(i));
        }
        
        mOffersTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
    }

    private void initializeTableRowView(O2GOfferTableRow row) {
        TableRow tableRow = new TableRow(this);

        TextView tvSymbol = new TextView(this);
        tvSymbol.setPadding(0, STANDARD_MARGIN, 0, STANDARD_MARGIN);
        tvSymbol.setText(row.getInstrument());
        tvSymbol.setLayoutParams(new TableRow.LayoutParams());

        tableRow.addView(tvSymbol);

        int iDigits = row.getDigits();
        DecimalFormat decimalFormat = mDecimalFormats[iDigits];

        double dBid = row.getBid();
        TextView tvBid = new TextView(this);
        tvBid.setText(decimalFormat.format(dBid));
        tvBid.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvBid.setGravity(Gravity.RIGHT);
        tvBid.setLayoutParams(new TableRow.LayoutParams());
        tvBid.setTag(dBid);
        tableRow.addView(tvBid);

        double dAsk = row.getAsk();
        TextView tvAsk = new TextView(this);
        tvAsk.setText(decimalFormat.format(dAsk));
        tvAsk.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvAsk.setGravity(Gravity.RIGHT);
        tvAsk.setLayoutParams(new TableRow.LayoutParams());
        tvAsk.setTag(dAsk);
        tableRow.addView(tvAsk);

        double dSpread = OffersActivity.calculateSpread(dBid, dAsk,
                row.getPointSize());
        TextView tvSpread = new TextView(this);
        tvSpread.setText(mDecimalFormats[1].format(dSpread));
        tvSpread.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvSpread.setGravity(Gravity.RIGHT);
        tvSpread.setLayoutParams(new TableRow.LayoutParams());
        tvSpread.setTag(dSpread);
        tableRow.addView(tvSpread);

        TextView tvPipCost = new TextView(this);
        tvPipCost.setText(mDecimalFormats[2].format(row.getPipCost()));
        tvPipCost.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvPipCost.setGravity(Gravity.RIGHT);
        tvPipCost.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvPipCost);

        TextView tvHigh = new TextView(this);
        tvHigh.setText(decimalFormat.format(row.getHigh()));
        tvHigh.setTextColor(COLOR_UP);
        tvHigh.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvHigh.setGravity(Gravity.RIGHT);
        tvHigh.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvHigh);

        TextView tvLow = new TextView(this);
        tvLow.setText(decimalFormat.format(row.getLow()));
        tvLow.setTextColor(COLOR_DOWN);
        tvLow.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvLow.setGravity(Gravity.RIGHT);
        tvLow.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvLow);

        // calculate commissions
        CommissionsCalculator commCalc = SharedObjects.getInstance().getCommissionsCalculator();

        if (commCalc.isCommEnabled()) {

            double commOpen = commCalc.calcOpenCommission(row, mLots, isBuy);
            double commClose = commCalc.calcCloseCommission(row, mLots, isBuy);
            double commTotal = commCalc.calcTotalCommission(row, mLots, isBuy);

            TextView tvCommOpen = new TextView(this);
            tvCommOpen.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
            tvCommOpen.setText(mDecimalFormats[2].format(commOpen));
            tvCommOpen.setGravity(Gravity.RIGHT);
            tvCommOpen.setLayoutParams(new TableRow.LayoutParams());
            tvCommOpen.setTag(commOpen);
            tableRow.addView(tvCommOpen);

            TextView tvCommClose = new TextView(this);
            tvCommClose.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
            tvCommClose.setText(mDecimalFormats[2].format(commClose));
            tvCommClose.setGravity(Gravity.RIGHT);
            tvCommClose.setLayoutParams(new TableRow.LayoutParams());
            tvCommClose.setTag(commClose);
            tableRow.addView(tvCommClose);

            TextView tvCommTotal = new TextView(this);
            tvCommTotal.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
            tvCommTotal.setText(mDecimalFormats[2].format(commTotal));
            tvCommTotal.setGravity(Gravity.RIGHT);
            tvCommTotal.setLayoutParams(new TableRow.LayoutParams());
            tvCommTotal.setTag(commTotal);
            tableRow.addView(tvCommTotal);
        }

        mOfferTableRows.put(row.getOfferID(), tableRow);

        mTableLayout.addView(tableRow, new TableLayout.LayoutParams(
                TableLayout.LayoutParams.FILL_PARENT,
                TableLayout.LayoutParams.WRAP_CONTENT));
    }

    private static double calculateSpread(double dBid, double dAsk,
            double dPointSize) {
        return (dAsk - dBid) / dPointSize;
    }

    @Override
    public void onAdded(String rowID, O2GRow rowData) {
    }

    @Override
    public void onChanged(String rowID, O2GRow rowData) {
        UpdateOffersRunnable runnable = new UpdateOffersRunnable(
                (O2GOfferTableRow) rowData);
        mHandler.post(runnable);
    }

    @Override
    public void onDeleted(String rowID, O2GRow rowData) {
    }

    @Override
    public void onStatusChanged(O2GTableStatus status) {
    }

    @Override
    protected void onDestroy() {
        mOffersTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);

        mSession.unsubscribeSessionStatus(this);

        super.onDestroy();
    }

    private class UpdateOffersRunnable implements Runnable {

        private double mBid;

        private double mAsk;

        private double mPointSize;

        private double mHigh;

        private double mLow;

        private double mPipCost;

        private String mOfferID;

        private double mCommOpen;

        private double mCommClose;

        private double mCommTotal;

        private DecimalFormat mDecimalFormat = null;

        public UpdateOffersRunnable(O2GOfferTableRow offerRow) {
            mBid = offerRow.getBid();
            mAsk = offerRow.getAsk();
            mPointSize = offerRow.getPointSize();
            mHigh = offerRow.getHigh();
            mLow = offerRow.getLow();
            mPipCost = offerRow.getPipCost();
            mOfferID = offerRow.getOfferID();
            mDecimalFormat = mDecimalFormats[offerRow.getDigits()];

            CommissionsCalculator commCalc = SharedObjects.getInstance().getCommissionsCalculator();

            if (commCalc.isCommEnabled()) {
                mCommOpen = commCalc.calcOpenCommission(offerRow, mLots, isBuy);
                mCommClose = commCalc.calcCloseCommission(offerRow, mLots, isBuy);
                mCommTotal = commCalc.calcTotalCommission(offerRow, mLots, isBuy);
            }
        }

        public void run() {
            TableRow tableRow = mOfferTableRows.get(mOfferID);

            if (tableRow == null) {
                return;
            }

            TextView tvBid = (TextView) tableRow.getChildAt(1);
            tvBid.setText(mDecimalFormat.format(mBid));
            this.setColor((Double) tvBid.getTag(), mBid, tvBid);
            tvBid.setTag(mBid);

            TextView tvAsk = (TextView) tableRow.getChildAt(2);
            tvAsk.setText(mDecimalFormat.format(mAsk));
            this.setColor((Double) tvAsk.getTag(), mAsk, tvAsk);
            tvAsk.setTag(mAsk);

            double dSpread = OffersActivity.calculateSpread(mBid, mAsk,
                    mPointSize);
            TextView tvSpread = (TextView) tableRow.getChildAt(3);
            tvSpread.setText(mDecimalFormats[1].format(dSpread));
            this.setColor((Double) tvSpread.getTag(), dSpread, tvSpread);
            tvSpread.setTag(dSpread);

            TextView tvPipCost = (TextView) tableRow.getChildAt(4);
            tvPipCost.setText(mDecimalFormats[2].format(mPipCost));

            TextView tvHigh = (TextView) tableRow.getChildAt(5);
            tvHigh.setText(mDecimalFormat.format(mHigh));

            TextView tvLow = (TextView) tableRow.getChildAt(6);
            tvLow.setText(mDecimalFormat.format(mLow));

            CommissionsCalculator commCalc = SharedObjects.getInstance().getCommissionsCalculator();

            if (commCalc.isCommEnabled()) {

                TextView tvCommOpen = (TextView) tableRow.getChildAt(7);
                tvCommOpen.setText(mDecimalFormats[2].format(mCommOpen));
                this.setColor((Double) tvCommOpen.getTag(), mCommOpen, tvCommOpen);
                tvCommOpen.setTag(mCommOpen);

                TextView tvCommClose = (TextView) tableRow.getChildAt(8);
                tvCommClose.setText(mDecimalFormats[2].format(mCommClose));
                this.setColor((Double) tvCommClose.getTag(), mCommClose, tvCommClose);
                tvCommClose.setTag(mCommClose);

                TextView tvCommTotal = (TextView) tableRow.getChildAt(9);
                tvCommTotal.setText(mDecimalFormats[2].format(mCommTotal));
                this.setColor((Double) tvCommTotal.getTag(), mCommTotal, tvCommTotal);
                tvCommTotal.setTag(mCommTotal);
            }
        }

        private void setColor(double dOldValue, double dNewValue, TextView view) {
            int iResult = Double.compare(dNewValue, dOldValue);
            if (iResult == 0) {
                view.setTextColor(view.getTextColors().getDefaultColor());
            } else if (iResult > 0) {
                view.setTextColor(COLOR_UP);
            } else {
                view.setTextColor(COLOR_DOWN);
            }
        }
    }

    @Override
    protected void onPause() {
        mActive = false;
        super.onPause();
    }

    private void showCustomMsgDlg(final String msg, final boolean logout, final boolean goToLogin, final boolean goBack) {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);

        final Intent intent = goToLogin ? new Intent(this, LoginDialogActivity.class) : null;

        DialogInterface.OnClickListener listener = new DialogInterface.OnClickListener() {
            public void onClick(DialogInterface dialog, int id) {
                if (logout && mSession != null)
                    mSession.logout();

                if (goToLogin) {
                    startActivity(intent);
                    finish();
                }
                else if (goBack)
                    onBackPressed();
            }
        };
        builder.setMessage(msg);
        builder.setCancelable(false);
        builder.setPositiveButton("Ok", listener);

        AlertDialog alert = builder.create();
        alert.show();
    }

    @Override
    public void onResume() {
        mActive = true;

        O2GSessionStatusCode status = mSession.getSessionStatus();
        if (O2GSessionStatusCode.CONNECTED != status) {
            showCustomMsgDlg("Session lost, please log in again", false, true, false);
        }
        super.onResume();
    }
    
    @Override
    public void onSessionStatusChanged(final O2GSessionStatusCode o2GSessionStatusCode) {
        O2GSessionStatusCode status = mSession.getSessionStatus();
        if (O2GSessionStatusCode.CONNECTED != status &&
                mActive /*get rid of redundant message*/) {

            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    showCustomMsgDlg("Session lost, please log in again", false, true, false);
                }
            });
        }
    }

    @Override
    public void onLoginFailed(final String s) {}
}
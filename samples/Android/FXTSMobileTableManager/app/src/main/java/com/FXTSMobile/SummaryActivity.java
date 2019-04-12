package com.FXTSMobile;

import java.text.DecimalFormat;
import java.util.HashMap;
import java.util.Map;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.view.Gravity;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;

import com.fxcore2.IO2GEachRowListener;
import com.fxcore2.IO2GTableListener;
import com.fxcore2.O2GOfferTableRow;
import com.fxcore2.O2GOffersTable;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GSummaryTableRow;
import com.fxcore2.O2GSummaryTable;
import com.fxcore2.O2GRow;
import com.fxcore2.O2GTableStatus;
import com.fxcore2.O2GTableType;
import com.fxcore2.O2GTableUpdateType;
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSessionStatusCode;

public class SummaryActivity extends Activity implements IO2GTableListener, IO2GEachRowListener, IO2GSessionStatus {
    private final int STANDARD_MARGIN = 5;
    private Handler mHandler = new Handler();
    private TableLayout mTableLayout;
    private DecimalFormat[] mDecimalFormats = null;
    private Map<String, TableRow> mSummaryTableRows;
    private O2GSummaryTable mSummaryTable;
    private O2GOffersTable mOffersTable;

    private boolean mActive = false;
    private O2GSession mSession;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.summary);
        mTableLayout = (TableLayout) findViewById(R.id.tablelayout);
        mSummaryTableRows = new HashMap<String, TableRow>();
        
        initializePredefinedDecimalFormats();
        initializeOffersTable();
        initializeSummaryTable();

        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeSessionStatus(this);
    }

    private void initializeOffersTable() {
        mOffersTable = (O2GOffersTable) SharedObjects.getInstance()
                .getSession().getTableManager().getTable(O2GTableType.OFFERS);
    }

    private void initializePredefinedDecimalFormats() {
        mDecimalFormats = new DecimalFormat[10];
        
        for (int i = 0; i < mDecimalFormats.length; i++) {
            DecimalFormat decimalFormat = new DecimalFormat();
            decimalFormat.setDecimalSeparatorAlwaysShown(true);
            decimalFormat.setMaximumFractionDigits(i);
            decimalFormat.setMinimumFractionDigits(i);
            mDecimalFormats[i] = decimalFormat;
        }
    }
    
    private void initializeSummaryTable() {
        mSummaryTable = (O2GSummaryTable) SharedObjects.getInstance()
                .getSession().getTableManager().getTable(O2GTableType.SUMMARY);
        mSummaryTable.forEachRow(this);
        mSummaryTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        mSummaryTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        mSummaryTable.subscribeUpdate(O2GTableUpdateType.DELETE, this);
    }

    @Override
    public void onAdded(String rowID, O2GRow rowData) {
        final O2GSummaryTableRow row = (O2GSummaryTableRow)rowData;
        mHandler.post(new Runnable() {
            
            @Override
            public void run() {
                addSummary(row);
            }
        });
    }

    @Override
    public void onChanged(String rowID, O2GRow rowData) {
        UpdateSummarysRunnable runnable = new UpdateSummarysRunnable(
                (O2GSummaryTableRow) rowData);
        mHandler.post(runnable);
    }

    @Override
    public void onDeleted(String rowID, O2GRow rowData) {
        final String sOfferID = rowID;
        mHandler.post(new Runnable() {
            
            @Override
            public void run() {
                TableRow tableRow = mSummaryTableRows.remove(sOfferID);
                if (tableRow != null) {
                    mTableLayout.removeView(tableRow);
                }
            }
        });
    }

    @Override
    public void onStatusChanged(O2GTableStatus status) {
    }

    @Override
    public void onEachRow(String rowID, O2GRow rowData) {
        O2GSummaryTableRow row = (O2GSummaryTableRow) rowData;
        addSummary(row);
    }

    private void addSummary(O2GSummaryTableRow row) {
        TableRow tableRow = new TableRow(this);

        String sOfferID = row.getOfferID();
        O2GOfferTableRow offerRow = mOffersTable.findRow(sOfferID);
        DecimalFormat decimalFormat = mDecimalFormats[offerRow.getDigits()];
        
        TextView tvSummarySymbol = new TextView(this);
        tvSummarySymbol.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvSummarySymbol.setText(offerRow.getInstrument());
        tvSummarySymbol.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvSummarySymbol);
        
        TextView tvSellAmountK = new TextView(this);
        tvSellAmountK.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvSellAmountK.setText(Double.toString(row.getSellAmount()));
        tvSellAmountK.setGravity(Gravity.RIGHT);
        tvSellAmountK.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvSellAmountK);    
        
        TextView tvSellAvgOpenPrice = new TextView(this);
        tvSellAvgOpenPrice.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvSellAvgOpenPrice.setText(decimalFormat.format(row.getSellAvgOpen()));
        tvSellAvgOpenPrice.setGravity(Gravity.RIGHT);
        tvSellAvgOpenPrice.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvSellAvgOpenPrice);   
        
        TextView tvBuyAmountK = new TextView(this);
        tvBuyAmountK.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvBuyAmountK.setText(Double.toString(row.getBuyAmount()));
        tvBuyAmountK.setGravity(Gravity.RIGHT);
        tvBuyAmountK.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvBuyAmountK); 
        
        TextView tvBuyAvgOpenPrice = new TextView(this);
        tvBuyAvgOpenPrice.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvBuyAvgOpenPrice.setText(decimalFormat.format(row.getBuyAvgOpen()));
        tvBuyAvgOpenPrice.setGravity(Gravity.RIGHT);
        tvBuyAvgOpenPrice.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvBuyAvgOpenPrice);
        
        TextView tvGrossPL = new TextView(this);
        tvGrossPL.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvGrossPL.setText(decimalFormat.format(row.getGrossPL()));
        tvGrossPL.setGravity(Gravity.RIGHT);
        tvGrossPL.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvGrossPL);    
        
        TextView tvNetPL = new TextView(this);
        tvNetPL.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvNetPL.setText(decimalFormat.format(row.getNetPL()));
        tvNetPL.setGravity(Gravity.RIGHT);
        tvNetPL.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvNetPL);  
        
        mSummaryTableRows.put(row.getOfferID(), tableRow);

        mTableLayout.addView(tableRow, new TableLayout.LayoutParams(
                TableLayout.LayoutParams.FILL_PARENT,
                TableLayout.LayoutParams.WRAP_CONTENT));
    }
    
    @Override
    protected void onDestroy() {
        mSummaryTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        mSummaryTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        mSummaryTable.unsubscribeUpdate(O2GTableUpdateType.DELETE, this);

        mSession.unsubscribeSessionStatus(this);

        super.onDestroy();
    }

    private class UpdateSummarysRunnable implements Runnable {

        private String mOfferID;
        
        private double mSellAmountK;
        
        private double mSellAvgOpenPrice;
        
        private double mBuyAmountK;
        
        private double mBuyAvgOpenPrice;
        
        private double mGrossPL;
        
        private double mNetPL;
        
        private int mDigits;
        
        public UpdateSummarysRunnable(O2GSummaryTableRow summaryRow) {
            mOfferID = summaryRow.getOfferID();
            mSellAmountK = summaryRow.getSellAmount();
            mSellAvgOpenPrice = summaryRow.getSellAvgOpen();
            mBuyAmountK = summaryRow.getBuyAmount();
            mBuyAvgOpenPrice = summaryRow.getBuyAvgOpen();
            mGrossPL = summaryRow.getGrossPL();
            mNetPL = summaryRow.getNetPL();
            mDigits = mOffersTable.findRow(summaryRow.getOfferID()).getDigits();
        }

        public void run() {
            TableRow tableRow = mSummaryTableRows.get(mOfferID);

            if (tableRow == null) {
                return;
            }

            DecimalFormat offerDecimalFormat = mDecimalFormats[mDigits];
            DecimalFormat currencyDecimalFormat = mDecimalFormats[2]; 
            
            ((TextView) tableRow.getChildAt(1)).setText(Double.toString(mSellAmountK));
            ((TextView) tableRow.getChildAt(2)).setText(offerDecimalFormat.format(mSellAvgOpenPrice));
            ((TextView) tableRow.getChildAt(3)).setText(Double.toString(mBuyAmountK));
            ((TextView) tableRow.getChildAt(4)).setText(offerDecimalFormat.format(mBuyAvgOpenPrice));
            ((TextView) tableRow.getChildAt(5)).setText(currencyDecimalFormat.format(mGrossPL));
            ((TextView) tableRow.getChildAt(6)).setText(currencyDecimalFormat.format(mNetPL));
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
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
import com.fxcore2.O2GOrderTableRow;
import com.fxcore2.O2GOrdersTable;
import com.fxcore2.O2GRow;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GTableStatus;
import com.fxcore2.O2GTableType;
import com.fxcore2.O2GTableUpdateType;
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.*;

public class OrdersActivity extends Activity implements IO2GTableListener, IO2GEachRowListener, IO2GSessionStatus {
    private final int STANDARD_MARGIN = 5;
    private Handler mHandler = new Handler();
    private TableLayout mTableLayout;
    private DecimalFormat[] mDecimalFormats = null;
    private Map<String, TableRow> mOrderTableRows;
    private O2GOrdersTable mOrdersTable;
    private O2GOffersTable mOffersTable;

    private boolean mActive = false;
    private O2GSession mSession;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.orders);
        mTableLayout = (TableLayout) findViewById(R.id.tablelayout);
        mOrderTableRows = new HashMap<String, TableRow>();
        
        initializePredefinedDecimalFormats();
        initializeOffersTable();
        initializeOrdersTable();

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
    
    private void initializeOrdersTable() {
        //ORDERS table depends on TRADES table, so TRADES table must be refreshed already
        mOrdersTable = (O2GOrdersTable) SharedObjects.getInstance()
                .getSession().getTableManager().getTable(O2GTableType.ORDERS);
        mOrdersTable.forEachRow(this);
        mOrdersTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        mOrdersTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        mOrdersTable.subscribeUpdate(O2GTableUpdateType.DELETE, this);
    }

    @Override
    public void onAdded(String rowID, O2GRow rowData) {
        final O2GOrderTableRow row = (O2GOrderTableRow)rowData;
        mHandler.post(new Runnable() {
            
            @Override
            public void run() {
                addOrder(row);
            }
        });
    }

    @Override
    public void onChanged(String rowID, O2GRow rowData) {
        UpdateOrdersRunnable runnable = new UpdateOrdersRunnable(
                (O2GOrderTableRow) rowData);
        mHandler.post(runnable);
    }

    @Override
    public void onDeleted(String rowID, O2GRow rowData) {
        final String sOrderID = rowID;
        mHandler.post(new Runnable() {
            
            @Override
            public void run() {
                TableRow tableRow = mOrderTableRows.remove(sOrderID);
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
        O2GOrderTableRow row = (O2GOrderTableRow) rowData;
        addOrder(row);
    }

    private void addOrder(O2GOrderTableRow row) {
        TableRow tableRow = new TableRow(this);

        TextView tvOrderID = new TextView(this);
        tvOrderID.setPadding(0, STANDARD_MARGIN, 0, STANDARD_MARGIN);
        tvOrderID.setText(row.getOrderID());
        tvOrderID.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOrderID);

        TextView tvAccountID = new TextView(this);
        tvAccountID.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvAccountID.setText(row.getAccountID());
        tvAccountID.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvAccountID);
        
        TextView tvOrderType = new TextView(this);
        tvOrderType.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvOrderType.setText(row.getType());
        tvOrderType.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOrderType);      
        
        TextView tvOrderStatus = new TextView(this);
        tvOrderStatus.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvOrderStatus.setText(row.getStatus());
        tvOrderStatus.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOrderStatus);    

        String sOfferID = row.getOfferID();
        O2GOfferTableRow offerRow = mOffersTable.findRow(sOfferID);
        
        TextView tvOrderSymbol = new TextView(this);
        tvOrderSymbol.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvOrderSymbol.setText(offerRow.getInstrument());
        tvOrderSymbol.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOrderSymbol);    
        
        TextView tvOrderSide = new TextView(this);
        tvOrderSide.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvOrderSide.setText(row.getBuySell());
        tvOrderSide.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOrderSide);  

        TextView tvOrderRate = new TextView(this);
        tvOrderRate.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvOrderRate.setText(mDecimalFormats[offerRow.getDigits()].format(row.getRate()));
        tvOrderRate.setGravity(Gravity.RIGHT);
        tvOrderRate.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOrderRate);  
        
        TextView tvOrderAmount = new TextView(this);
        tvOrderAmount.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvOrderAmount.setText(Integer.toString(row.getAmount() / 1000));
        tvOrderAmount.setGravity(Gravity.RIGHT);
        tvOrderAmount.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOrderAmount);    
        
        TextView tvContingencyType = new TextView(this);
        tvContingencyType.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        final O2GContingencyType contingencyType = row.getContingencyType();
        final String contingencyTypeStr = getContingencyTypeString(contingencyType.ordinal());
        tvContingencyType.setText(contingencyTypeStr);
        tvContingencyType.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvContingencyType);    
        
        mOrderTableRows.put(row.getOrderID(), tableRow);

        mTableLayout.addView(tableRow, new TableLayout.LayoutParams(
                TableLayout.LayoutParams.FILL_PARENT,
                TableLayout.LayoutParams.WRAP_CONTENT));
    }
    
    private static String getContingencyTypeString(int iContingencyType) {
        if (iContingencyType == 1) {
            return "OCO";
        } else if (iContingencyType == 2) {
            return "OTO";
        }
        else if (iContingencyType == 3) {
            return "ELS";
        }
        else if (iContingencyType == 4) {
            return "OTOCO";
        }
        else
            return "";
    }

    @Override
    protected void onDestroy() {
        mOrdersTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        mOrdersTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        mOrdersTable.unsubscribeUpdate(O2GTableUpdateType.DELETE, this);

        mSession.unsubscribeSessionStatus(this);

        super.onDestroy();
    }

    private class UpdateOrdersRunnable implements Runnable {

        private String mOrderID;
        
        private String mOrderStatus;
        
        private double mRate;
        
        private int mAmountK;
        
        private int mContingencyType;
        
        private int mDigits;
        
        public UpdateOrdersRunnable(O2GOrderTableRow orderRow) {
            mOrderID = orderRow.getOrderID();
            mOrderStatus = orderRow.getStatus();
            mRate = orderRow.getRate();
            mAmountK = orderRow.getAmount() / 1000;
            mContingencyType = orderRow.getContingencyType().ordinal();
            mDigits = mOffersTable.findRow(orderRow.getOfferID()).getDigits();
        }

        public void run() {
            TableRow tableRow = mOrderTableRows.get(mOrderID);

            if (tableRow == null) {
                return;
            }

            ((TextView) tableRow.getChildAt(3)).setText(mOrderStatus);
            ((TextView) tableRow.getChildAt(6)).setText(mDecimalFormats[mDigits].format(mRate));
            ((TextView) tableRow.getChildAt(7)).setText(Integer.toString(mAmountK));
            ((TextView) tableRow.getChildAt(8)).setText(getContingencyTypeString(mContingencyType));
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
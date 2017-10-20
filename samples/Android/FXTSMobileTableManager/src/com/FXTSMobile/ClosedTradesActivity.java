package com.FXTSMobile;

import java.text.DecimalFormat;
import java.util.HashMap;
import java.util.Map;

import android.app.Activity;
import android.os.Bundle;
import android.os.Handler;
import android.view.Gravity;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;

import com.fxcore2.IO2GEachRowListener;
import com.fxcore2.IO2GTableListener;
import com.fxcore2.O2GClosedTradeRow;
import com.fxcore2.O2GOfferTableRow;
import com.fxcore2.O2GOffersTable;
import com.fxcore2.O2GClosedTradeTableRow;
import com.fxcore2.O2GClosedTradesTable;
import com.fxcore2.O2GRow;
import com.fxcore2.O2GTableStatus;
import com.fxcore2.O2GTableType;
import com.fxcore2.O2GTableUpdateType;

public class ClosedTradesActivity extends Activity implements IO2GTableListener, IO2GEachRowListener {
    private final int STANDARD_MARGIN = 5;
    private Handler mHandler = new Handler();
    private TableLayout mTableLayout;
    private DecimalFormat[] mDecimalFormats = null;
    private Map<String, TableRow> mClosedTradeTableRows;
    private O2GClosedTradesTable mClosedTradesTable;
    private O2GOffersTable mOffersTable;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.closed_trades);
        mTableLayout = (TableLayout) findViewById(R.id.tablelayout);
        mClosedTradeTableRows = new HashMap<String, TableRow>();
        
        initializePredefinedDecimalFormats();
        initializeOffersTable();
        initializeClosedTradesTable();
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
    
    private void initializeClosedTradesTable() {
        mClosedTradesTable = (O2GClosedTradesTable) SharedObjects.getInstance()
                .getSession().getTableManager().getTable(O2GTableType.CLOSED_TRADES);
        mClosedTradesTable.forEachRow(this);
        mClosedTradesTable.subscribeUpdate(O2GTableUpdateType.INSERT, this);
        mClosedTradesTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        mClosedTradesTable.subscribeUpdate(O2GTableUpdateType.DELETE, this);
    }

    @Override
    public void onAdded(String rowID, O2GRow rowData) {
        final O2GClosedTradeTableRow row = (O2GClosedTradeTableRow)rowData;
        mHandler.post(new Runnable() {
            
            @Override
            public void run() {
                addClosedTrade(row);
            }
        });
    }

    @Override
    public void onChanged(String rowID, O2GRow rowData) {
    }

    @Override
    public void onDeleted(String rowID, O2GRow rowData) {
        final String sClosedTradeID = rowID;
        mHandler.post(new Runnable() {
            
            @Override
            public void run() {
                TableRow tableRow = mClosedTradeTableRows.remove(sClosedTradeID);
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
        O2GClosedTradeTableRow row = (O2GClosedTradeTableRow) rowData;
        addClosedTrade(row);
    }

    private void addClosedTrade(O2GClosedTradeTableRow row) {
        TableRow tableRow = new TableRow(this);

        TextView tvTradeID = new TextView(this);
        tvTradeID.setPadding(0, STANDARD_MARGIN, 0, STANDARD_MARGIN);
        tvTradeID.setText(row.getTradeID());
        tvTradeID.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvTradeID);

        TextView tvAccountID = new TextView(this);
        tvAccountID.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvAccountID.setText(row.getAccountID());
        tvAccountID.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvAccountID);
        
        String sOfferID = row.getOfferID();
        O2GOfferTableRow offerRow = mOffersTable.findRow(sOfferID);
        
        TextView tvClosedTradeSymbol = new TextView(this);
        tvClosedTradeSymbol.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvClosedTradeSymbol.setText(offerRow.getInstrument());
        tvClosedTradeSymbol.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvClosedTradeSymbol);  
        
        TextView tvClosedTradeAmount = new TextView(this);
        tvClosedTradeAmount.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvClosedTradeAmount.setText(Integer.toString(row.getAmount() / 1000));
        tvClosedTradeAmount.setGravity(Gravity.RIGHT);
        tvClosedTradeAmount.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvClosedTradeAmount);  
        
        TextView tvClosedTradeSide = new TextView(this);
        tvClosedTradeSide.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvClosedTradeSide.setText(row.getBuySell());
        tvClosedTradeSide.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvClosedTradeSide);    
        
        DecimalFormat offerDecimalFormat = mDecimalFormats[offerRow.getDigits()];
        
        TextView tvOpenPrice = new TextView(this);
        tvOpenPrice.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvOpenPrice.setText(offerDecimalFormat.format(row.getOpenRate()));
        tvOpenPrice.setGravity(Gravity.RIGHT);
        tvOpenPrice.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvOpenPrice);
        
        TextView tvClosePrice = new TextView(this);
        tvClosePrice.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvClosePrice.setText(offerDecimalFormat.format(row.getCloseRate()));
        tvClosePrice.setGravity(Gravity.RIGHT);
        tvClosePrice.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvClosePrice); 
        
        TextView tvPL = new TextView(this);
        tvPL.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvPL.setText(mDecimalFormats[1].format(this.getPL(row)));
        tvPL.setGravity(Gravity.RIGHT);
        tvPL.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvPL); 
        
        TextView tvGrossPL = new TextView(this);
        tvGrossPL.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvGrossPL.setText(mDecimalFormats[2].format(row.getGrossPL()));
        tvGrossPL.setGravity(Gravity.RIGHT);
        tvGrossPL.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvGrossPL);    
        
        mClosedTradeTableRows.put(row.getTradeID(), tableRow);

        mTableLayout.addView(tableRow, new TableLayout.LayoutParams(
                TableLayout.LayoutParams.FILL_PARENT,
                TableLayout.LayoutParams.WRAP_CONTENT));
    }
    
    private double getPL(O2GClosedTradeRow closedTradeRow) {
        String sSide = closedTradeRow.getBuySell();
        double dOpenRate = closedTradeRow.getOpenRate();
        double dCloseRate = closedTradeRow.getCloseRate();
        double dPointSize = mOffersTable.findRow(closedTradeRow.getOfferID()).getPointSize();
        if (sSide.compareTo(com.fxcore2.Constants.Buy) == 0) {
            return (dCloseRate - dOpenRate) / dPointSize; 
        } else {
            return (dOpenRate - dCloseRate) / dPointSize;
        }
    }
    
    @Override
    protected void onDestroy() {
        super.onDestroy();
        mClosedTradesTable.unsubscribeUpdate(O2GTableUpdateType.INSERT, this);
        mClosedTradesTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
        mClosedTradesTable.unsubscribeUpdate(O2GTableUpdateType.DELETE, this);
    }

}
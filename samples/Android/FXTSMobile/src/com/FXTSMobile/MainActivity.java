package com.FXTSMobile;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.Collections;
import java.util.HashMap;
import java.util.Map;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.graphics.Color;
import android.os.Bundle;
import android.os.Handler;
import android.util.Pair;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;
import android.widget.Toast;

import com.fxcore2.IO2GResponseListener;
import com.fxcore2.O2GOfferRow;
import com.fxcore2.O2GOffersTableResponseReader;
import com.fxcore2.O2GRequest;
import com.fxcore2.O2GResponse;
import com.fxcore2.O2GResponseType;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GTableType;


public class MainActivity extends Activity implements IO2GResponseListener, OnClickListener{
    private O2GSession mSession;
    
    private Handler mHandler = new Handler(); 
    
    private TableLayout mTableLayout;
    
    private Map<String, Integer> mIndexes;
    
    private Map<String, Pair<BigDecimal, BigDecimal>> mActualPrices;
    
    private boolean mOffersInitialized;
    
    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);
        mIndexes = new HashMap<String, Integer>();
        mActualPrices = Collections.synchronizedMap(new HashMap<String, Pair<BigDecimal, BigDecimal>>());
        SharedObjects.getInstance().setActualPrices(mActualPrices);
        
        mOffersInitialized = false;
        
        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeResponse(this);
        O2GRequest refreshOffersRequest = mSession.getRequestFactory().createRefreshTableRequest(O2GTableType.OFFERS);
        mSession.sendRequest(refreshOffersRequest);
    }
    
    @Override
    public void onBackPressed() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage("Are you sure you want to logout?")
               .setCancelable(false)
               .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                   public void onClick(DialogInterface dialog, int id) {
                        mSession.unsubscribeResponse(MainActivity.this);
                        mSession.logout();
                        MainActivity.this.finish();
                   }
               })
               .setNegativeButton("No", new DialogInterface.OnClickListener() {
                   public void onClick(DialogInterface dialog, int id) {
                        dialog.cancel();
                   }
               });
        AlertDialog alert = builder.create();
        
        alert.show();
    }
    
    public void onRequestCompleted(final String requestID, final O2GResponse response) {
        if (response.getType() == O2GResponseType.GET_OFFERS) {
            mHandler.post(new Runnable() {
                
                public void run() {
                    O2GOffersTableResponseReader reader = mSession.getResponseReaderFactory().createOffersTableReader(response);
                    
                    mTableLayout = (TableLayout)findViewById(R.id.tablelayout);
                    for (int i = 0; i < reader.size(); i++) {
                        O2GOfferRow row = reader.getRow(i);
                        mIndexes.put(row.getOfferID(), i);
                        
                        TableRow tableRow = new TableRow(MainActivity.this);
                        tableRow.setTag(row);
                        tableRow.setOnClickListener(MainActivity.this);
                        
                        TextView tvSymbol = new TextView(MainActivity.this);
                        tvSymbol.setPadding(0, 5, 0, 5);
                        tvSymbol.setText(row.getInstrument());
                        tvSymbol.setTextSize(TypedValue.COMPLEX_UNIT_DIP, 20);
                        tvSymbol.setLayoutParams(new TableRow.LayoutParams());
                        tableRow.addView(tvSymbol);
                        
                        double dBid = row.getBid();
                        BigDecimal bdBid = new BigDecimal(dBid);
                        bdBid = bdBid.setScale(row.getDigits(), RoundingMode.HALF_UP);
                        
                        TextView tvBid = new TextView(MainActivity.this);
                        tvBid.setText(bdBid.toString());
                        tvBid.setTextSize(TypedValue.COMPLEX_UNIT_DIP, 20);
                        tvBid.setGravity(Gravity.RIGHT);
                        tvBid.setLayoutParams(new TableRow.LayoutParams());
                        tvBid.setTag(bdBid);
                        tableRow.addView(tvBid);

                        double dAsk = row.getAsk();
                        BigDecimal bdAsk = new BigDecimal(dAsk);
                        bdAsk = bdAsk.setScale(row.getDigits(), RoundingMode.HALF_UP);

                        TextView tvAsk = new TextView(MainActivity.this);
                        tvAsk.setText(bdAsk.toString());
                        tvAsk.setTextSize(TypedValue.COMPLEX_UNIT_DIP, 20);
                        tvAsk.setGravity(Gravity.RIGHT);
                        tvAsk.setLayoutParams(new TableRow.LayoutParams());
                        tvAsk.setTag(bdAsk);
                        tableRow.addView(tvAsk);
                        mActualPrices.put(row.getOfferID(), new Pair<BigDecimal, BigDecimal>(bdBid, bdAsk));    
                        
                        mTableLayout.addView(tableRow, new TableLayout.LayoutParams( 
                                TableLayout.LayoutParams.FILL_PARENT, 
                                TableLayout.LayoutParams.WRAP_CONTENT));
                        
                    }
                    
                    mOffersInitialized = true;
                }
            });
        }
        else {
            mHandler.post(new Runnable() {
                
                public void run() {
                    String sText = String.format("Request %s completed", requestID);
                    Toast.makeText(MainActivity.this, sText, Toast.LENGTH_SHORT).show();
                }
            });
        }
    }

    public void onClick(View v) {
        if (v instanceof TableRow) {
            O2GOfferRow offerRow = (O2GOfferRow)v.getTag();
            SharedObjects.getInstance().setSelectedOffer(offerRow);
            Intent intent = new Intent(this, CreateOrderActivity.class);
            startActivity(intent);
        }
    }

    public void onRequestFailed(String requestID, String error) {
        mHandler.post(new RequestFailedRunnable(requestID, error));
    }

    public void onTablesUpdates(O2GResponse response) {
        if (!mOffersInitialized) {
            return;
        }
        
        O2GOffersTableResponseReader reader = mSession.getResponseReaderFactory().createOffersTableReader(response);
        for (int i = 0; i < reader.size(); i++) {
            O2GOfferRow row = reader.getRow(i);
            Integer iIndex = mIndexes.get(row.getOfferID());
            mHandler.post(new UpdateOffersRunnable(iIndex + 2, row));
        }
    }
    
    private class UpdateOffersRunnable implements Runnable {
        private int mIndex;
        private O2GOfferRow mRow;
        
        public UpdateOffersRunnable(int iIndex, O2GOfferRow offerRow) {
            mIndex = iIndex;
            mRow = offerRow;
        }
        
        public void run() {
            TableRow tableRow = (TableRow)mTableLayout.getChildAt(mIndex);
            
            TextView tvBid = (TextView)tableRow.getChildAt(1);
            
            double dBid = mRow.getBid();
            BigDecimal bdBid = new BigDecimal(dBid);
            bdBid = bdBid.setScale(mRow.getDigits(), RoundingMode.HALF_UP);
            tvBid.setText(bdBid.toString());
            
            BigDecimal bdPreviousBid = (BigDecimal)tvBid.getTag();
            this.setColor(bdPreviousBid, bdBid, tvBid);
            tvBid.setTag(bdBid);
            
            TextView tvAsk = (TextView)tableRow.getChildAt(2);
            
            double dAsk = mRow.getAsk();
            BigDecimal bdAsk = new BigDecimal(dAsk);
            bdAsk = bdAsk.setScale(mRow.getDigits(), RoundingMode.HALF_UP);
            tvAsk.setText(bdAsk.toString());
            
            BigDecimal bdPreviousAsk = (BigDecimal)tvAsk.getTag();
            this.setColor(bdPreviousAsk, bdAsk, tvAsk);
            tvAsk.setTag(bdAsk);
            
            mActualPrices.put(mRow.getOfferID(), new Pair<BigDecimal, BigDecimal>(bdBid, bdAsk));   
        }
        
        private void setColor(BigDecimal oldValue, BigDecimal newValue, TextView view) {
            if (newValue.compareTo(oldValue) == 0) {
                view.setBackgroundColor(Color.BLACK);
            }
            else if (newValue.compareTo(oldValue) == 1) {
                view.setBackgroundColor(Color.BLUE);
            }
            else {
                view.setBackgroundColor(Color.RED);
            }
        }
    }
    
    private class RequestFailedRunnable implements Runnable {
        private String mError;
        private String mRequestID;
        
        public RequestFailedRunnable(String sRequestID, String sError) {
            mRequestID = sRequestID;
            mError = sError;
        }
        
        public void run() {
            String sText = String.format("Request %s failed. Error - %s", mRequestID, mError);
            Toast.makeText(MainActivity.this, sText, Toast.LENGTH_SHORT).show();
        }
    }
}
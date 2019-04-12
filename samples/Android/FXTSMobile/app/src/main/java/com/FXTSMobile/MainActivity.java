package com.FXTSMobile;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
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
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSessionStatusCode;

public class MainActivity extends Activity implements IO2GResponseListener, OnClickListener, IO2GSessionStatus {
    private O2GSession mSession;
    
    private Handler mHandler = new Handler(); 
    
    private TableLayout mTableLayout;
    
    private Map<String, Integer> mIndexes;
    
    private Map<String, Pair<BigDecimal, BigDecimal>> mActualPrices;
    
    private boolean mOffersInitialized;

    private boolean mVerbose = false;
    private boolean mActive = false;

    public void onRequestCompleted(final String requestID, final O2GResponse response) {
        final O2GResponseType type = response.getType();

        if (type == O2GResponseType.GET_OFFERS) {
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
        else { // type != GET_OFFERS
            mHandler.post(new Runnable() {

                public void run() {

                    if (mVerbose) {
                        final String sText = String.format("Request %s completed", requestID);

                        runOnUiThread(new Runnable() {
                            @Override
                            public void run() {
                                Toast.makeText(MainActivity.this, sText, Toast.LENGTH_SHORT).show();
                            }
                        });
                    }
                }
            });
        }
    }

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);

        mIndexes = new HashMap<String, Integer>();
        mActualPrices = Collections.synchronizedMap(new HashMap<String, Pair<BigDecimal, BigDecimal>>());
        SharedObjects.getInstance().setActualPrices(mActualPrices);

        setTitle("FXTS Mobile (Non-TM)");

        mOffersInitialized = false;
        
        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeResponse(this);

        O2GRequest refreshOffersRequest = mSession.getRequestFactory().createRefreshTableRequest(O2GTableType.OFFERS);
        mSession.sendRequest(refreshOffersRequest);

        mSession.subscribeSessionStatus(this);
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
            view.setBackgroundColor(Color.WHITE);

            if (newValue.compareTo(oldValue) == 0) {
                view.setTextColor(Color.BLACK);
            }
            else if (newValue.compareTo(oldValue) >= 1) {
                view.setTextColor(Color.BLUE);
            }
            else {

                view.setTextColor(Color.RED);
            }
        }
    }

    @Override
    public void onBackPressed() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage("Are you sure you want to logout?")
               .setCancelable(false)
               .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                   public void onClick(DialogInterface dialog, int id) {
                        mSession.unsubscribeResponse(MainActivity.this);
                        mSession.unsubscribeSessionStatus(MainActivity.this);
                        mSession.logout();

                        Intent intent = new Intent(MainActivity.this, LoginDialogActivity.class);
                        startActivity(intent);

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

    private CharSequence splitError(final String errMsg, List<String> messages) {
        if (null == errMsg || errMsg.isEmpty())
            return "";

        StringBuilder formatedErrorMessage = new StringBuilder();
        int idx = 0;

        final String[] messagesArray = errMsg.split("\\. ");

        if (null != messages) {
            final List<String> messagesList = Arrays.asList(messagesArray);
            messages.addAll(messagesList);
        }

        for (final String message : messagesArray) {
            formatedErrorMessage.append(message);

            ++idx;
            if (idx != messagesArray.length) // NOT last
                formatedErrorMessage.append('\n');
        }
        return formatedErrorMessage;
    }

    private CharSequence parseError(final String originErrStr) {
        /* Examples:
        Can't connect to server http://www.fxcorporate.com/Hosts.jsp.
        ORA-499: Unable to obtain station descriptor. HTTP request failed object='/Hosts.jsp?ID=2966564702&PN=Demo&SN=ForexConnect&MV=5&LN=D25192327001&AT=PLAIN' errorCode=6
        Description=19915;DAS 19915: ZDas Exception\n ORA-20134: Maximum quantity violated: 100000.\n
        */
        if (null == originErrStr || originErrStr.isEmpty())
            return "Unknown error";

        final String errCodeStrPrefix = "ORA-";
        StringBuilder finalErrMsg = new StringBuilder();

        String errCode       = ""; // ORA-499
        String errMsg        = ""; // Unable to obtain station descriptor
        String errReason     = ""; // HTTP request failed object='/Hosts.jsp?ID=2966564702&PN=Demo&SN=ForexConnect&MV=5&LN=D25192327001&AT=PLAIN'
        String errReasonCode = ""; // 6

        final int errCodeStart = originErrStr.indexOf(errCodeStrPrefix);
        if (errCodeStart >= 0) { // found
            final int errCodeNumberStart = errCodeStart + errCodeStrPrefix.length();
            final int errCodeNumberEnd = originErrStr.indexOf(':', errCodeNumberStart);

            if (errCodeNumberEnd > errCodeNumberStart)
                errCode = originErrStr.substring(errCodeStart, errCodeNumberEnd);

            final int errFullDescrStart = errCodeNumberEnd >= 0 ? (errCodeNumberEnd + 2) : 0;
            final String errFullDescription = originErrStr.substring(errFullDescrStart);

            if (!errFullDescription.isEmpty()) {
                List<String> messages = new ArrayList<>();
                splitError(errFullDescription, messages);

                if (messages.size() > 0)
                    errMsg = messages.get(0);

                if (messages.size() > 1) {
                    errReason = messages.get(1);

                    final String eCodeToken = "errorCode=";
                    final int errReasonCodeStart = errReason.indexOf(eCodeToken);
                    if (errReasonCodeStart >= 0) {
                        errReasonCode = errReason.substring(errReasonCodeStart + eCodeToken.length());
                        errReason = errReason.substring(0, errReasonCodeStart);
                    }
                }
            }
        }
        else  {
            finalErrMsg.append("Error: ");
            final CharSequence msg = splitError(originErrStr, null);
            finalErrMsg.append(msg);

            return finalErrMsg;
        }

        if (!errMsg.isEmpty()) {
            finalErrMsg.append("Error: ");
            finalErrMsg.append(errMsg);
        }

        if (!errCode.isEmpty()) {
            finalErrMsg.append("\n\n");
            finalErrMsg.append("Error code: ");
            finalErrMsg.append(errCode);
        }

        if (!errReason.isEmpty()) {
            finalErrMsg.append("\n\n");
            finalErrMsg.append("Reason: ");
            finalErrMsg.append(errReason);
        }

        if (!errReasonCode.isEmpty()) {
            finalErrMsg.append("\n\n");
            finalErrMsg.append("Reason code: ");
            finalErrMsg.append(errReasonCode);
        }
        return finalErrMsg;
    }

    private class RequestFailedRunnable implements Runnable {
        private String mError;
        private String mRequestID;
        
        public RequestFailedRunnable(String sRequestID, String sError) {
            mRequestID = sRequestID;
            mError = sError;
        }
        
        public void run() {
            final String sText = String.format("Request %s failed. Error - %s", mRequestID, mError);

            StringBuilder errMsgToShow = new StringBuilder();
            errMsgToShow.append("Could not execute request\n\n");
            final CharSequence errMsg = parseError(mError);
            errMsgToShow.append(errMsg);

            final String errMsgToShowConst = errMsgToShow.toString();

            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    showCustomMsgDlg(errMsgToShowConst, false, false, false);
                }
            });
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

    @Override
    protected void onDestroy() {
        mSession.unsubscribeSessionStatus(this);
		mSession.unsubscribeResponse(this);

        super.onDestroy();
    }
}
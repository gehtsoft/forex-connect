package com.FXTSMobile;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Map;

import com.fxcore2.O2GOfferRow;
import com.fxcore2.O2GRequest;
import com.fxcore2.O2GRequestFactory;
import com.fxcore2.O2GRequestParamsEnum;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GValueMap;
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSessionStatusCode;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.Bundle;
import android.util.Pair;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.CompoundButton;
import android.widget.EditText;
import android.widget.RadioButton;
import android.widget.SeekBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.CompoundButton.OnCheckedChangeListener;

public class CreateOrderActivity extends Activity implements OnCheckedChangeListener, SeekBar.OnSeekBarChangeListener, IO2GSessionStatus {
    private Map<String, Pair<BigDecimal, BigDecimal>> mActualPrices;
    private O2GSession mSession;
    private O2GOfferRow mOfferRow;

    private RadioButton mRBBuy;
    private RadioButton mRBSell;

    private EditText mETAmount;
    private EditText mETRate;

    private SeekBar mSBAmount;
    private SeekBar mSBRate;

    private Spinner mSpinnerOrderType;

    private BigDecimal mCurrentRate;

    private boolean mActive = false;

    private class ShowErrDialogAndAct implements Runnable {
        public String errMsg = "Unknown error";

        public boolean doLogout  = false;
        public boolean goToLogin = false;
        public boolean goBack    = false;

        public void setOptions(final String errMsg,
                               final boolean doLogout, final boolean goToLogin, final boolean goBack) {
            this.errMsg = errMsg;

            this.doLogout  = doLogout;
            this.goToLogin = goToLogin;
            this.goBack    = goBack;
        }

        @Override
        public void run() {
            showCustomMsgDlg("Please enter valid positive amount number", doLogout, goToLogin, goBack);
        }
    }

    private void onAmountError() {
        final ShowErrDialogAndAct showErrDialog = new ShowErrDialogAndAct();
        showErrDialog.setOptions("Please enter valid positive amount number",
                                 false, false, false);
        runOnUiThread(showErrDialog);
    }

    public void createOrderHandler(View view) {
        final O2GValueMap valueMap = mSession.getRequestFactory().createValueMap();
        valueMap.setString(O2GRequestParamsEnum.COMMAND, "CreateOrder");

        final boolean bBuy = mRBBuy.isChecked();
        valueMap.setString(O2GRequestParamsEnum.BUY_SELL, bBuy ? "B" : "S");

        final String sSelectedOrderType = mSpinnerOrderType.getSelectedItem().toString();
        final boolean bMarketOrder = sSelectedOrderType.contains("Market");
        if (bMarketOrder) {
            valueMap.setString(O2GRequestParamsEnum.ORDER_TYPE, "OM");
        }
        else {
            final double dRate = Double.parseDouble(mETRate.getText().toString());
            valueMap.setDouble(O2GRequestParamsEnum.RATE, dRate);
            String sLimitOrderType = "";
            if (bBuy) {
                final double dAsk = mActualPrices.get(mOfferRow.getOfferID()).second.doubleValue();
                sLimitOrderType = (dRate > dAsk) ? "SE" : "LE";
            }
            else {
                final double dBid = mActualPrices.get(mOfferRow.getOfferID()).second.doubleValue();
                sLimitOrderType = (dRate < dBid) ? "SE" : "LE";
            }
            valueMap.setString(O2GRequestParamsEnum.ORDER_TYPE, sLimitOrderType);
        }

        valueMap.setString(O2GRequestParamsEnum.OFFER_ID, mOfferRow.getOfferID());
        valueMap.setString(O2GRequestParamsEnum.ACCOUNT_ID, SharedObjects.getInstance().getAccountID());
        
        final String amountStr = mETAmount.getText().toString();
        int iAmountK = 0;
        
        try {
            iAmountK = Integer.parseInt(amountStr);
        } catch (final Throwable t) {
            onAmountError();
            return;
        }
        
        if (iAmountK <= 0) {
            onAmountError();
            return;
        }
        final int iAmount = iAmountK * 1000;
        valueMap.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
        valueMap.setString(O2GRequestParamsEnum.TIME_IN_FORCE, "GTC");
        O2GRequestFactory requestFactory = mSession.getRequestFactory();

        boolean doFinish = true;

        if (requestFactory != null) {
            O2GRequest request = mSession.getRequestFactory().createOrderRequest(valueMap);

            String errorStr = requestFactory.getLastError();
            errorStr = parseError(errorStr).toString();
            final String errorStrConst = errorStr;

            if (request == null) {
                doFinish = false;

                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        String errMsgToShow = "Could not create order\n\nPlease note that you can't create orders on unsubscribed symbols\n";
                        errMsgToShow += "Also check your internet connection and server availability\n\n";
                        errMsgToShow += errorStrConst;

                        showCustomMsgDlg(errMsgToShow, false, false, true);
                    }
                });
            } else {
                mSession.sendRequest(request);
            }
        }
        else { // NO request factory
            doFinish = false;

            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    showCustomMsgDlg("Could not create order\nPlease check your internet connection and server availability\n\n",
                            false, false, true);
                }
            });
        }

        if (doFinish)
            this.finish();
    }

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.create_order);

        Spinner spinner = (Spinner) findViewById(R.id.spinnerOrderType);
        ArrayAdapter<CharSequence> adapter = ArrayAdapter.createFromResource(
                this, R.array.order_types, android.R.layout.simple_spinner_item);
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spinner.setAdapter(adapter);
        spinner.setSelection(0);

        mSession = SharedObjects.getInstance().getSession();

        mActualPrices = SharedObjects.getInstance().getActualPrices();
        mOfferRow = SharedObjects.getInstance().getSelectedOffer();

        mRBBuy = (RadioButton)findViewById(R.id.rbBuy);
        mRBBuy.setTag("B");

        mRBSell = (RadioButton)findViewById(R.id.rbSell);
        mRBSell.setTag("S");

        mRBBuy.setOnCheckedChangeListener(this);
        mRBSell.setOnCheckedChangeListener(this);

        TextView tvSymbol = (TextView)findViewById(R.id.tvSymbol);
        tvSymbol.setText(mOfferRow.getInstrument());

        mETAmount = (EditText)findViewById(R.id.etAmount);
        mETAmount.setText("100");

        mETRate = (EditText)findViewById(R.id.etRate);
        setRate(false);

        mSBAmount = (SeekBar)findViewById(R.id.sbAmount);
        mSBAmount.setOnSeekBarChangeListener(this);

        mSBRate = (SeekBar)findViewById(R.id.sbRate);
        mSBRate.setOnSeekBarChangeListener(this);

        mSpinnerOrderType = (Spinner)findViewById(R.id.spinnerOrderType);

        mSession.subscribeSessionStatus(this);
    }

    private void setRate(boolean bBid) {
        Pair<BigDecimal, BigDecimal> pair = mActualPrices.get(mOfferRow.getOfferID());
        BigDecimal bdPrice = (bBid) ? pair.first : pair.second;
        mCurrentRate = bdPrice;
        mETRate.setText(mCurrentRate.toString());
    }

    public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
        if (buttonView instanceof RadioButton) {
            if (isChecked) {
                if ("B".equals(buttonView.getTag().toString())) {
                    mRBSell.setChecked(false);
                    setRate(false);
                }
                else {
                    mRBBuy.setChecked(false);
                    setRate(true);
                }
                mSBRate.setProgress(50);
            }
        }
    }

    public void onProgressChanged(SeekBar seekBar, int progress,
                                  boolean fromUser) {
        if (seekBar.equals(mSBAmount)) {
            int iAmount = (progress + 1) * 10;
            mETAmount.setText(Integer.toString(iAmount));
        }
        else if (seekBar.equals(mSBRate)) {
            int iProgress = (progress + 1) - 50;
            BigDecimal changedPrice = mCurrentRate.add(BigDecimal.valueOf(iProgress * mOfferRow.getPointSize()));
            changedPrice = changedPrice.setScale(mOfferRow.getDigits(), RoundingMode.HALF_UP);
            mETRate.setText(changedPrice.toString());
        }
    }

    public void onStartTrackingTouch(SeekBar seekBar) {
    }

    public void onStopTrackingTouch(SeekBar seekBar) {
    }

    @Override
    public void onBackPressed() {
        this.finish();
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
        */
        if (null == originErrStr || originErrStr.isEmpty())
            return "Unknown error";

        final String errCodeStrPrefix = "ORA-";
        StringBuilder finalErrMsg = new StringBuilder();

        final int errCodeStart = originErrStr.indexOf(errCodeStrPrefix);
        if (errCodeStart >= 0) { // found
            String errCode       = "unknown"; // ORA-499
            String errMsg        = "unknown"; // Unable to obtain station descriptor
            String errReason     = "unknown"; // HTTP request failed object='/Hosts.jsp?ID=2966564702&PN=Demo&SN=ForexConnect&MV=5&LN=D25192327001&AT=PLAIN'
            String errReasonCode = "unknown"; // 6

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

            if (errCode.isEmpty())
                errCode = "unknown";
            if (errMsg.isEmpty())
                errMsg = "unknown";
            if (errReason.isEmpty())
                errReason = "unknown";
            if (errReasonCode.isEmpty())
                errReasonCode = "unknown";

            finalErrMsg.append("Error: ");
            finalErrMsg.append(errMsg);
            finalErrMsg.append("\n\n");

            finalErrMsg.append("Error code: ");
            finalErrMsg.append(errCode);
            finalErrMsg.append("\n\n");

            finalErrMsg.append("Reason: ");
            finalErrMsg.append(errReason);
            finalErrMsg.append("\n\n");

            finalErrMsg.append("Reason code: ");
            finalErrMsg.append(errReasonCode);
        }
        else  {
            finalErrMsg.append("Error: ");
            final CharSequence msg = splitError(originErrStr, null);
            finalErrMsg.append(msg);
        }
        return finalErrMsg;
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
        super.onDestroy();
    }
}

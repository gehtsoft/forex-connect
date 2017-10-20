package com.FXTSMobile;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.Map;

import com.fxcore2.O2GOfferRow;
import com.fxcore2.O2GRequest;
import com.fxcore2.O2GRequestFactory;
import com.fxcore2.O2GRequestParamsEnum;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GValueMap;

import android.app.Activity;
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
import android.widget.Toast;
import android.widget.CompoundButton.OnCheckedChangeListener;

public class CreateOrderActivity extends Activity implements OnCheckedChangeListener, SeekBar.OnSeekBarChangeListener{
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
    
    public void createOrderHandler(View view) {
        O2GValueMap valueMap = mSession.getRequestFactory().createValueMap();
        valueMap.setString(O2GRequestParamsEnum.COMMAND, "CreateOrder");
        
        boolean bBuy = mRBBuy.isChecked();
        valueMap.setString(O2GRequestParamsEnum.BUY_SELL, bBuy ? "B" : "S");
        
        String sSelectedOrderType = mSpinnerOrderType.getSelectedItem().toString();
        boolean bMarketOrder = sSelectedOrderType.contains("Market");
        if (bMarketOrder) {
            valueMap.setString(O2GRequestParamsEnum.ORDER_TYPE, "OM");
        }
        else {
            double dRate = Double.parseDouble(mETRate.getText().toString());
            valueMap.setDouble(O2GRequestParamsEnum.RATE, dRate);
            String sLimitOrderType = "";
            if (bBuy) {
                double dAsk = mActualPrices.get(mOfferRow.getOfferID()).second.doubleValue();
                sLimitOrderType = (dRate > dAsk) ? "SE" : "LE";
            }
            else {
                double dBid = mActualPrices.get(mOfferRow.getOfferID()).second.doubleValue();
                sLimitOrderType = (dRate < dBid) ? "SE" : "LE";
            }
            valueMap.setString(O2GRequestParamsEnum.ORDER_TYPE, sLimitOrderType);
        }
        
        valueMap.setString(O2GRequestParamsEnum.OFFER_ID, mOfferRow.getOfferID());
        valueMap.setString(O2GRequestParamsEnum.ACCOUNT_ID, SharedObjects.getInstance().getAccountID());
        int iAmountK = Integer.parseInt(mETAmount.getText().toString());
        int iAmount = iAmountK * 1000;
        valueMap.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
        valueMap.setString(O2GRequestParamsEnum.TIME_IN_FORCE, "GTC");
        O2GRequestFactory requestFactory = mSession.getRequestFactory();
        if (requestFactory != null) {
            O2GRequest request = mSession.getRequestFactory().createOrderRequest(valueMap);
            if (request == null) {
                Toast.makeText(CreateOrderActivity.this, requestFactory.getLastError(), Toast.LENGTH_SHORT).show();
            } else {
                mSession.sendRequest(request);
            }
        }
        this.finish();
    }
}

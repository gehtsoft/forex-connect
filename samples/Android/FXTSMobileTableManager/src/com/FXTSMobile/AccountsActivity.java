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
import com.fxcore2.O2GAccountTableRow;
import com.fxcore2.O2GAccountsTable;
import com.fxcore2.O2GRow;
import com.fxcore2.O2GTableStatus;
import com.fxcore2.O2GTableType;
import com.fxcore2.O2GTableUpdateType;

public class AccountsActivity extends Activity implements IO2GTableListener, IO2GEachRowListener {
    private final int STANDARD_MARGIN = 5;
    private Handler mHandler = new Handler();
    private TableLayout mTableLayout;
    private Map<String, TableRow> mAccountTableRows;
    private O2GAccountsTable mAccountsTable;
    private DecimalFormat mMoneyFormat;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.accounts);
        mTableLayout = (TableLayout) findViewById(R.id.tablelayout);
        mAccountTableRows = new HashMap<String, TableRow>();
        initializeMoneyFormat();
        initializeAccountsTable();
    }

    private void initializeMoneyFormat() {
        mMoneyFormat = new DecimalFormat();
        mMoneyFormat.setMaximumFractionDigits(2);
        mMoneyFormat.setMinimumFractionDigits(2);
        mMoneyFormat.setDecimalSeparatorAlwaysShown(true);
    }

    private void initializeAccountsTable() {
        //ACCOUNTS table depends on TRADES table, so it must be refreshed already
        mAccountsTable = (O2GAccountsTable) SharedObjects.getInstance()
                .getSession().getTableManager().getTable(O2GTableType.ACCOUNTS);
        mAccountsTable.subscribeUpdate(O2GTableUpdateType.UPDATE, this);
        mAccountsTable.forEachRow(this);
    }

    @Override
    public void onAdded(String rowID, O2GRow rowData) {
        // TODO Auto-generated method stub

    }

    @Override
    public void onChanged(String rowID, O2GRow rowData) {
        UpdateAccountsRunnable runnable = new UpdateAccountsRunnable(
                (O2GAccountTableRow) rowData);
        mHandler.post(runnable);
    }

    @Override
    public void onDeleted(String rowID, O2GRow rowData) {
        // TODO Auto-generated method stub

    }

    @Override
    public void onStatusChanged(O2GTableStatus status) {
        // TODO Auto-generated method stub

    }

    @Override
    public void onEachRow(String rowID, O2GRow rowData) {
        O2GAccountTableRow row = (O2GAccountTableRow) rowData;

        TableRow tableRow = new TableRow(this);

        TextView tvAccountID = new TextView(this);
        tvAccountID.setPadding(0, STANDARD_MARGIN, 0, STANDARD_MARGIN);
        tvAccountID.setText(row.getAccountID());
        tvAccountID.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvAccountID);

        TextView tvBalance = new TextView(this);
        tvBalance.setText(mMoneyFormat.format(row.getBalance()));
        tvBalance.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvBalance.setGravity(Gravity.RIGHT);
        tvBalance.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvBalance);

        TextView tvEquity = new TextView(this);
        tvEquity.setText(mMoneyFormat.format(row.getEquity()));
        tvEquity.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvEquity.setGravity(Gravity.RIGHT);
        tvEquity.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvEquity);

        TextView tvDayPL = new TextView(this);
        tvDayPL.setText(mMoneyFormat.format(row.getDayPL()));
        tvDayPL.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvDayPL.setGravity(Gravity.RIGHT);
        tvDayPL.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvDayPL);

        TextView tvGrossPL = new TextView(this);
        tvGrossPL.setText(mMoneyFormat.format(row.getGrossPL()));
        tvGrossPL.setPadding(STANDARD_MARGIN, 0, STANDARD_MARGIN, 0);
        tvGrossPL.setGravity(Gravity.RIGHT);
        tvGrossPL.setLayoutParams(new TableRow.LayoutParams());
        tableRow.addView(tvGrossPL);

        mAccountTableRows.put(row.getAccountID(), tableRow);

        mTableLayout.addView(tableRow, new TableLayout.LayoutParams(
                TableLayout.LayoutParams.FILL_PARENT,
                TableLayout.LayoutParams.WRAP_CONTENT));
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        mAccountsTable.unsubscribeUpdate(O2GTableUpdateType.UPDATE, this);
    }

    private class UpdateAccountsRunnable implements Runnable {

        private String mAccountID;
        private double mBalance;
        private double mEquity;
        private double mDayPL;
        private double mGrossPL;

        public UpdateAccountsRunnable(O2GAccountTableRow accountRow) {
            mAccountID = accountRow.getAccountID();
            mBalance = accountRow.getBalance();
            mEquity = accountRow.getEquity();
            mDayPL = accountRow.getDayPL();
            mGrossPL = accountRow.getGrossPL();
        }

        public void run() {
            TableRow tableRow = mAccountTableRows.get(mAccountID);

            if (tableRow == null) {
                return;
            }

            ((TextView) tableRow.getChildAt(1)).setText(mMoneyFormat.format(mBalance));
            ((TextView) tableRow.getChildAt(2)).setText(mMoneyFormat.format(mEquity));
            ((TextView) tableRow.getChildAt(3)).setText(mMoneyFormat.format(mDayPL));
            ((TextView) tableRow.getChildAt(4)).setText(mMoneyFormat.format(mGrossPL));
        }
    }

}
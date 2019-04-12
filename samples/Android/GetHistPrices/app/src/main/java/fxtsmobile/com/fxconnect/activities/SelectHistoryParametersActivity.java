package fxtsmobile.com.fxconnect.activities;

import android.app.DatePickerDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.support.v7.app.AlertDialog;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.DatePicker;
import android.widget.EditText;
import android.widget.Spinner;;
import android.widget.TextView;
import android.widget.Toast;

import com.fxcore2.IO2GResponseListener;
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GOfferRow;
import com.fxcore2.O2GOffersTableResponseReader;
import com.fxcore2.O2GRequest;
import com.fxcore2.O2GRequestFactory;
import com.fxcore2.O2GResponse;
import com.fxcore2.O2GResponseReaderFactory;
import com.fxcore2.O2GResponseType;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GSessionStatusCode;
import com.fxcore2.O2GTableManager;
import com.fxcore2.O2GTableManagerStatus;
import com.fxcore2.O2GTableType;
import com.fxcore2.O2GTimeframeCollection;
import com.fxcore2.O2GTimeframe;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;
import java.util.Locale;

import fxtsmobile.com.fxconnect.R;
import fxtsmobile.com.fxconnect.model.SharedObjects;

public class SelectHistoryParametersActivity extends AppCompatActivity
                                             implements DatePickerDialog.OnDateSetListener, IO2GSessionStatus, IO2GResponseListener {

    private static final String DATE_FORMAT_TEMPLATE = "MMM dd, yyyy";

    private TextView SymbolValueTextView;
    private TextView startDateValueTextView;
    private TextView endDateValueTextView;
    private TextView TimeframeValueTextView;

    private Spinner SymbolSpinner;
    private Spinner TimeframeSpinner;

    private ViewGroup startDateLayout;
    private ViewGroup endDateLayout;

    private EditText MaxBarsNumEditText;

    private Button logoutButton;
    private Button showHistoryButton;

    private O2GSession mSession;
    private SharedPreferences mPreferences;

    private int dateType = 0;
    private boolean mActive = false;

    private Calendar startDateCalendar;
    private Calendar endDateCalendar;

    private O2GRequestFactory requestFactory;
    private O2GResponseReaderFactory responseFactory;

    private List<String> timeFrames = new ArrayList<>();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_select_history_parameters);

        dateType = 0;

        setTitle(R.string.title_activity_select_parameters);

        mSession = SharedObjects.getInstance().getSession();

        SymbolValueTextView = findViewById(R.id.SymbolValueTextView);
        startDateValueTextView = findViewById(R.id.startDateValueTextView);
        endDateValueTextView = findViewById(R.id.endDateValueTextView);
        TimeframeValueTextView = findViewById(R.id.TimeframeValueTextView);

        SymbolSpinner = findViewById(R.id.SymbolSpinner);
        TimeframeSpinner = findViewById(R.id.TimeframeSpinner);

        startDateLayout = findViewById(R.id.startDateLayout);
        endDateLayout = findViewById(R.id.endDateLayout);

        MaxBarsNumEditText = findViewById(R.id.MaxBarsNum);

        logoutButton = findViewById(R.id.logoutButton);
        showHistoryButton = findViewById(R.id.showHistoryButton);

        LoadPreferences();

        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeSessionStatus(this);

        mSession.subscribeResponse(this);
        responseFactory = mSession.getResponseReaderFactory();

        requestFactory = mSession.getRequestFactory();
        final O2GRequest refreshOffersRequest = requestFactory.createRefreshTableRequest(O2GTableType.OFFERS);
        mSession.sendRequest(refreshOffersRequest);

        setupFields();
    }

    @Override
    public void onBackPressed() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage("Are you sure you want to logout?")
                .setCancelable(false)
                .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int id) {
                        mSession.unsubscribeSessionStatus(SelectHistoryParametersActivity.this);
                        mSession.unsubscribeResponse(SelectHistoryParametersActivity.this);
                        mSession.logout();

                        Intent intent = new Intent(SelectHistoryParametersActivity.this, LoginActivity.class);
                        startActivity(intent);

                        SelectHistoryParametersActivity.this.finish();
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

    private synchronized void updateInstrumentsList(final O2GResponse response) {
        List<String> instruments = new ArrayList<>();
        final O2GOffersTableResponseReader reader = responseFactory.createOffersTableReader(response);

        for (int i = 0; i < reader.size(); ++i) {
            final O2GOfferRow row = reader.getRow(i);
            final String instrument = row.getInstrument();

            instruments.add(instrument);
        }
        final List<String> instrumentsConst = instruments;
        
        runOnUiThread(new Runnable() {

            @Override
            public void run() {
                final ArrayAdapter<String> instrumentsAdapter = new ArrayAdapter<>(SelectHistoryParametersActivity.this,
                                                                                    android.R.layout.simple_spinner_item, instrumentsConst);
                SymbolSpinner.setAdapter(instrumentsAdapter);

                SymbolSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
                    @Override
                    public void onItemSelected(AdapterView<?> adapterView, View view, int i, long l) {
                        setPickerValue(instrumentsConst.get(i), SymbolValueTextView);
                    }

                    @Override
                    public void onNothingSelected(AdapterView<?> adapterView) {
                    }
                });
            }
        });
    }

    public void onRequestCompleted(final String requestID, final O2GResponse response) {
        final O2GResponseType type = response.getType();

        if (type == O2GResponseType.GET_OFFERS) {
            updateInstrumentsList(response);

            mSession.unsubscribeResponse(this);
        }
    }

    public void onRequestFailed(String requestID, String error) {
        runOnUiThread(new Runnable() {

            @Override
            public void run() {
                showCustomMsgDlg("Failed to load data from the server\nPlease check your internet connection and server availability",
                        true, true, false);
            }
        });
    }

    public void onTablesUpdates(O2GResponse response) {
    }

    private void LoadPreferences() {
        mPreferences = getPreferences(Context.MODE_PRIVATE);

        final String sLastSymbol = mPreferences.getString("symbol", "EUR/USD");
        final String sLastTimeframe = mPreferences.getString("timeframe", "H1");
        final String sLastMaxBars = mPreferences.getString("maxBars", "100");
    }

    private void commitPreferences() {
        final String sLastSymbol = SymbolValueTextView.getText().toString();
        final String sLastTimeframe = TimeframeValueTextView.getText().toString();
        final String sLastMaxBars = MaxBarsNumEditText.getText().toString();

        SharedPreferences.Editor editor = mPreferences.edit();
        editor.putString("symbol", sLastSymbol);
        editor.putString("timeframe", sLastTimeframe);
        editor.putString("maxBars", sLastMaxBars);

        editor.commit();
    }

    private void showHistory() {
        String instrument = SymbolValueTextView.getText().toString();
        instrument = instrument.replace(" \uFF1E", "");
        HistoricPricesActivity.instrument = instrument;

        String timeFrameId = TimeframeValueTextView.getText().toString();
        timeFrameId = timeFrameId.replace(" \uFF1E", "");
        HistoricPricesActivity.timeFrameId = timeFrameId;

        boolean error = false;
        final String maxBarsStr = MaxBarsNumEditText.getText().toString();
        int maxBars = 0;
        try {
            maxBars = Integer.parseInt(maxBarsStr);
        }
        catch (NumberFormatException e) {
            error = true;
        }

        if (error || maxBars <= 0) {
            runOnUiThread(new Runnable() {

                @Override
                public void run() {
                    showCustomMsgDlg("Please enter valid positive number as a max. bars count",
                            false, false, false);
                }
            });
            return;
        }
        HistoricPricesActivity.maxBars = maxBars;
        HistoricPricesActivity.startDateCalendar = startDateCalendar;
        HistoricPricesActivity.endDateCalendar = endDateCalendar;

        Intent intent = new Intent(this, HistoricPricesActivity.class);
        startActivity(intent);

        commitPreferences();
        finish();
    }

    private void setPickerValue(String value, TextView valueTextView) {
        value += " \uFF1E";
        valueTextView.setText(value);
    }

    private void waitTableManagerRefresh() {
        O2GTableManager tableManger = mSession.getTableManager();
        while (tableManger.getStatus() != O2GTableManagerStatus.TABLES_LOADED &&
                tableManger.getStatus() != O2GTableManagerStatus.TABLES_LOAD_FAILED) {
            try {
                Thread.sleep(50);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        }
    }

    private void setupStartDate() {
        startDateCalendar = Calendar.getInstance();
        startDateCalendar.add(Calendar.MONTH, -1);
        SimpleDateFormat simpleDateFormat = new SimpleDateFormat(DATE_FORMAT_TEMPLATE, Locale.getDefault());
        String date = simpleDateFormat.format(startDateCalendar.getTime());
        setPickerValue(date, startDateValueTextView);

        startDateLayout.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                int year = startDateCalendar.get(Calendar.YEAR);
                int month = startDateCalendar.get(Calendar.MONTH);
                int day = startDateCalendar.get(Calendar.DAY_OF_MONTH);

                dateType = 1;
                new DatePickerDialog(SelectHistoryParametersActivity.this, SelectHistoryParametersActivity.this, year, month, day).show();
            }
        });
    }

    private void setupEndDate() {
        endDateCalendar = Calendar.getInstance();
        SimpleDateFormat simpleDateFormat = new SimpleDateFormat(DATE_FORMAT_TEMPLATE, Locale.getDefault());
        String date = simpleDateFormat.format(endDateCalendar.getTime());
        setPickerValue(date, endDateValueTextView);

        endDateLayout.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                int year = endDateCalendar.get(Calendar.YEAR);
                int month = endDateCalendar.get(Calendar.MONTH);
                int day = endDateCalendar.get(Calendar.DAY_OF_MONTH);

                dateType = 2;
                new DatePickerDialog(SelectHistoryParametersActivity.this, SelectHistoryParametersActivity.this, year, month, day).show();
            }
        });
    }

    private void setupButtons() {
        logoutButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mSession.unsubscribeSessionStatus(SelectHistoryParametersActivity.this);
                SharedObjects.getInstance().getSession().logout();

                Intent loginIntent = new Intent(SelectHistoryParametersActivity.this, LoginActivity.class);
                startActivity(loginIntent);
                finish();
            }
        });

        showHistoryButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                showHistory();
            }
        });
    }

    private void setupTimeFrames() {
        final O2GTimeframeCollection timeFrameCollection = requestFactory.getTimeFrameCollection();
        for (final O2GTimeframe frame : timeFrameCollection) {
            final String timeFrame = frame.getId();
            final boolean isT1 = timeFrame.contains("t1") || timeFrame.contains("T1");
            if (false == isT1)
                timeFrames.add(timeFrame);
        }

        ArrayAdapter<String> timeframesAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, timeFrames);
        TimeframeSpinner.setAdapter(timeframesAdapter);
        TimeframeSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> adapterView, View view, int i, long l) {
                final String timeFrame = timeFrames.get(i);
                setPickerValue(timeFrame, TimeframeValueTextView);
            }

            @Override
            public void onNothingSelected(AdapterView<?> adapterView) {
            }
        });
    }

    private void setupFields() {
        waitTableManagerRefresh();

        setupStartDate();
        setupEndDate();
        setupTimeFrames();

        setupButtons();
    }

    @Override
    protected void onPause() {
        mActive = false;
        super.onPause();
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

    private void showCustomMsgDlg(final String msg, final boolean logout, final boolean goToLogin, final boolean goBack) {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);

        final Intent intent = goToLogin ? new Intent(this, LoginActivity.class) : null;

        DialogInterface.OnClickListener listener = new DialogInterface.OnClickListener() {
            public void onClick(DialogInterface dialog, int id) {
                if (logout && mSession != null) {
                    mSession.logout();
                }
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
    public void onDateSet(DatePicker datePicker, int i, int i1, int i2) {

        Calendar newCalendar = Calendar.getInstance();
        newCalendar.set(i, i1, i2);

        if (1 == dateType && newCalendar.getTimeInMillis() >= endDateCalendar.getTimeInMillis()) {

            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    Toast.makeText(getApplicationContext(), "Start date must be less than the end date", Toast.LENGTH_SHORT).show();
                }
            });
            return;
        }
        else if (2 == dateType && newCalendar.getTimeInMillis() <= startDateCalendar.getTimeInMillis()) {

            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    Toast.makeText(getApplicationContext(), "End date must be greater than the start date", Toast.LENGTH_SHORT).show();
                }
            });
            return;
        }

        SimpleDateFormat simpleDateFormat = new SimpleDateFormat(DATE_FORMAT_TEMPLATE, Locale.getDefault());
        if (1 == dateType) {
            startDateCalendar = newCalendar;
            String date = simpleDateFormat.format(startDateCalendar.getTime());
            setPickerValue(date, startDateValueTextView);
        }
        else if (2 == dateType) {
            endDateCalendar = newCalendar;
            String date = simpleDateFormat.format(endDateCalendar.getTime());
            setPickerValue(date, endDateValueTextView);
        }
    }

    @Override
    protected void onDestroy() {
        mSession.unsubscribeSessionStatus(this);
        mSession.unsubscribeResponse(this);

        super.onDestroy();
    }
}

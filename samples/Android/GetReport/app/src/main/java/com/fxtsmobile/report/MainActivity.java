package com.fxtsmobile.report;

import android.app.DatePickerDialog;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.URLUtil;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.DatePicker;
import android.widget.Spinner;
import android.widget.TextView;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.widget.Toast;

import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GAccountRow;
import com.fxcore2.O2GAccountsTableResponseReader;
import com.fxcore2.O2GLoginRules;
import com.fxcore2.O2GResponse;
import com.fxcore2.O2GResponseReaderFactory;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GSessionStatusCode;
import com.fxcore2.O2GTableType;
import com.fxtsmobile.report.model.ReportField;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Calendar;
import java.util.List;
import java.util.Locale;

public class MainActivity extends AppCompatActivity implements DatePickerDialog.OnDateSetListener, IO2GSessionStatus {

    private static final String DATE_FORMAT_TEMPLATE = "MMM dd, yyyy";
    private static final String REPORT_TYPE = "REPORT_NAME_CUSTOMER_ACCOUNT_STATEMENT";

    private O2GSession mSession;

    private TextView accountIdValueTextView;
    private TextView startDateValueTextView;
    private TextView endDateValueTextView;
    private TextView reportFormatValueTextView;
    private TextView reportLanguageValueTextView;
    private Spinner accountIdSpinner;
    private Spinner reportFormatSpinner;
    private Spinner reportLanguageSpinner;
    private ViewGroup startDateLayout;
    private ViewGroup endDateLayout;
    private Button logoutButton;
    private Button showReportButton;

    List<O2GAccountRow> accountRows = new ArrayList<>();

    private O2GAccountRow selectedAccountRow;
    private Calendar startDateCalendar;
    private Calendar endDateCalendar;
    private ReportField selectedReportFormat;
    private ReportField selectedReportLanguage;

    private int dateType = 0;
    private boolean mActive = false;

    private List<ReportField> reportFormats = Arrays.asList(
            new ReportField("HTML", "HTML format"),
            new ReportField("XML", "XML format"),
            new ReportField("PDF", "PDF format"),
            new ReportField("XML", "Excel format"));

    private List<ReportField> reportLanguages = Arrays.asList(
            new ReportField("enu", "English"),
            new ReportField("jpn", "Japanese"),
            new ReportField("fra", "French"),
            new ReportField("esp", "Spanish"),
            new ReportField("cht", "Chinese Traditional"),
            new ReportField("chs", "Chinese Simplified"),
            new ReportField("rus", "Russian"));

    private void showReport() {
        if (selectedAccountRow == null) {
            return;
        }
        O2GSession session = SharedObjects.getInstance().getSession();
        String url = session.getReportURL(selectedAccountRow.getAccountID(), startDateCalendar, endDateCalendar, selectedReportFormat.getValue(),
                selectedReportLanguage.getValue());

        final boolean validUrl = URLUtil.isValidUrl(url);
        if (null == url || url.isEmpty() || !validUrl) {
            final String urlConst = url;

            runOnUiThread(new Runnable() {
                @Override
                public void run() {

                    if (null == urlConst || urlConst.isEmpty())
                        showCustomMsgDlg("Could not get report URL, please check report parameters", false, false, false);
                    else { // invalid URL
                        String errMsgToShow = "Error:\n\n";
                        errMsgToShow += urlConst; // assuming it contains error message

                        showCustomMsgDlg(errMsgToShow, false, false, false);
                    }
                }
            });
            return;
        }
        Intent urlIntent = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
        startActivity(urlIntent);
    }
	
    private List<O2GAccountRow> getAccountRows() {
        O2GSession session = SharedObjects.getInstance().getSession();
        O2GLoginRules loginRules = session.getLoginRules();

        if (loginRules == null) {
            return new ArrayList<>();
        }

        O2GResponseReaderFactory responseReaderFactory = session.getResponseReaderFactory();
        O2GResponse tableRefreshResponse = loginRules.getTableRefreshResponse(O2GTableType.ACCOUNTS);
        O2GAccountsTableResponseReader accountsTableReader = responseReaderFactory.createAccountsTableReader(tableRefreshResponse);
        int size = accountsTableReader.size();

        List<O2GAccountRow> rows = new ArrayList<>();

        for (int i = 0; i < size; i++) {
            O2GAccountRow row = accountsTableReader.getRow(i);
            rows.add(row);
        }

        return rows;
    }

    private void setupFields() {
        if (accountRows.isEmpty()) {
            return;
        }

        setupAccounts();
        setupStartDate();
        setupEndDate();
        setupReportFormats();
        setupReportLanguages();
        setupButtons();
    }

    private void setupAccounts() {
        selectedAccountRow = accountRows.get(0);

        if (accountRows.size() > 1) {
            List<String> accountIds = new ArrayList<>();
            for (O2GAccountRow row : accountRows) {
                accountIds.add(row.getAccountID());
            }

            ArrayAdapter<String> accountIdsAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, accountIds);
            accountIdSpinner.setAdapter(accountIdsAdapter);
            accountIdSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
                @Override
                public void onItemSelected(AdapterView<?> adapterView, View view, int i, long l) {
                    selectedAccountRow = accountRows.get(i);
                    String accountId = selectedAccountRow.getAccountID();
                    setPickerValue(accountId, accountIdValueTextView);
                }

                @Override
                public void onNothingSelected(AdapterView<?> adapterView) {
                }
            });
        } else {
            String accountId = selectedAccountRow.getAccountID();
            accountIdValueTextView.setText(accountId);
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
                new DatePickerDialog(MainActivity.this, MainActivity.this, year, month, day)
                        .show();
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
                new DatePickerDialog(MainActivity.this, MainActivity.this, year, month, day)
                        .show();
            }
        });
    }

    private void setupReportFormats() {
        selectedReportFormat = reportFormats.get(0);

        List<String> reportFormatsData = new ArrayList<>();
        for (ReportField reportField : reportFormats) {
            reportFormatsData.add(reportField.getDescription());
        }

        ArrayAdapter<String> reportFormatsAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, reportFormatsData);
        reportFormatSpinner.setAdapter(reportFormatsAdapter);
        reportFormatSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> adapterView, View view, int i, long l) {
                selectedReportFormat = reportFormats.get(i);
                setPickerValue(selectedReportFormat.getDescription(), reportFormatValueTextView);
            }

            @Override
            public void onNothingSelected(AdapterView<?> adapterView) {
            }
        });
    }


    private void setupReportLanguages() {
        selectedReportLanguage = reportFormats.get(0);

        List<String> reportLanguagesData = new ArrayList<>();
        for (ReportField reportField : reportLanguages) {
            reportLanguagesData.add(reportField.getDescription());
        }

        ArrayAdapter<String> reportLanguagesAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, reportLanguagesData);
        reportLanguageSpinner.setAdapter(reportLanguagesAdapter);
        reportLanguageSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> adapterView, View view, int i, long l) {
                selectedReportLanguage = reportLanguages.get(i);
                setPickerValue(selectedReportLanguage.getDescription(), reportLanguageValueTextView);
            }

            @Override
            public void onNothingSelected(AdapterView<?> adapterView) {
            }
        });
    }

    private void setupButtons() {
        logoutButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mSession.unsubscribeSessionStatus(MainActivity.this);
                SharedObjects.getInstance().getSession().logout();

                Intent loginIntent = new Intent(MainActivity.this, LoginActivity.class);
                startActivity(loginIntent);
                finish();
            }
        });

        showReportButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                showReport();
            }
        });
    }
	
    private void setPickerValue(String value, TextView valueTextView) {
        value += " \uFF1E";
        valueTextView.setText(value);
    }

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
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        dateType = 0;

        accountIdValueTextView = findViewById(R.id.accountIdValueTextView);
        startDateValueTextView = findViewById(R.id.startDateValueTextView);
        endDateValueTextView = findViewById(R.id.endDateValueTextView);
        reportFormatValueTextView = findViewById(R.id.reportFormatValueTextView);
        reportLanguageValueTextView = findViewById(R.id.reportLanguageValueTextView);
        accountIdSpinner = findViewById(R.id.accountIdSpinner);
        reportFormatSpinner = findViewById(R.id.reportFormatSpinner);
        reportLanguageSpinner = findViewById(R.id.reportLanguageSpinner);
        startDateLayout = findViewById(R.id.startDateLayout);
        endDateLayout = findViewById(R.id.endDateLayout);
        logoutButton = findViewById(R.id.logoutButton);
        showReportButton = findViewById(R.id.showReportButton);

        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeSessionStatus(this);

        accountRows = getAccountRows();
        setupFields();

        setTitle("Get Report");
    }

    @Override
    public void onBackPressed() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage("Are you sure you want to logout?")
                .setCancelable(false)
                .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int id) {
                        mSession.unsubscribeSessionStatus(MainActivity.this);
                        mSession.logout();

                        Intent intent = new Intent(MainActivity.this, LoginActivity.class);
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

    @Override
    protected void onPause() {
        mActive = false;
        super.onPause();
    }

    private void showCustomMsgDlg(final String msg, final boolean logout, final boolean goToLogin, final boolean goBack) {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);

        final Intent intent = goToLogin ? new Intent(this, LoginActivity.class) : null;

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
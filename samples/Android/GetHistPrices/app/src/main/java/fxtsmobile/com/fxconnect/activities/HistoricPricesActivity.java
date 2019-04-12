package fxtsmobile.com.fxconnect.activities;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.Bundle;
import android.support.design.widget.TabLayout;
import android.support.v4.view.ViewPager;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.widget.ProgressBar;

import com.fxcore2.IO2GResponseListener;
import com.fxcore2.O2GCandleOpenPriceMode;
import com.fxcore2.O2GMarketDataSnapshotResponseReader;
import com.fxcore2.O2GRequest;
import com.fxcore2.O2GRequestFactory;
import com.fxcore2.O2GResponse;
import com.fxcore2.O2GResponseReaderFactory;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GTimeframe;
import com.fxcore2.O2GTimeframeCollection;
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSessionStatusCode;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Calendar;
import java.util.List;

import fxtsmobile.com.fxconnect.R;
import fxtsmobile.com.fxconnect.adapters.HistoricPricesViewPagerAdapter;
import fxtsmobile.com.fxconnect.model.BarItem;
import fxtsmobile.com.fxconnect.model.CandleChartItem;
import fxtsmobile.com.fxconnect.model.HistoricPricesRepository;
import fxtsmobile.com.fxconnect.model.SharedObjects;
import fxtsmobile.com.fxconnect.model.VolumeItem;

public class HistoricPricesActivity extends AppCompatActivity implements IO2GResponseListener, IO2GSessionStatus {
    public static String instrument = "EUR/USD";
    public static String timeFrameId = "H1";
    public static int maxBars = 100;
    public static Calendar startDateCalendar;
    public static Calendar endDateCalendar;

    private ProgressBar progressBar;
    private TabLayout tabLayout;
    private ViewPager viewPager;

    private O2GSession session;
    private O2GRequestFactory requestFactory;
    private O2GResponseReaderFactory responseFactory;
    private O2GRequest marketDataRequest;

    private boolean mActive = false;

    private void requestHistoricPrices() {
        progressIndication(true);

        final O2GTimeframeCollection timeFrameCollection = requestFactory.getTimeFrameCollection();
        O2GTimeframe frameSelected = null;
        for (final O2GTimeframe frame : timeFrameCollection) {
            final String currId = frame.getId();
            if (currId.equals(timeFrameId))
                frameSelected = frame;
        }
        if (null == frameSelected) {
            runOnUiThread(new Runnable() {

                @Override
                public void run() {
                    showCustomMsgDlg("Internal error: invalid frame selected",
                            false, false, true);
                }
            });
            return;
        }
        marketDataRequest = requestFactory.createMarketDataSnapshotRequestInstrument(instrument, frameSelected, maxBars);
        requestFactory.fillMarketDataSnapshotRequestTime(marketDataRequest, startDateCalendar, endDateCalendar, false, O2GCandleOpenPriceMode.PREVIOUS_CLOSE);

        session.subscribeResponse(this);
        session.sendRequest(marketDataRequest);
    }


    private boolean canReadRequest(String responseId) {
        return marketDataRequest != null && marketDataRequest.getRequestId().equals(responseId);
    }

    @Override
    public void onRequestCompleted(String s, O2GResponse o2GResponse) {
        if (!canReadRequest(s)) {
            return;
        }
        session.unsubscribeResponse(this);

        O2GMarketDataSnapshotResponseReader marketSnapshotReader = responseFactory.createMarketDataSnapshotReader(o2GResponse);

        List<CandleChartItem> historicPriceChartItems = new ArrayList<>();
        List<VolumeItem> historicPriceTableItems = new ArrayList<>();

        for (int i = 0; i < marketSnapshotReader.size(); i++) {
            Calendar calendar = marketSnapshotReader.getDate(i);

            double askLow = marketSnapshotReader.getAskLow(i);
            double askClose = marketSnapshotReader.getAskClose(i);
            double ask = marketSnapshotReader.getAsk(i);
            double askOpen = marketSnapshotReader.getAskOpen(i);
            double askHigh = marketSnapshotReader.getAskHigh(i);
            BarItem askItem = new BarItem(askLow, askClose, ask, askOpen, askHigh);

            double bidLow = marketSnapshotReader.getBidLow(i);
            double bidClose = marketSnapshotReader.getBidClose(i);
            double bid = marketSnapshotReader.getBid(i);
            double bidOpen = marketSnapshotReader.getBidOpen(i);
            double bidHigh = marketSnapshotReader.getBidHigh(i);
            BarItem bidItem = new BarItem(bidLow, bidClose, bid, bidOpen, bidHigh);

            int volume = marketSnapshotReader.getVolume(i);

            historicPriceChartItems.add(new CandleChartItem(askItem, bidItem, calendar));
            historicPriceTableItems.add(new VolumeItem(calendar, volume));
        }

        HistoricPricesRepository.getInstance().setChartData(historicPriceChartItems);
        HistoricPricesRepository.getInstance().setTableData(historicPriceTableItems);

        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                tabLayout.setupWithViewPager(viewPager);
                HistoricPricesViewPagerAdapter adapter =
                        new HistoricPricesViewPagerAdapter(getSupportFragmentManager(), HistoricPricesActivity.this);
                viewPager.setAdapter(adapter);
                progressIndication(false);
            }
        });
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_historic_prices);

        progressBar = findViewById(R.id.progressBar);
        tabLayout = findViewById(R.id.tabLayout);
        viewPager = findViewById(R.id.viewPager);

        setTitle(R.string.historic_prices_title);

        session = SharedObjects.getInstance().getSession();
        session.subscribeSessionStatus(this);

        requestFactory = session.getRequestFactory();
        responseFactory = session.getResponseReaderFactory();

        startDateCalendar = getStartDate();
        endDateCalendar = getEndDate();

        requestHistoricPrices();
    }

    private Calendar getStartDate() {
        Calendar calendar = Calendar.getInstance();
        calendar.add(Calendar.MONTH, -1);
        return calendar;
    }

    private Calendar getEndDate() {
        return Calendar.getInstance();
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
    public void onRequestFailed(final String s, final String s1) {
        StringBuilder errMsgToShow = new StringBuilder();
        errMsgToShow.append("Could not get data from server\nPlease check your internet connection and server availability\n\n");
        final CharSequence errMsgParsed = parseError(s1);
        errMsgToShow.append(errMsgParsed);

        if (null != s && !s.isEmpty()) {
            errMsgToShow.append("\n\nAdditional information: ");
            errMsgToShow.append(s);
        }
        final String errMsg = errMsgToShow.toString();

        runOnUiThread(new Runnable() {

            @Override
            public void run() {
                showCustomMsgDlg(errMsg, false, false, false);
            }
        });
    }

    @Override
    public void onTablesUpdates(O2GResponse o2GResponse) {
    }

    private void progressIndication(final boolean isBusy) {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                int progressBarVisibility = isBusy
                        ? View.VISIBLE
                        : View.GONE;

                int viewVisibility = isBusy
                        ? View.INVISIBLE
                        : View.VISIBLE;

                progressBar.setVisibility(progressBarVisibility);
                tabLayout.setVisibility(viewVisibility);
                viewPager.setVisibility(viewVisibility);
            }
        });
    }

    @Override
    protected void onDestroy() {
        session.unsubscribeResponse(this);
        session.unsubscribeSessionStatus(this);
        super.onDestroy();
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
                if (logout && session != null)
                    session.logout();

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

        O2GSessionStatusCode status = session.getSessionStatus();
        if (O2GSessionStatusCode.CONNECTED != status) {
            showCustomMsgDlg("Session lost, please log in again", false, true, false);
        }
        super.onResume();
    }

    @Override
    public void onSessionStatusChanged(final O2GSessionStatusCode o2GSessionStatusCode) {
        final O2GSessionStatusCode status = session.getSessionStatus();

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
    public void onBackPressed() {
        Intent intent = new Intent(this, SelectHistoryParametersActivity.class);
        startActivity(intent);
    }
}

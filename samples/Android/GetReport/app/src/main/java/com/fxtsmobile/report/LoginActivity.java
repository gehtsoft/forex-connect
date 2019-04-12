package com.fxtsmobile.report;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ProgressBar;
import android.widget.Toast;

import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GSessionStatusCode;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class LoginActivity extends AppCompatActivity implements IO2GSessionStatus {

    private static final String DEFAULT_CONNECTION = "Demo";
    private static final String DEFAULT_HOST = "http://www.fxcorporate.com/";

    private EditText loginEditText;
    private EditText passwordEditText;
    private EditText connectionEditText;
    private EditText hostEditText;
    private EditText sidEditText;
    private EditText pinEditText;
    private Button loginButton;
    private ProgressBar progressBar;

    private SharedPreferences mPreferences;

    private O2GSession session;

    private void login() {
        final O2GSessionStatusCode statusCode = session.getSessionStatus();
        if (statusCode == O2GSessionStatusCode.DISCONNECTING)
            session.logout();
        
        if (statusCode == O2GSessionStatusCode.CONNECTING)
            return; // already
        if (statusCode == O2GSessionStatusCode.CONNECTED) {
            onConnected();
            return;
        }
        final String login = loginEditText.getText().toString();
        final String password = passwordEditText.getText().toString();
        final String connection = connectionEditText.getText().toString();
        String host = hostEditText.getText().toString();

        if (login.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter login", Toast.LENGTH_SHORT).show();
            loginEditText.requestFocus();
            return;
        }
        if (password.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter password", Toast.LENGTH_SHORT).show();
            passwordEditText.requestFocus();
            return;
        }
        if (connection.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter connection name", Toast.LENGTH_SHORT).show();
            connectionEditText.requestFocus();
            return;
        }
        if (host.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter host name", Toast.LENGTH_SHORT).show();
            hostEditText.requestFocus();
            return;
        }

        if (!host.endsWith("Hosts.jsp")) {
            if (!host.endsWith("/")) {
                host = host + "/";
            }
            host = host + "Hosts.jsp";
        }
        session.login(login, password, host, connection);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);

        loginEditText = findViewById(R.id.loginEditText);
        passwordEditText = findViewById(R.id.passwordEditText);
        connectionEditText = findViewById(R.id.connectionEditText);
        hostEditText = findViewById(R.id.hostEditText);
        sidEditText = findViewById(R.id.sidEditText);
        pinEditText = findViewById(R.id.pinEditText);
        loginButton =  findViewById(R.id.loginButton);
        progressBar =  findViewById(R.id.progressBar);

        mPreferences = getPreferences(Context.MODE_PRIVATE);
        String sLastLogin = mPreferences.getString("login", "");
        loginEditText.setText(sLastLogin);
        connectionEditText.setText(mPreferences.getString("connection", ""));
        hostEditText.setText(mPreferences.getString("host", ""));
        sidEditText.setText(mPreferences.getString("sid", ""));

        String sConnection = connectionEditText.getText().toString();
        if (sConnection.isEmpty()) {
            connectionEditText.setText(DEFAULT_CONNECTION);
        }
        String sHost = hostEditText.getText().toString();
        if (sHost.isEmpty()) {
            hostEditText.setText(DEFAULT_HOST);
        }

        loginButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                login();
            }
        });

        setTitle("Login");

        session = SharedObjects.getInstance().getSession();
        session.subscribeSessionStatus(this);
    }

    @Override
    public void onSessionStatusChanged(final O2GSessionStatusCode o2GSessionStatusCode) {

        if(O2GSessionStatusCode.TRADING_SESSION_REQUESTED == o2GSessionStatusCode) {
            final String sid = sidEditText.getText().toString();
            final String pin = pinEditText.getText().toString();
            session.setTradingSession(sid, pin);
            return;
        }

        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                String strCode = o2GSessionStatusCode.toString().replace("_", " ");
                Toast.makeText(getApplicationContext(), strCode, Toast.LENGTH_SHORT).show();

                int progressVisibility = o2GSessionStatusCode == O2GSessionStatusCode.CONNECTING
                        ? View.VISIBLE
                        : View.GONE;

                boolean isLoginEnabled = o2GSessionStatusCode != O2GSessionStatusCode.CONNECTING;

                loginButton.setEnabled(isLoginEnabled);
                progressBar.setVisibility(progressVisibility);

                if (o2GSessionStatusCode == O2GSessionStatusCode.CONNECTED) {
                    onConnected();
                }
            }
        });
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
    public void onLoginFailed(final String s) {
        StringBuilder errMsgToShow = new StringBuilder();
        errMsgToShow.append("Could not connect or log in.\nPlease check authentication and connection parameters.\n\n");
        final CharSequence errMsg = parseError(s);
        errMsgToShow.append(errMsg);

        final StringBuilder errMsgToShowConst = errMsgToShow;

        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                new AlertDialog.Builder(LoginActivity.this)
                        .setMessage(errMsgToShowConst)
                        .setPositiveButton(android.R.string.ok, new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialogInterface, int i) {
                            }
                        })
                        .create()
                        .show();
            }
        });
    }

    private void onConnected() {
        commitPreferences();

        Intent intent = new Intent(this, MainActivity.class);
        startActivity(intent);

        finish();
    }

    private void commitPreferences() {
        final String sLogin = loginEditText.getText().toString();
        final String sConnection = connectionEditText.getText().toString();
        final String sHost = hostEditText.getText().toString();
        final String sid = sidEditText.getText().toString();

        SharedPreferences.Editor editor = mPreferences.edit();
        editor.putString("login", sLogin);
        editor.putString("connection", sConnection);
        editor.putString("host", sHost);
        editor.putString("sid", sid);
        editor.commit();
    }

    @Override
    protected void onDestroy() {
        session.unsubscribeSessionStatus(this);
        super.onDestroy();
    }

    @Override
    public void onBackPressed() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage("Are you sure you want to exit?")
                .setCancelable(false)
                .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int id) {
                        session.logout();
                        session.unsubscribeSessionStatus(LoginActivity.this);

                        LoginActivity.this.finish();

                        moveTaskToBack(true);
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
}

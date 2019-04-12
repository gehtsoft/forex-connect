package fxtsmobile.com.fxconnect.activities;

import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GSessionStatusCode;
import com.fxcore2.O2GTableManager;
import com.fxcore2.O2GTableManagerStatus;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import fxtsmobile.com.fxconnect.R;
import fxtsmobile.com.fxconnect.model.SharedObjects;

public class LoginActivity extends AppCompatActivity implements IO2GSessionStatus {
    private static final String DEFAULT_CONNECTION = "Demo";
    private static final String DEFAULT_HOST = "http://www.fxcorporate.com/";

    private O2GSession mSession;
    private EditText mEditTextLogin;
    private EditText mEditTextPassword;
    private EditText mEditTextConnection;
    private EditText mEditTextHost;
    private EditText sidEditText;
    private EditText pinEditText;
    private Button mLoginButton;
    private SharedPreferences mPreferences;

    private Handler mHandler = new Handler();

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);

        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeSessionStatus(this);

        mEditTextLogin = findViewById(R.id.etLogin);
        mEditTextPassword = findViewById(R.id.etPassword);
        mEditTextConnection = findViewById(R.id.etConnection);
        mEditTextHost = findViewById(R.id.etHost);
        mLoginButton = findViewById(R.id.btnLogin);
        sidEditText = findViewById(R.id.sidEditText);
        pinEditText = findViewById(R.id.pinEditText);

        setTitle(R.string.login_title);

        mPreferences = getPreferences(Context.MODE_PRIVATE);
        String sLastLogin = mPreferences.getString("login", "");
        mEditTextLogin.setText(sLastLogin);
        mEditTextConnection.setText(mPreferences.getString("connection", ""));
        mEditTextHost.setText(mPreferences.getString("host", ""));
        sidEditText.setText(mPreferences.getString("sid", ""));

        String sConnection = mEditTextConnection.getText().toString();
        if (sConnection.isEmpty()) {
            mEditTextConnection.setText(DEFAULT_CONNECTION);
        }
        String sHost = mEditTextHost.getText().toString();
        if (sHost.isEmpty()) {
            mEditTextHost.setText(DEFAULT_HOST);
        }

        if (!"".equals(sLastLogin)) {
            mEditTextPassword.requestFocus();
        }
        else {
            mEditTextLogin.requestFocus();
        }
    }

    public void btnLogin_Click(View view) {
        login();
    }

    private void login() {
        final O2GSessionStatusCode statusCode = mSession.getSessionStatus();
        if (statusCode == O2GSessionStatusCode.DISCONNECTING)
            mSession.logout();

        if (statusCode == O2GSessionStatusCode.CONNECTING)
            return; // already
        if (statusCode == O2GSessionStatusCode.CONNECTED) {
            onConnected();
            return;
        }
        final String sLogin = mEditTextLogin.getText().toString();
        final String sPassword = mEditTextPassword.getText().toString();
        final String sConnection = mEditTextConnection.getText().toString();
        String sHost = mEditTextHost.getText().toString();

        if (sLogin.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter login", Toast.LENGTH_SHORT).show();
            mEditTextLogin.requestFocus();
            return;
        }
        if (sPassword.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter password", Toast.LENGTH_SHORT).show();
            mEditTextPassword.requestFocus();
            return;
        }
        if (sConnection.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter connection name", Toast.LENGTH_SHORT).show();
            mEditTextConnection.requestFocus();
            return;
        }
        if (sHost.isEmpty())
        {
            Toast.makeText(getApplicationContext(), "Please enter host name", Toast.LENGTH_SHORT).show();
            mEditTextHost.requestFocus();
            return;
        }
        mLoginButton.setEnabled(false);

        if (!sHost.endsWith("Hosts.jsp")) {
            if (!sHost.endsWith("/")) {
                sHost = sHost + "/";
            }
            sHost = sHost + "Hosts.jsp";
        }
        mSession.login(sLogin, sPassword, sHost, sConnection);
    }

    @Override
    protected void onDestroy() {
        mSession.unsubscribeSessionStatus(this);
        super.onDestroy();
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

    private class LoginErrorRunnable implements Runnable {

        private StringBuilder errMsgToShow = new StringBuilder();

        public LoginErrorRunnable(final String sLoginError) {
            errMsgToShow.append("Could not connect or log in.\nPlease check authentication and connection parameters.\n\n");
            final CharSequence errMsg = parseError(sLoginError);
            errMsgToShow.append(errMsg);
        }

        public void run() {

            final String errMsg = errMsgToShow.toString();

            runOnUiThread(new Runnable() {

                @Override
                public void run() {
                    showCustomMsgDlg(errMsg, false, false, false);
                }
            });
        }
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

    private void onConnected() {
        waitTableManagerRefresh();
        commitPreferences();

        Intent intent = new Intent(LoginActivity.this, SelectHistoryParametersActivity.class);
        startActivity(intent);

        mLoginButton.setEnabled(true);
        finish();
    }

    private class SessionStatusChangedRunnable implements Runnable {

        private O2GSessionStatusCode mCode;

        public SessionStatusChangedRunnable(O2GSessionStatusCode eCode) {
            mCode = eCode;
        }

        public void run() {
            mSession = SharedObjects.getInstance().getSession();
            mCode = mSession.getSessionStatus();

            final String sid = sidEditText.getText().toString();
            final String pin = pinEditText.getText().toString();
            mSession.setTradingSession(sid, pin);

            runOnUiThread(new Runnable() {

                @Override
                public void run() {
                    LoginActivity.this.setTitle(mCode.toString());

                    final String strCode = mCode.toString().replace("_", " ");
                    Toast.makeText(getApplicationContext(), strCode, Toast.LENGTH_SHORT).show();
                }
            });

            if (mCode == O2GSessionStatusCode.CONNECTED) {
                onConnected();
            }
            else if (mCode == O2GSessionStatusCode.DISCONNECTED) {
                mLoginButton.setEnabled(true);
            }
        }
    }

    public void onLoginFailed(String error) {
        mHandler.post(new LoginErrorRunnable(error));
        mSession.logout();
    }

    public void onSessionStatusChanged(O2GSessionStatusCode eStatusCode) {
        mHandler.post(new SessionStatusChangedRunnable(eStatusCode));
    }

    private void commitPreferences() {
        final String sLogin = mEditTextLogin.getText().toString();
        final String sConnection = mEditTextConnection.getText().toString();
        final String sHost = mEditTextHost.getText().toString();
        final String sid = sidEditText.getText().toString();

        SharedPreferences.Editor editor = mPreferences.edit();
        editor.putString("login", sLogin);
        editor.putString("connection", sConnection);
        editor.putString("host", sHost);
        editor.putString("sid", sid);
        editor.commit();
    }

    @Override
    public void onBackPressed() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage("Are you sure you want to exit?")
                .setCancelable(false)
                .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int id) {
                        mSession.logout(); // just in case
                        mSession.unsubscribeSessionStatus(LoginActivity.this); // just in case

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

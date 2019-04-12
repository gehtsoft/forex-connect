package com.FXTSMobile;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.SharedPreferences.Editor;
import android.os.Bundle;
import android.os.Handler;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GAccountsTableResponseReader;
import com.fxcore2.O2GResponse;
import com.fxcore2.O2GSession;
import com.fxcore2.O2GSessionStatusCode;
import com.fxcore2.O2GTableType;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class LoginDialogActivity extends Activity implements IO2GSessionStatus  {

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
    
    private String mLoginError;
    private Handler mHandler = new Handler();
    
    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.login_dialog);

        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeSessionStatus(this);
        
        mEditTextLogin = (EditText)this.findViewById(R.id.etLogin);
        mEditTextPassword = (EditText)this.findViewById(R.id.etPassword);
        mEditTextConnection = (EditText)this.findViewById(R.id.etConnection);
        mEditTextHost = (EditText)this.findViewById(R.id.etHost);
        mLoginButton = (Button)this.findViewById(R.id.btnLogin);
        sidEditText = findViewById(R.id.sidEditText);
        pinEditText = findViewById(R.id.pinEditText);

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
        String sLogin = mEditTextLogin.getText().toString();
        String sPassword = mEditTextPassword.getText().toString();
        String sConnection = mEditTextConnection.getText().toString();
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

    private Runnable mLoginErrorRunnable = new Runnable() {
        
        public void run() {
            StringBuilder errMsgToShow = new StringBuilder();
            errMsgToShow.append("Could not connect or log in.\nPlease check authentication and connection parameters.\n\n");
            final CharSequence errMsg = parseError(mLoginError);
            errMsgToShow.append(errMsg);

            final StringBuilder errMsgToShowConst = errMsgToShow;

            runOnUiThread(new Runnable() {

                @Override
                public void run() {
                    showCustomMsgDlg(errMsgToShowConst.toString(), false, false, false);
                }
            });
        }
    };

    private void onConnected() {
        setAccountID();
        commitPreferences();

        Intent intent = new Intent(LoginDialogActivity.this, MainActivity.class);
        startActivity(intent);
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

            if (O2GSessionStatusCode.TRADING_SESSION_REQUESTED != mCode) {
                final String strCode = mCode.toString().replace("_", " ");

                runOnUiThread(new Runnable() {

                    @Override
                    public void run() {
                        LoginDialogActivity.this.setTitle(strCode);
                        Toast.makeText(getApplicationContext(), strCode, Toast.LENGTH_SHORT).show();
                    }
                });
            }

            if (mCode == O2GSessionStatusCode.CONNECTED) {
                onConnected();

                mLoginButton.setEnabled(true);
            }
            else if (mCode == O2GSessionStatusCode.DISCONNECTED) {
                mLoginButton.setEnabled(true);
            }
        }
        
    }
    
    private void setAccountID() {
        O2GResponse accountsResponse = mSession.getLoginRules().getTableRefreshResponse(O2GTableType.ACCOUNTS);
        O2GAccountsTableResponseReader reader = mSession.getResponseReaderFactory().createAccountsTableReader(accountsResponse);
        SharedObjects.getInstance().setAccountID(reader.getRow(0).getAccountID());
    }
    
    public void onLoginFailed(String error) {
        mLoginError = error;
        mHandler.post(mLoginErrorRunnable);
    }
    
    public void onSessionStatusChanged(O2GSessionStatusCode eStatusCode) {
        mHandler.post(new SessionStatusChangedRunnable(eStatusCode));
    }

    private void commitPreferences() {
        String sLogin = mEditTextLogin.getText().toString();
        String sConnection = mEditTextConnection.getText().toString();
        String sHost = mEditTextHost.getText().toString();
        final String sid = sidEditText.getText().toString();

        Editor editor = mPreferences.edit();
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
                        mSession.unsubscribeSessionStatus(LoginDialogActivity.this); // just in case

                        LoginDialogActivity.this.finish();

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
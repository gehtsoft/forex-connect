package com.FXTSMobile;

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

public class LoginDialogActivity extends Activity implements IO2GSessionStatus  {
    private O2GSession mSession;
    private EditText mEditTextLogin;
    private EditText mEditTextPassword;
    private EditText mEditTextConnection;
    private EditText mEditTextHost;
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
        
        
        mPreferences = getPreferences(Context.MODE_PRIVATE);
        String sLastLogin = mPreferences.getString("login", "");
        mEditTextLogin.setText(sLastLogin);
        mEditTextConnection.setText(mPreferences.getString("connection", ""));
        mEditTextHost.setText(mPreferences.getString("host", ""));
        
        if (!"".equals(sLastLogin)) {
            mEditTextPassword.requestFocus();
        }
        else {
            mEditTextLogin.requestFocus();
        }
    }
    
    public void btnLogin_Click(View view) {
        mLoginButton.setEnabled(false);
        login();
    }
    
    private void login() {
        String sLogin = mEditTextLogin.getText().toString();
        String sPassword = mEditTextPassword.getText().toString();
        String sConnection = mEditTextConnection.getText().toString();
        String sHost = mEditTextHost.getText().toString();
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
    private Runnable mLoginErrorRunnable = new Runnable() {
        
        public void run() {
            Toast.makeText(getApplicationContext(), mLoginError, Toast.LENGTH_SHORT).show();
        }
    };
    
    private class SessionStatusChangedRunnable implements Runnable {
        
        private O2GSessionStatusCode mCode;
        
        public SessionStatusChangedRunnable(O2GSessionStatusCode eCode) {
            mCode = eCode;
        }
        
        public void run() {
            LoginDialogActivity.this.setTitle(mCode.toString());
            Toast.makeText(getApplicationContext(), mCode.toString(), Toast.LENGTH_SHORT).show();
            if (mCode == O2GSessionStatusCode.CONNECTED) {
                setAccountID();
                mLoginButton.setEnabled(true);
                commitPreferences();
                Intent intent = new Intent(LoginDialogActivity.this, MainActivity.class);
                startActivity(intent);
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

        Editor editor = mPreferences.edit();
        editor.putString("login", sLogin);
        editor.putString("connection", sConnection);
        editor.putString("host", sHost);
        editor.commit();
    }
}
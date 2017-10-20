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
import com.fxcore2.O2GSession;
import com.fxcore2.O2GSessionStatusCode;
import com.fxcore2.O2GTableManager;
import com.fxcore2.O2GTableManagerStatus;

public class LoginDialogActivity extends Activity implements IO2GSessionStatus  {
    private O2GSession mSession;
    private EditText mEditTextLogin;
    private EditText mEditTextPassword;
    private EditText mEditTextConnection;
    private EditText mEditTextHost;
    private Button mLoginButton;
    private SharedPreferences mPreferences;
    
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
    
    private class LoginErrorRunnable implements Runnable {
    
        private String mLoginError;
        
        public LoginErrorRunnable(String sLoginError) {
            this.mLoginError = sLoginError;
            
        }

        public void run() {
            Toast.makeText(getApplicationContext(), mLoginError, Toast.LENGTH_SHORT).show();
        }
    }
    
    
    private class SessionStatusChangedRunnable implements Runnable {
        
        private O2GSessionStatusCode mCode;
        
        public SessionStatusChangedRunnable(O2GSessionStatusCode eCode) {
            mCode = eCode;
        }
        
        public void run() {
            LoginDialogActivity.this.setTitle(mCode.toString());
            Toast.makeText(getApplicationContext(), mCode.toString(), Toast.LENGTH_SHORT).show();
            if (mCode == O2GSessionStatusCode.CONNECTED) {
                waitTableManagerRefresh();
                commitPreferences();
                Intent intent = new Intent(LoginDialogActivity.this, MainActivity.class);
                startActivity(intent);
                mLoginButton.setEnabled(true);
            }
            else if (mCode == O2GSessionStatusCode.DISCONNECTED) {
                mLoginButton.setEnabled(true);
            }
        }

        private void waitTableManagerRefresh() {
            O2GTableManager tableManger = mSession.getTableManager();
            while (tableManger.getStatus() != O2GTableManagerStatus.TABLES_LOADED &&
                   tableManger.getStatus() != O2GTableManagerStatus.TABLES_LOAD_FAILED) {
                try {
                    Thread.sleep(50);
                } catch (InterruptedException e) {
                    // TODO Auto-generated catch block
                    e.printStackTrace();
                }
            }
        }
        
    }
    
    public void onLoginFailed(String error) {
        mHandler.post(new LoginErrorRunnable(error));
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
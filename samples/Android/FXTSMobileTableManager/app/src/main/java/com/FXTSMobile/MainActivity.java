package com.FXTSMobile;

import android.app.AlertDialog;
import android.app.ListActivity;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.AdapterView;
import android.widget.AdapterView.OnItemClickListener;
import android.widget.ArrayAdapter;
import android.widget.ListAdapter;
import android.widget.TextView;

import com.fxcore2.O2GSession;
import com.fxcore2.IO2GSessionStatus;
import com.fxcore2.O2GSessionStatusCode;

public class MainActivity extends ListActivity implements OnItemClickListener, IO2GSessionStatus {

    private O2GSession mSession;
    private boolean mActive = false;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        ListAdapter adapter = new ArrayAdapter<String>(this,
                R.layout.main_list_item, this.getResources().getStringArray(
                        R.array.tables));
        this.setListAdapter(adapter);
        this.getListView().setOnItemClickListener(this);

        mSession = SharedObjects.getInstance().getSession();
        mSession.subscribeSessionStatus(this);
    }

    @Override
    public void onBackPressed() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setMessage("Are you sure you want to log out?")
                .setCancelable(false)
                .setPositiveButton("Yes",
                        new DialogInterface.OnClickListener() {
                            public void onClick(DialogInterface dialog, int id) {
                                mSession.unsubscribeSessionStatus(MainActivity.this);
                                SharedObjects.getInstance().getSession().logout();

                                Intent intent = new Intent(MainActivity.this, LoginDialogActivity.class);
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
    public void onItemClick(AdapterView<?> arg0, View arg1, int arg2, long arg3) {
        TextView textView = (TextView) arg1;
        String sTableName = textView.getText().toString();
        Class<?> activityClass = this.getActivityClassForTable(sTableName);
        if (activityClass != null) {
            Intent intent = new Intent(this, activityClass);
            this.startActivity(intent);
        }
    }

    private Class<?> getActivityClassForTable(String sTableName) {
        if (sTableName.compareTo("Offers") == 0) {
            return OffersActivity.class;
        } else if (sTableName.compareTo("Accounts") == 0) {
            return AccountsActivity.class;
        } else if (sTableName.compareTo("Orders") == 0) {
            return OrdersActivity.class;
        } else if (sTableName.compareTo("Trades") == 0) {
            return TradesActivity.class;
        } else if (sTableName.compareTo("Closed Trades") == 0) {
            return ClosedTradesActivity.class;
        } else if (sTableName.compareTo("Summary") == 0) {
            return SummaryActivity.class;
        } else {
            return null;
        }
    }

    @Override
    protected void onPause() {
        mActive = false;
        super.onPause();
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

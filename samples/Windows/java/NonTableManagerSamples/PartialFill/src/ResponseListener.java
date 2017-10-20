package partialfill;

import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

import com.fxcore2.*;

public class ResponseListener implements IO2GResponseListener {
    private O2GSession mSession;
    private String mRequestID;
    private String mOrderID;
    private O2GResponse mResponse;
    private Semaphore mSemaphore;

    // ctor
    public ResponseListener(O2GSession session) {
        mSession = session;
        mRequestID = "";
        mOrderID = "";
        mResponse = null;
        mSemaphore = new Semaphore(0);
    }

    public void setRequestID(String sRequestID) {
        mResponse = null;
        mOrderID = "";
        mRequestID = sRequestID;
    }

    public boolean waitEvents() throws Exception {
        return mSemaphore.tryAcquire(30, TimeUnit.SECONDS);
    }

    public O2GResponse getResponse() {
        return mResponse;
    }

    public void onRequestCompleted(String sRequestId, O2GResponse response) {
        if (mRequestID.equals(response.getRequestId())) {
            mResponse = response;
            if (response.getType() != O2GResponseType.CREATE_ORDER_RESPONSE) {
                mSemaphore.release();
            }
        }
    }

    public void onRequestFailed(String sRequestID, String sError) {
        if (mRequestID.equals(sRequestID)) {
            System.out.println("Request failed: " + sError);
            mSemaphore.release();
        }
    }

    public void onTablesUpdates(O2GResponse response) {
        O2GResponseReaderFactory factory = mSession.getResponseReaderFactory();
        if (factory != null) {
            O2GTablesUpdatesReader reader = factory.createTablesUpdatesReader(response);
            for (int ii = 0; ii < reader.size(); ii++) {
                switch (reader.getUpdateTable(ii)) {
                case ACCOUNTS:
                    O2GAccountRow account = reader.getAccountRow(ii);
                    // Show balance updates
                    System.out.println(String.format("Balance: %.2f", account.getBalance()));
                    break;
                case ORDERS:
                    O2GOrderRow order = reader.getOrderRow(ii);
                    switch (reader.getUpdateType(ii)) {
                    case INSERT:
                        if(mRequestID.equals(order.getRequestID())) {
                            mOrderID = order.getOrderID();
                            printOrder("New order is added", order);
                        }
                        break;
                    case UPDATE:
                        printOrder("An order is changed", order);
                        break;
                    case DELETE:
                        if (mRequestID.equals(order.getRequestID())) {
                            String sStatus = order.getStatus();
                            if (sStatus.equals("R")) {
                                printOrder("An order has been rejected", order);
                            } else {
                                printOrder("An order is going to be removed", order);
                            }
                            mSemaphore.release();
                        }
                        break;
                    }
                    break;
                case TRADES:
                    O2GTradeRow trade = reader.getTradeRow(ii);
                    if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                        System.out.println(String.format("Position is opened: TradeID='%s', TradeIDOrigin='%s'",
                                trade.getTradeID(), trade.getTradeIDOrigin()));
                    }
                    break;
                case CLOSED_TRADES:
                    O2GClosedTradeRow closedTrade = reader.getClosedTradeRow(ii);
                    if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                        System.out.println(String.format("Position is closed: TradeID='%s'",
                                closedTrade.getTradeID()));
                    }
                    break;
                case MESSAGES:
                    O2GMessageRow message = reader.getMessageRow(ii);
                    if (reader.getUpdateType(ii) == O2GTableUpdateType.INSERT) {
                        String text = message.getText();
                        int findPos = text.indexOf(mOrderID);
                        if (findPos >= 0) {
                            System.out.println(String.format("Feature='%s', Message='%s'",
                                    message.getFeature(), text));
                        }
                    }
                    break;
                }
            }
        }
    }

    private void printOrder(String sCaption, O2GOrderRow orderRow) {
        System.out.println(String.format("%s: OrderID='%s', TradeID='%s', Status='%s', " +
                "Amount='%s', OriginAmount='%s', FilledAmount='%s'",
                sCaption, orderRow.getOrderID(), orderRow.getTradeID(), orderRow.getStatus(),
                orderRow.getAmount(), orderRow.getOriginAmount(), orderRow.getFilledAmount()));
    }
}

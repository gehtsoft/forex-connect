package common;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import com.fxcore2.*;

public class BatchOrderMonitor {
    private List<String> mRequestIDs;
    private List<OrderMonitor> mMonitors;
    
    public BatchOrderMonitor() {
        mRequestIDs = new ArrayList<String>();
        mMonitors = new ArrayList<OrderMonitor>();
    }

    public List<OrderMonitor> getMonitors() {
        return mMonitors;
    }

    public void setRequestIDs(List<String> requestIDs) {
        mRequestIDs.clear();
        for(String requestID : requestIDs) {
            mRequestIDs.add(requestID);
        }
    }

    public void onRequestCompleted(String requestID, O2GResponse response) {
    }
    
    private void removeRequestID(String requestID) {
        if (isOwnRequest(requestID)) {
            mRequestIDs.remove(requestID);
        }
    }

    public void onRequestFailed(String requestId) {
        removeRequestID(requestId);
    }

    public void onTradeAdded(O2GTradeRow tradeRow) {
        for(int i = 0; i < mMonitors.size(); i++) {
            mMonitors.get(i).onTradeAdded(tradeRow);
        }
    }

    public void onOrderAdded(O2GOrderRow order) {
        String requestID = order.getRequestID();
        System.out.println("Order Added " + order.getOrderID());
        if (isOwnRequest(requestID)) {
            if (OrderMonitor.isClosingOrder(order) || OrderMonitor.isOpeningOrder(order)) {
                addToMonitoring(order);
            }
        }
    }

    public void onOrderDeleted(O2GOrderRow order) {
        for(int i = 0; i < mMonitors.size(); i++) {
            mMonitors.get(i).onOrderDeleted(order);
        }
    }

    public void onMessageAdded(O2GMessageRow message) {
        for(int i = 0; i < mMonitors.size(); i++) {
            mMonitors.get(i).onMessageAdded(message);
        }
    }

    public void onClosedTradeAdded(O2GClosedTradeRow closeTradeRow) {
        for(int i = 0; i < mMonitors.size(); i++) {
            mMonitors.get(i).onClosedTradeAdded(closeTradeRow);
        }
    }

    public boolean isBatchExecuted() {
        boolean allCompleted = true;
        for (Iterator<OrderMonitor> iterator = mMonitors.iterator(); iterator.hasNext();) {
            OrderMonitor monitor = iterator.next();
            if (monitor.isOrderCompleted()) {
                removeRequestID(monitor.getOrder().getRequestID());
            } else {
                allCompleted = false;
            }
        }
        boolean result = mRequestIDs.size() == 0 && allCompleted;
        return result;
    }

    private boolean isOwnRequest(String requestID) {
        return mRequestIDs.contains(requestID);
    }

    private void addToMonitoring(O2GOrderRow order) {
        OrderMonitor monitor = new OrderMonitor(order);
        mMonitors.add(monitor);
    }
}

#pragma once

class OrderMonitor;
class BatchOrderMonitor
{
 public:
    BatchOrderMonitor();
    ~BatchOrderMonitor();
    void setRequestIDs(std::vector<std::string> &requestIDs);
    void onRequestCompleted(const char *requestID, IO2GResponse *response);
    void onRequestFailed(const char *requestId , const char *error);
    void onTradeAdded(IO2GTradeRow *trade);
    void onOrderAdded(IO2GOrderRow *order);
    void onOrderDeleted(IO2GOrderRow *order);
    void onMessageAdded(IO2GMessageRow *message);
    void onClosedTradeAdded(IO2GClosedTradeRow *closedTrade);
    bool isBatchExecuted();
    void getMonitors(std::vector<OrderMonitor*> &monitors);
 private:
    bool isOwnRequest(const std::string &sRequestID);
    void addToMonitoring(IO2GOrderRow *order);
    void removeRequestID(const std::string &sRequestID);

    std::vector<std::string> mRequestIDs;
    std::vector<OrderMonitor *> mMonitors;
};


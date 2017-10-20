import Foundation

class ResponseListener : IO2GResponseListener {
    private var requestId: String
    private var condition: NSCondition
    private var response: IO2GResponse?
    
    init() {
        requestId = ""
        condition = NSCondition()
    }
    
    func getResponse() -> IO2GResponse? {
        return response
    }
    
    func wait() {
        let now = NSDate()
        condition.wait(until: now.addingTimeInterval(30) as Date)
        condition.unlock()
    }
    
    func setRequestId(requestId: String) {
        self.requestId = requestId;
        response = nil
        condition.lock()
    }
    
    /** Request execution completed data handler. */
    func onRequestCompleted(_ requestId: String!, _ response: IO2GResponse!) {
        if self.requestId == requestId {
            self.response = response
            condition.signal()
        }
    }
    
    /** Request execution failed data handler. */
    func onRequestFailed(_ requestId: String!, _ error: String!) {
        if self.requestId == requestId {
            self.response = nil
            condition.signal();
        }
    }
    
    /** Request update data received data handler. */
    func onTablesUpdates(_ tablesUpdates: IO2GResponse!) {
    }
}

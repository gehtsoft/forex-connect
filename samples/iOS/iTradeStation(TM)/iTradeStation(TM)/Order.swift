import Foundation

class Order : IO2GResponseListener
{
    private var requestId: String?
    private var errorMsg: String?
    private var notifier: NSCondition
    
    private var forexConnect = ForexConnect.sharedInstance()
    
    var errorMessage: String?
    {
        get { return errorMsg }
    }

    var orderCreatedSuccessfully: Bool
    {
        get { return errorMsg == nil }
    }
    
    init()
    {
        notifier = NSCondition()
        forexConnect.session.subscribeResponse(self)
    }
    
    deinit
    {
        forexConnect.session.unsubscribeResponse(self)
    }
    
    @objc func onRequestCompleted(_ requestId: String!, _ response: IO2GResponse!)
    {
        if self.requestId == requestId
        {
            self.errorMsg = nil
            self.notifier.signal()
        }
    }
    
    @objc func onRequestFailed(_ requestId: String!, _ error: String!)
    {
        if self.requestId == requestId
        {
            self.errorMsg = error
            self.notifier.signal()
        }
    }
    
    func onTablesUpdates(_ tablesUpdates: IO2GResponse!)
    {
    }
    
    func createOrder(offerIndex: Int, isBuy: Bool, amount: Int, rate: Double, orderType: Int)
    {
        requestId = forexConnect.createOrder(offerIndex: offerIndex, isBuy: isBuy, amount: amount, rate: rate, orderType: orderType)
        notifier.lock()
    }
    
    func wait() {
        let now = Date()
        let isOk = notifier.wait(until: now.addingTimeInterval(10) as Date)
        if !isOk {
            errorMsg = "Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding."
        }
        notifier.unlock()
        requestId = nil
    }
}

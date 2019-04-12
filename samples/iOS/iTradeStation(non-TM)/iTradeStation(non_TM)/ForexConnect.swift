import Foundation
import UIKit

class OfferRow {
    var instr: String
    var bid: Double
    var ask: Double
    var bidDirection: Int
    var askDirection: Int
    var offerID: String
    var isChanged: Bool
    var digits: Int
    
    init(instr: NSString, _ bid: Double, _ ask: Double, _ offerID: String, _ digits: Int) {
        self.instr = instr as String
        self.ask = ask
        self.bid = bid
        self.digits = digits
        self.isChanged = true
        self.offerID = offerID
        self.bidDirection = 0
        self.askDirection = 0
    }
}

class ForexConnect: IO2GSessionStatus, IO2GResponseListener
{
    private var user: String
    private var pwd: String
    private var url: String
    private var connection: String
    private var sessionId: String
    private var pin: String
    private var session: IO2GSession
    private var cond: NSCondition;
    private var statusNotificator: (IO2GSessionStatus_O2GSessionStatus) -> ()
    private var offersUpdateNotificator: (() -> ())?
    private var offersRow: Array<OfferRow>
    private var firstAccountID: String
    private var offerRowsLock: NSLock
    private var isConnected = false
    
    static let sharedInstance = ForexConnect()
    
    static func getSharedInstance() -> ForexConnect {
        return sharedInstance
    }
    
    private init() {
        cond = NSCondition()
        user = ""
        pwd = ""
        url = ""
        connection = ""
        sessionId = ""
        pin = ""
        firstAccountID = ""
        offersRow = Array<OfferRow>()
        offerRowsLock = NSLock()
        statusNotificator = { (param: IO2GSessionStatus_O2GSessionStatus) -> () in  }
        
        O2GTransport.setNumberOfReconnections(0)
        session = O2GTransport.createSession()
        session.subscribeSessionStatus(self)
        session.subscribeResponse(self)
    }
    
    deinit {
        session.unsubscribeResponse(self)
        session.unsubscribeSessionStatus(self)
    }
    
    func getSession() -> IO2GSession {
        return session
    }
    
    @objc func onLoginFailed(_ error: String!) {
        print("Login has been failed: \(error)")
    }
    
    @objc func onRequestCompleted(_ requestId: String!, _ response: IO2GResponse!) {
        autoreleasepool {
        if response != nil {
            if response.getType() == GetTrades{
                onTradesReceived(response: response)
            }
            else if response.getType() == GetClosedTrades {
                onClosedTradesReceived(response: response)
            }
            else if response.getType() == GetOrders {
                onOrdersReceived(response: response)
            }
            else if response.getType() == GetOffers {
                onOffersReceived(response: response)
            }
            else if response.getType() == GetAccounts {
                onAccountsReceived(response: response)
            }
        }
        }
    }
    
    @objc func onRequestFailed(_ requestId: String!, _ error: String!) {
    }
    
    @objc func onSessionStatusChanged(_ status: IO2GSessionStatus_O2GSessionStatus) {
        
        autoreleasepool {
        switch status {
            
        case IO2GSession_Disconnected:
            print("Session status has been changed: Disconnected")
            statusNotificator(IO2GSession_Disconnected);
            isConnected = false
            offersRow.removeAll()
            clearCredintals()
            cond.signal()
        
        case IO2GSession_Disconnecting:
            statusNotificator(IO2GSession_Disconnecting);
            print("Session status has been changed: Disconnecting")
            
        case IO2GSession_Connecting:
            statusNotificator(IO2GSession_Connecting);
            print("Session status has been changed: Connecting")
            
        case IO2GSession_Connected:
            statusNotificator(IO2GSession_Connected);
            print("Session status has been changed: Connected")
            
            // Both Offers and Trades tables are loaded by default
            // you can immediately get contents of the tables
            // by calling IO2GLoginRules.getTableRefreshResponse()
            //
            refreshTable(table: Offers)
            refreshTable(table: Accounts)
            
            // Trades, ClosedTrades, Orders tables are not loaded by default,
            // you need to load ones manually by sending a refresh table request.
            //
            refreshTable(table: Trades)
            refreshTable(table: ClosedTrades)
            refreshTable(table: Orders)
            
            isConnected = true
            
            cond.signal()
        
        case IO2GSession_Reconnecting:
            statusNotificator(IO2GSession_Reconnecting)
            print("Session status has been changed: Reconnecting")
            
        case IO2GSession_SessionLost:
            statusNotificator(IO2GSession_SessionLost);
            print("Session status has been changed: SessionLost")
            isConnected = false
            
        case IO2GSession_TradingSessionRequested:
            statusNotificator(IO2GSession_TradingSessionRequested)
            print("Session status has been changed: TradingSessionRequested")
            
            if sessionId.isEmpty {
                let descriptors = session.getTradingSessionDescriptors()
                if ((descriptors?.size())! > Int32(0)) {
                    sessionId = (descriptors?.get(0).getID())!
                }
            }
            session.setTrading(sessionId, pin: pin)
            break
            
        default:
            print("Session status has been changed: Unknown")
        }
        }
    }

	// 1. Both Offers and Accounts tables are loaded by default, you don't need to load them manually.
    //    After the tables to be updated, you will receive the response with the rows of updated tables
    //    (forex connect framework pass it to callback IO2GResponseListener.onTablesUpdates());
    //    you can also obtain that response by calling IO2GLoginRules.getTableRefreshResponse() if you don't want to use a response listener.
    //
    // 2. Other tables are not loaded by default, you need to load ones manually by sending a refresh table request.
    //    After sending the request to the server, you will receive the response in IO2GResponseListener.onRequestCompleted)
    //    After every updating the tables, you will receive the response with the rows of updated tables.
    //    (forex connect framework pass it to callback IO2GResponseListener.onTablesUpdates())
    func refreshTable(table: O2GTable) {
        autoreleasepool {
        let loginRules = session.getLoginRules()
        if loginRules != nil && (loginRules?.isTableLoaded(byDefault: table))! {
            let response = loginRules?.getTableRefreshResponse(table)
            onRequestCompleted(response?.getRequestID(), response)
        } else {
            let requestFactory = session.getRequestFactory()
            let updateTableRequest = requestFactory?.createRefreshTableRequest(byAccount: table, firstAccountID)
            session.send(updateTableRequest)
        }
        }
    }

    func subscribeStatus(closure: @escaping (IO2GSessionStatus_O2GSessionStatus) -> ()) {
        statusNotificator = closure
    }
    
    @objc func onTablesUpdates(_ response: IO2GResponse!) {
        if response.getType() == TablesUpdates {
            onTablesUpdateReceive(response: response);
        }
    }

    func onTablesUpdateReceive(response: IO2GResponse) {
        autoreleasepool {
        let factory = session.getResponseReaderFactory()
        if factory == nil {
            return
        }
        
        let updatesReader = factory?.createTablesUpdatesReader(response)
        if updatesReader == nil {
            return
        }
        
        let size = updatesReader?.size();
        let first : Int32 = 0
        
        for i in first..<size! {
            let o2gtable = updatesReader?.getUpdateTable(i);
            
            switch o2gtable {
            case Offers?:
                onOffersReceived(response: response)
                break
            case Accounts?:
                onAccountsReceived(response: response)
                break
            case Trades?:
                onOrdersReceived(response: response)
                break
            case Orders?:
                onTradesReceived(response: response)
                break
            case ClosedTrades?:
                onClosedTradesReceived(response: response)
                break
            default:
                break
            }
        }
        }
    }

    func onTradesReceived(response: IO2GResponse) {
        let factory = session.getResponseReaderFactory()
        if factory == nil {
            return
        }
        let tradesReader = factory?.createTradesTableReader(response)
        if tradesReader == nil {
            return
        }
        print("onTradesReceived() number of rows: " + String(describing: tradesReader?.size()));
    }
    
    
    func onOrdersReceived(response: IO2GResponse) {
        let factory = session.getResponseReaderFactory()
        if factory == nil {
            return
        }
        let ordersReader = factory?.createOrdersTableReader(response)
        if ordersReader == nil {
            return
        }
        print("onOrdersReceived() number of rows: " + String(describing: ordersReader?.size()));
    }
    
    func onClosedTradesReceived(response: IO2GResponse) {
        let factory = session.getResponseReaderFactory()
        if factory == nil {
            return
        }
        let closedTradesReader = factory?.createClosedTradesTableReader(response)
        if closedTradesReader == nil {
            return
        }
        print("onClosedTradesReceived() number of rows: " + String(describing: closedTradesReader?.size()));
    }

    func onOffersReceived(response: IO2GResponse) {
        
        let factory = session.getResponseReaderFactory()
        if factory == nil {
            return
        }
        
        let offersReader = factory?.createOffersTableReader(response)
        if offersReader == nil {
            return
        }
        
        setData(offersReader: offersReader!)
        
    }
    
    func onAccountsReceived(response: IO2GResponse) {
       
        let factory = session.getResponseReaderFactory()
        if factory == nil {
            return
        }
        
        let accountsReader = factory?.createAccountsTableReader(response)
        if accountsReader == nil {
            return
        }
        
        if firstAccountID.isEmpty {
            let account = accountsReader?.getRow(Int32(0))
            firstAccountID = (account?.getAccountID())!
        }
    }
    
    func setData(offersReader: IO2GOffersTableResponseReader) {
        
        offerRowsLock.lock()
        
        let size = offersReader.size()
        let first : Int32 = 0
        
        for i in first..<size {
            let offer = offersReader.getRow(i)
            if offer == nil {
                continue
            }
            let strOfferID = offer?.getOfferID()
            
            if offer?.getSubscriptionStatus().characters.first != "T" {
                continue
            }
            
            let ask = offer?.getAsk()
            let bid = offer?.getBid()
            
            let foundOffers = offersRow.filter({ (row) -> Bool in
                row.offerID == strOfferID
            });
            
            let digits : Int32 = (offer?.getDigits())!
            
            if foundOffers.count == 0 {
                let offerRow = OfferRow(instr: offer!.getInstrument()! as NSString,
                                        (offer?.getBid())!,
                                        (offer?.getAsk())!,
                                        (offer?.getOfferID())!,
                                        Int(digits))
                
                offersRow.append(offerRow)
                
            } else {
                let foundOffer = foundOffers.first
                foundOffer?.bidDirection = (bid! < (foundOffer?.bid)!) ? -1 : (bid! > (foundOffer?.bid)! ? 1 : 0)
                foundOffer?.askDirection = (ask! < (foundOffer?.ask)!) ? -1 : (ask! > (foundOffer?.ask)! ? 1 : 0)
                foundOffer?.ask = ask!
                foundOffer?.bid = bid!
                foundOffer?.isChanged = true;
            }
        }
        offerRowsLock.unlock()
        
        if let offersUpdateNotificatorValue = offersUpdateNotificator {
            offersUpdateNotificatorValue()
        }
        
    }
    
    func createOrder(offerIndex: Int, isBuy: Bool, amount: Int, rate: Double, orderType: Int) -> String? {
        let factory = session.getRequestFactory()
        let orderTypes = [O2G2_Orders_TrueMarketOpen, O2G2_Orders_StopEntry, O2G2_Orders_LimitEntry];
        
        let valueMap = factory?.createValueMap()
        
        valueMap?.setString(Command, O2G2_Commands_CreateOrder);
        valueMap?.setString(OrderType, orderTypes[orderType]);
        valueMap?.setString(AccountID, firstAccountID);
        valueMap?.setString(OfferID, offersRow[offerIndex].offerID);
        valueMap?.setString(BuySell, isBuy ? "B": "S");
        valueMap?.setInt(Amount, Int32(amount));
        valueMap?.setDouble(Rate, rate);
        valueMap?.setString(TimeInForce, "GTC");
        
        let request = factory?.createOrderRequest(valueMap)
        session.send(request)
        
        return request?.getID()
    }

    func setLoginData(user: String, pwd: String, url: String, connection: String) {
        self.user = user
        self.pwd = pwd
        self.url = url
        self.connection = connection
    }
    
    func setTradingStationDescriptors(sessionId: String, _ pin: String) {
        self.sessionId = sessionId
        self.pin = pin
    }
    
    func login(sessionId: String, pin: String) {
        cond.lock()
        
        print("Connect to: \(user) * \(url) \(connection) \(sessionId) \(pin)")
        session.login(user, pwd, url, connection)
    }
    
    func logout() {
        session.logout()
    }
    
    func clearCredintals() {
        user = ""
        pwd = ""
        url = ""
        connection = ""
        sessionId = ""
        pin = ""
        firstAccountID = ""
    }
    

    func setCAInfo(saFilePath: String) {
        O2GTransport.setCAInfo(saFilePath)
    }
    
    func waitForConnectionCompleted() -> Bool {
        let now = NSDate()
        cond.wait(until: now.addingTimeInterval(30) as Date)
        cond.unlock()
        return isConnected
    }
    
    func subscribeOffersUpdates(closure: @escaping () -> ()) {
        offersUpdateNotificator = closure;
    }
    
    func unsubscribeOffersUpdates() {
        offersUpdateNotificator = nil;
    }

    func offersCount() -> Int {
        offerRowsLock.lock()
        let result = offersRow.count
        offerRowsLock.unlock()
        return result
    }

    func getInstrument(index: Int) -> String {
        offerRowsLock.lock()
        var result: String
        if (index >= offersRow.count) {
            result = ""
        } else {
            result = offersRow[index].instr
        }
        offerRowsLock.unlock()
        return result
    }
    
    func getBid(index: Int) -> Double {
        offerRowsLock.lock()
        var result: Double
        if (index >= offersRow.count) {
            result = Double.nan
        } else {
            result = offersRow[index].bid
        }
        offerRowsLock.unlock()
        return result
        
    }
    
    func getAsk(index: Int) -> Double {
        offerRowsLock.lock()
        var result: Double
        if (index >= offersRow.count) {
            result = Double.nan
        } else {
            result = offersRow[index].ask
        }
        offerRowsLock.unlock()
        return result
        
    }
    
    func getBidDirection(index: Int) -> Int {
        offerRowsLock.lock()
        var result: Int
        if (index >= offersRow.count) {
            result = 0
        } else {
            result = offersRow[index].bidDirection
        }
        offerRowsLock.unlock()
        return result
        
    }
    
    func getAskDirection(index: Int) -> Int {
        offerRowsLock.lock()
        var result: Int
        if (index >= offersRow.count) {
            result = 0
        } else {
            result = offersRow[index].askDirection
        }
        offerRowsLock.unlock()
        return result
        
    }
    
    func getOfferID(index: Int) -> String {
        offerRowsLock.lock()
        var result: String
        if (index >= offersRow.count) {
            result = ""
        } else {
            result = offersRow[index].offerID
        }
        offerRowsLock.unlock()
        return result
        
    }
    
    func getDigits(index: Int) -> Int {
        offerRowsLock.lock()
        var result: Int
        if (index >= offersRow.count) {
            result = 0
        } else {
            result = offersRow[index].digits
        }
        offerRowsLock.unlock()
        return result
    }
}

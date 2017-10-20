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
    private var offersUpdateNotificator: () -> ()
    private var offersRow: Array<OfferRow>
    private var firstAccountID: String
    private var offerRowsLock: NSLock
    private var tableManager: IO2GTableManager?
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
        offersUpdateNotificator = { () -> () in  }
        O2GTransport.setNumberOfReconnections(uint.max)
        session = O2GTransport.createSession()
        session.subscribeSessionStatus(self)
        session.subscribeResponse(self)
        session.useTableManager(Yes, nil)
    }
    
    deinit {
        session.unsubscribeResponse(self)
        session.unsubscribeSessionStatus(self)
    }
    
    @objc func onLoginFailed(_ error: String!) {
        print("Login has been failed: \(error)")
    }
    
    @objc func onRequestCompleted(_ requestId: String!, _ response: IO2GResponse!) {
    }
    
    @objc func onRequestFailed(_ requestId: String!, _ error: String!) {
    }
    
    func getSession() -> IO2GSession {
        return session
    }
    
    @objc func onSessionStatusChanged(_ status: IO2GSessionStatus_O2GSessionStatus) {
                 
        switch status {
            
        case IO2GSession_Disconnected:
            print("Session status has been changed: Disconnected")
            statusNotificator(IO2GSession_Disconnected);
            isConnected = false
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
            
            let loginRules = session.getLoginRules()
            if loginRules != nil && (loginRules?.isTableLoaded(byDefault: Offers))! {
                let response = loginRules?.getTableRefreshResponse(Offers)
                onOffersTableReceived(response: response!)
            }
            
            tableManager = session.getTableManager()
            let accountsTable = tableManager?.getTable(Accounts) as! IO2GAccountsTable
            let accountRow = accountsTable.getRow(0) as IO2GAccountRow
            firstAccountID = accountRow.getAccountID()
            
            isConnected = true
            
            cond.signal()
        
        case IO2GSession_Reconnecting:
            statusNotificator(IO2GSession_Reconnecting)
            print("Session status has been changed: Reconnecting")
            
        case IO2GSession_SessionLost:
            statusNotificator(IO2GSession_SessionLost);
            print("Session status has been changed: SessionLost")
            isConnected = false
            self.login(sessionId: "", pin: "")
            
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

    func subscribeStatus(closure: @escaping (IO2GSessionStatus_O2GSessionStatus) -> ()) {
        statusNotificator = closure
    }
    
    @objc func onTablesUpdates(_ response: IO2GResponse!) {
        if response.getType() != TablesUpdates {
            return
        }

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
            if updatesReader?.getUpdateTable(i) != Offers {
                continue
            }
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
    }
    
    func onOffersTableReceived(response: IO2GResponse) {
        
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
            
            if offer?.getSubscriptionStatus().characters.first == "T" {
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
            else if offer?.getSubscriptionStatus().characters.first == "D" {
                var indexToRemove: Int?
                for i in 0..<offersRow.count {
                    if offersRow[i].offerID == offer?.getOfferID() {
                        indexToRemove = i
                        break
                    }
                }
                if let idx = indexToRemove {
                    offersRow.remove(at: idx)
                }
            }
        }
        offerRowsLock.unlock()
        
        offersUpdateNotificator()
        
    }
    
    func createOrder(offerIndex: Int, isBuy: Bool, amount: Int, rate: Double, orderType: Int) {
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
    
    func setCAInfo(saFilePath: String) {
        O2GTransport.setCAInfo(saFilePath)
    }
    
    func getTableManager() -> IO2GTableManager {
        return tableManager!
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

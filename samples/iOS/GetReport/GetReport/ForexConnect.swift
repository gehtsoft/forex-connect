import Foundation
import UIKit

class ForexConnect: IO2GSessionStatus
{
    private var ld: LoginData?
    private var session: IO2GSession
    private var loginCondition: NSCondition;
    private var sessionStatusChangedCallback: ((IO2GSessionStatus_O2GSessionStatus) -> ())?
    private var accountsRow: [IO2GAccountRow]
    private var accountsId: [String]
    private var isConnected = false
    
    var allAccountsId: [String] {
        get {
            return accountsId
        }
    }
    
    // MARK: - Shared instance (singleton)
    
    static let sharedInstance = ForexConnect()
    
    // MARK: - init-deinit
    
    private init() {
        accountsRow = [IO2GAccountRow]()
        accountsId = [String]()
        loginCondition = NSCondition()
        O2GTransport.setNumberOfReconnections(0)
        session = O2GTransport.createSession()
        session.subscribeSessionStatus(self)
    }
    
    deinit {
        session.unsubscribeSessionStatus(self)
    }
    
    // MARK: - Login-Logout
    
    func setLoginData(user: String, pwd: String, url: String, connection: String, sessionId: String) {
        ld = LoginData(user: user, pwd: pwd, url: url, connection: connection, sessionId: sessionId, pin: "")
    }
    
    func login() {
        loginCondition.lock()
        accountsId.removeAll()
        accountsRow.removeAll()
        session.login(ld!.user, ld!.pwd, ld!.url, ld!.connection)
    }
    
    func waitForConnectionCompleted() -> Bool {
        let now = Date()
        loginCondition.wait(until: now.addingTimeInterval(30))
        loginCondition.unlock()
        return isConnected
    }
    
    func logout() {
        session.logout()
    }
    
    // MARK: - IO2GSessionStatus listner and sesion status callback
    
    @objc func onSessionStatusChanged(_ status: IO2GSessionStatus_O2GSessionStatus) {
        autoreleasepool {
        switch status {
            
        case IO2GSession_Disconnected:
            print("Session status has been changed: Disconnected")
            sessionStatusChangedCallback?(IO2GSession_Disconnected);
            isConnected = false
            loginCondition.signal()
        
        case IO2GSession_Disconnecting:
            sessionStatusChangedCallback?(IO2GSession_Disconnecting);
            print("Session status has been changed: Disconnecting")
            
        case IO2GSession_Connecting:
            sessionStatusChangedCallback?(IO2GSession_Connecting);
            print("Session status has been changed: Connecting")
            
        case IO2GSession_Connected:
            sessionStatusChangedCallback?(IO2GSession_Connected);
            print("Session status has been changed: Connected")
            isConnected = true
            extractAllAccounts()
            loginCondition.signal()
        
        case IO2GSession_Reconnecting:
            sessionStatusChangedCallback?(IO2GSession_Reconnecting)
            print("Session status has been changed: Reconnecting")
            
        case IO2GSession_SessionLost:
            sessionStatusChangedCallback?(IO2GSession_SessionLost);
            print("Session status has been changed: SessionLost")
            isConnected = false
            
        case IO2GSession_TradingSessionRequested:
            sessionStatusChangedCallback?(IO2GSession_TradingSessionRequested)
            print("Session status has been changed: TradingSessionRequested")
            
            if ld!.sessionId.isEmpty {
                let descriptors = session.getTradingSessionDescriptors()
                if ((descriptors?.size())! > Int32(0) && ld!.sessionId.isEmpty) {
                    ld!.sessionId = (descriptors?.get(0).getID())!
                }
            }
            session.setTrading(ld!.sessionId, pin: ld?.pin)
            break
            
        default:
            print("Session status has been changed: Unknown")
        }
        }
    }
    
    @objc func onLoginFailed(_ error: String!) {
        print("Login has been failed: \(error ?? "unknown")")
    }
    
    func subscribeStatus(closure: @escaping (IO2GSessionStatus_O2GSessionStatus) -> ()) {
        sessionStatusChangedCallback = closure
    }
    
    // MARK: - GetReportURL
    
    private func extractAllAccounts() {
        autoreleasepool {
        let loginRules = session.getLoginRules()
        
        if loginRules == nil {
            return
        }
        
        let responseFactory = session.getResponseReaderFactory()
        let accountResponse = loginRules!.getTableRefreshResponse(Accounts)
        let accountsReader = responseFactory?.createAccountsTableReader(accountResponse)
        let size = accountsReader?.size()
        for i: Int32 in 0..<size! {
            let account = accountsReader?.getRow(i)
            accountsRow.append(account!)
            let accountId = account?.getAccountID()
            accountsId.append(accountId!)
        }
        }
    }
    
    func getReportURL(accountID: String, dateFrom: Date, dateTo: Date, reportFormat: ReportFormat, language: Language) -> String? {
        let oleDateFrom = O2GDateTimeUtils2.cocoaTime(toOleTime: dateFrom)
        
        if oleDateFrom == nil {
            return nil
        }
        let oleDateTo = O2GDateTimeUtils2.cocoaTime(toOleTime: dateTo)
        if oleDateTo == nil {
            return nil
        }
        let reportFormatStr = ReportFormat.toString(format: reportFormat)
        let languageStr = Language.toString(language: language)
        
        var url: String?
        
        url = session.getReportURL(accountID,
                                   dateFrom: (oleDateFrom?.doubleValue)!,
                                   dateTo: (oleDateTo?.doubleValue)!,
                                   format: reportFormatStr,
                                   langID: languageStr)
        
        return url
    }
}

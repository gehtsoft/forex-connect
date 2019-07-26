import Foundation

class CommissionsCalculator {
    private let COMMISSIONED_UI_ENABLED_PROP = "COMMISSIONED_UI_ENABLED"

    private var session: IO2GSession
    private var settingsProvider: IO2GTradingSettingsProvider?
    private var offersTable: IO2GOffersTable?
    private var accountTableRow: IO2GAccountTableRow?
    
    // the commissionsProvider will be initialized in the OffersViewController instance
    private lazy var commissionsProvider: IO2GCommissionsProvider = initializeCommProvider()
    
    private var _isCommEnabled: Bool = false
    
    public var isCommEnabled : Bool {
        get {
            return _isCommEnabled
        }
    }
    
    init(session: IO2GSession, accountTableRow: IO2GAccountTableRow?, offersTable: IO2GOffersTable) {
        if session.getStatus() != IO2GSession_Connected {
            fatalError("session_not_ready")
        }
        
        self.session = session
        self.offersTable = offersTable
        self.accountTableRow = accountTableRow;
        self._isCommEnabled = detectCommissionsSupport(session: session)
    }
    
    private func detectCommissionsSupport(session: IO2GSession) -> Bool {
        var loginRules = session.getLoginRules()
        while loginRules == nil { loginRules = session.getLoginRules() }
        self.settingsProvider = loginRules!.getTradingSettingsProvider()
        let response = loginRules!.getSystemPropertiesResponse()
        if response == nil {
            return false
        }
        let reader = session.getResponseReaderFactory().createSystemPropertiesReader(response);
        if reader == nil {
            return false
        }
        let commEnabledVal = reader?.findProperty(COMMISSIONED_UI_ENABLED_PROP)
        if commEnabledVal == nil {
            return false
        }
        
        return commEnabledVal! == "Y";
    }
    
    private func initializeCommProvider() -> IO2GCommissionsProvider {
        let commissionsProvider = session.getCommissionsProvider()
        var downCounter = 25
        while (commissionsProvider!.getStatus() != CommissionStatusReady && downCounter > 0)
        {
            if commissionsProvider!.getStatus() == CommissionStatusDisabled { break }
            if commissionsProvider!.getStatus() == CommissionStatusFailToLoad { break }
            usleep(50000) // 50 millis
            downCounter -= 1
        }
        return commissionsProvider!
    }
    
    private func findOffersRow(offerID: String) -> IO2GOfferTableRow? {
        return offersTable!.findRow(offerID)
    }
    
    private func calcAmount(offersTableRow: IO2GOfferTableRow, lots: Int32) -> Int32 {
        let baseUnitSize = settingsProvider!.getBaseUnitSize(offersTableRow.getInstrument(), accountTableRow);
        return baseUnitSize * lots;
    }
    
    public func calcOpenCommission(offerID: String, lots: Int32, isBuy: Bool) -> Double {
        
        let offersTableRow = findOffersRow(offerID: offerID)
        
        if accountTableRow == nil || offersTableRow == nil {
            return Double.nan
        }
        
        let amount = calcAmount(offersTableRow: offersTableRow!, lots: lots)
        let buySell = isBuy ? "B" : "S"
        return commissionsProvider.calcOpenCommission(offersTableRow, accountTableRow, amount, buySell, 0);
    }
    
    public func calcCloseCommission(offerID: String, lots: Int32, isBuy: Bool) -> Double {
        
        let offersTableRow = findOffersRow(offerID: offerID)
        
        if accountTableRow == nil || offersTableRow == nil {
            return Double.nan
        }
        
        let amount = calcAmount(offersTableRow: offersTableRow!, lots: lots)
        let buySell = isBuy ? "B" : "S"
        return commissionsProvider.calcCloseCommission(offersTableRow, accountTableRow, amount, buySell, 0);
    }
    
    public func calcTotalCommission(offerID: String, lots: Int32, isBuy: Bool) -> Double {
        
        let offersTableRow = findOffersRow(offerID: offerID)
        
        if accountTableRow == nil || offersTableRow == nil {
            return Double.nan
        }
        
        let amount = calcAmount(offersTableRow: offersTableRow!, lots: lots)
        let buySell = isBuy ? "B" : "S"
        return commissionsProvider.calcTotalCommission(offersTableRow, accountTableRow, amount, buySell, 0, 0);
    }
}

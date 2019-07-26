import Foundation
import UIKit


class AccountsTableRow
{
    public let accountID: String;
    public let equity: String;
    public let dayPL: String;
    public let usdMr: String;
    public let usableMr: String;
    public let usableMrInPersent: String;
    public let grossPL: String;
    public let marginCall: String;
    public let type: String;
    
    public var accountsRowData: [(String, String)]
    
    init(accountID: String, equity: Double, dayPL: Double, usdMr: Double, usableMr: Double, usableMrInPersent: Int, grossPL: Double, marginCall: String, type: String) {
        self.accountID = accountID
        self.equity = "\(equity)"
        self.dayPL = "\(dayPL)"
        self.grossPL = "\(grossPL)"
        self.usdMr = "\(usdMr)"
        self.usableMr = "\(usableMr)"
        self.usableMrInPersent = "\(usableMrInPersent)"
        self.marginCall = marginCall
        self.type = type
        
        accountsRowData = [(String, String)]()
        
        accountsRowData.append(("Equity", self.equity))
        accountsRowData.append(("Day P/L", self.dayPL))
        accountsRowData.append(("Gross P/L", self.grossPL))
        accountsRowData.append(("Usd Mr", self.usdMr))
        accountsRowData.append(("Usable Mr", self.usableMr))
        accountsRowData.append(("Usable Mr, %", self.usableMrInPersent))
        accountsRowData.append(("Margin Call", self.marginCall == "N" ? "No" : "Yes"))
        accountsRowData.append(("Type", self.type))
    }
}

class AccountsTable : IO2GTableListener, IO2GEachRowListener
{
    var accountsTable: IO2GAccountsTable
    var accountsUpdateNotificator: (([AccountsTableRow]) -> ())?
    var accountsTableRows = [String:AccountsTableRow]()
    
    init() {
        let tableManager = ForexConnect.sharedInstance().tableManager!
        accountsTable = tableManager.getTable(Accounts) as! IO2GAccountsTable
    }
    
    func load() {
        accountsTable.forEachRow(self)
        accountsTable.subscribeUpdate(Insert, self)
        accountsTable.subscribeUpdate(Update, self)
        accountsTable.subscribeUpdate(Delete, self)
    }
    
    deinit {
        let tableManager = ForexConnect.sharedInstance().tableManager!
        accountsTable = tableManager.getTable(Accounts) as! IO2GAccountsTable
        accountsTable.unsubscribeUpdate(Insert, self)
        accountsTable.unsubscribeUpdate(Update, self)
        accountsTable.unsubscribeUpdate(Delete, self)
    }
    
    func notify() {
        let rows = getAccountsTableRows()
        let sorderArray = rows.sorted { (left, right) -> Bool in
            left.accountID > right.accountID
        }
        if let accountsUpdateNotificatorValue = accountsUpdateNotificator {
            accountsUpdateNotificatorValue(sorderArray)
        }
    }
    
    func onEachRow(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as? IO2GAccountTableRow
        addAccount(accRow: row!)
        notify()
    }
    
    private func addAccount(accRow: IO2GAccountTableRow) {
        
        let accountID = accRow.getAccountID()
        let equity = accRow.getEquity()
        let dayPL = accRow.getDayPL()
        let usdMr = accRow.getUsedMargin()
        let usableMr = accRow.getUsableMargin()
        let usableMrInPersent = accRow.getUsableMarginInPercentage()
        let grossPL = accRow.getGrossPL()
        let marginCall = accRow.getMarginCallFlag()
        let type = accRow.getMaintenanceType()
        
        let myTableRow = AccountsTableRow(accountID: accountID!, equity: equity, dayPL: dayPL, usdMr: usdMr, usableMr: usableMr, usableMrInPersent: Int(usableMrInPersent), grossPL: grossPL, marginCall: marginCall!, type: type!)
        accountsTableRows[accountID!] = myTableRow
    }
    
    private func removeAccount(accountID: String) {
        
        if accountsTableRows[accountID] != nil {
            accountsTableRows.removeValue(forKey: accountID)
        }
    }
    
    func subscribeAccountsUpdates(closure: @escaping ([AccountsTableRow]) -> ()) {
        accountsUpdateNotificator = closure;
    }
    
    func unsubscribeAccountsUpdates() {
        accountsUpdateNotificator = nil;
    }
    
    func onAdded(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GAccountTableRow;
        addAccount(accRow: row)
        notify()
    }
    
    func onChanged(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GAccountTableRow;
        removeAccount(accountID: row.getAccountID())
        addAccount(accRow: row)
        notify()
    }
    
    func onDeleted(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GAccountTableRow;
        removeAccount(accountID: row.getAccountID())
        notify()
    }
    
    func onStatusChanged(_ status: O2GTableStatus) {
    }
    
    private func getAccountsTableRows() -> [AccountsTableRow] {
        let result = accountsTableRows.map { $0.value }
        return result
    }
}


class AccountsViewController : UITableViewController
{
    private var accountsTableRows = [AccountsTableRow]()
    private var accountsTable: AccountsTable?
    
    override func viewDidLoad() {
        super.viewDidLoad()

        accountsTable = AccountsTable()
        
        accountsTable?.subscribeAccountsUpdates(closure: accountsUpdated)
        accountsTable?.load()
        
        self.tableView.register(UITableViewCell.self, forCellReuseIdentifier: "cell")
        self.tableView.dataSource = self;
        self.tableView.delegate = self;
        self.title = "Accounts";
    }
    
    override func viewDidDisappear(_ animated: Bool) {
        super.viewDidDisappear(animated)
        accountsTable!.unsubscribeAccountsUpdates()
        accountsTable = nil
        accountsTableRows.removeAll()
    }
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return accountsTableRows.count
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        if (accountsTableRows.count > 0) {
            return accountsTableRows[0].accountsRowData.count
        }
        return 0;
    }
    
    override func tableView(_ tableView: UITableView, titleForHeaderInSection section: Int) -> String? {
        if section > accountsTableRows.count {
            return nil
        }
        return "Account ID: " + accountsTableRows[section].accountID
    }
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = self.tableView.dequeueReusableCell(withIdentifier: "cell")! as UITableViewCell
  
        if indexPath.section > accountsTableRows.count {
            return cell
        }
        
        let myAccRow = accountsTableRows[indexPath.section]
        let myAccRowData = myAccRow.accountsRowData
        
        cell.textLabel?.text = "    " + myAccRowData[indexPath.row].0 + ": " + myAccRowData[indexPath.row].1
        
        return cell
    }
    
    
    func accountsUpdated(accountsTableRows: [AccountsTableRow]) {
        weak var _self = self
        DispatchQueue.main.async {
            _self?.accountsTableRows = accountsTableRows
            _self?.tableView.reloadData()
        }
    }
    
}

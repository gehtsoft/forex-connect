import Foundation
import UIKit

class SummaryTableRow
{
    public let offerId: Int64
    public let instrument: String
    public let amount: String
    public let closeSell: String
    public let closeBuy: String
    public let grossPL: String
    public let close: String
    
    init(offerId: String, instrument: String, amount: String, closeBuy: String, closeSell: String, grossPL: String) {
        self.offerId = (offerId as NSString).longLongValue
        self.instrument = instrument
        self.amount = amount
        self.closeBuy = closeBuy
        self.closeSell = closeSell
        self.grossPL = grossPL
        self.close = closeSell == "0.0" ? closeBuy : closeSell
    }
}

class SummaryTable : IO2GTableListener, IO2GEachRowListener
{
    var summaryTable: IO2GSummaryTable
    var tradesTable: IO2GTradesTable
    var offersTable: IO2GOffersTable
    var summaryUpdateNotificator: (([SummaryTableRow]) -> ())?
    var summaryTableRows = [String:SummaryTableRow]()
    
    init() {
        let tableManager = ForexConnect.sharedInstance().tableManager!
        tradesTable = (tableManager.getTable(Trades) as? IO2GTradesTable)!
        offersTable = (tableManager.getTable(Offers) as? IO2GOffersTable)!
        summaryTable = (tableManager.getTable(Summary) as? IO2GSummaryTable)!
    }
    
    func load() {
        summaryTable.forEachRow(self)
        summaryTable.subscribeUpdate(Insert, self)
        summaryTable.subscribeUpdate(Update, self)
        summaryTable.subscribeUpdate(Delete, self)
    }
    
    deinit {
        summaryTable.unsubscribeUpdate(Insert, self)
        summaryTable.unsubscribeUpdate(Update, self)
        summaryTable.unsubscribeUpdate(Delete, self)
    }
    
    func notify() {
        let rows = getSummaryTableRows()
        let sorderArray = rows.sorted { (left, right) -> Bool in
            left.offerId > right.offerId
        }
        if let summaryUpdateNotificatorValue = summaryUpdateNotificator {
            summaryUpdateNotificatorValue(sorderArray)
        }
    }
    
    func onEachRow(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as? IO2GSummaryTableRow
        addSummary(row: row!)
        notify()
    }
    
    private func addSummary(row: IO2GSummaryTableRow) {
        
        let offerId = row.getOfferID()
        let offerRow = offersTable.findRow(offerId)
        
        let instrument = offerRow!.getInstrument()
        let amount = String(row.getAmount())
        let grossPL = row.getGrossPL().parseToPlaces(places: 2)
        let closeBuy = row.getBuyClose().parseToPlaces(places: Int(offerRow!.getDigits()))
        let closeSell = row.getSellClose().parseToPlaces(places: Int(offerRow!.getDigits()))
        
        let tableRow: SummaryTableRow = SummaryTableRow(offerId: offerId!, instrument: instrument!, amount: amount, closeBuy: closeBuy, closeSell: closeSell, grossPL: grossPL)
        summaryTableRows[offerId!] = tableRow
    }
    
    private func removeSummary(offerId: String) {
        if summaryTableRows[offerId] != nil {
            summaryTableRows.removeValue(forKey: offerId)
        }
    }
    
    func subscribeOrdersUpdates(closure: @escaping ([SummaryTableRow]) -> ()) {
        summaryUpdateNotificator = closure;
    }
    
    func unsubscribeSummaryUpdates() {
        summaryUpdateNotificator = nil;
    }
    
    func onAdded(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GSummaryTableRow;
        addSummary(row: row)
        notify()
    }
    
    func onChanged(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GSummaryTableRow;
        removeSummary(offerId: row.getOfferID())
        addSummary(row: row)
        notify()
    }
    
    func onDeleted(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GSummaryTableRow;
        removeSummary(offerId: row.getOfferID())
        notify()
    }
    
    func onStatusChanged(_ status: O2GTableStatus) {
    }
    
    private func getSummaryTableRows() -> [SummaryTableRow] {
        let result = summaryTableRows.map { $0.value }
        return result
    }
}


class SummaryViewController : UITableViewController
{
    private var summaryTable: SummaryTable?
    private var summaryTableRows = [SummaryTableRow]()
    
    let headerLabel: UILabel = UILabel(frame: CGRect(x: 30.0, y: 10.0, width: 600.0, height: 25.0))
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        summaryTable = SummaryTable()
        
        headerLabel.font = UIFont.boldSystemFont(ofSize: 18)
        headerLabel.textAlignment = NSTextAlignment.left
        headerLabel.textColor = UIColor.black
        headerLabel.backgroundColor = UIColor.gray
        headerLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
        headerLabel.text = "    Symbol       Amount       Close      GrossPL "
        
        self.tableView.register(UITableViewCell.self, forCellReuseIdentifier: "cell")
        self.tableView.dataSource = self;
        self.tableView.delegate = self;
        self.title = "Summary";
        
        summaryTable!.subscribeOrdersUpdates(closure: ordersUpdated)
        summaryTable!.load()
    }
    
    override func viewDidDisappear(_ animated: Bool) {
        super.viewDidDisappear(animated)
        
        summaryTable!.unsubscribeSummaryUpdates()
        summaryTable = nil
        summaryTableRows.removeAll()
    }
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return 1;
    }
    override func tableView(_ tableView: UITableView, heightForHeaderInSection section: Int) -> CGFloat {
        return 40.0
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return summaryTableRows.count
    }
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = self.tableView.dequeueReusableCell(withIdentifier: "cell")! as UITableViewCell
        
        if indexPath.row > summaryTableRows.count {
            return cell
        }
        
        let instr = summaryTableRows[indexPath.row].instrument
        let amount = summaryTableRows[indexPath.row].amount
        let close = summaryTableRows[indexPath.row].close
        let grossPL = summaryTableRows[indexPath.row].grossPL
        
        cell.textLabel!.font = UIFont.systemFont(ofSize: 14.0)
        
        if cell.contentView.viewWithTag(2) == nil {
            
            let amountLabel = UILabel(frame: CGRect(x: 100.0, y: 10.0, width: 70.0, height: 25.0))
            amountLabel.tag = 2
            amountLabel.textAlignment = NSTextAlignment.right
            amountLabel.font = UIFont.systemFont(ofSize: 14.0)
            amountLabel.textColor = UIColor.black
            amountLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(amountLabel)
            
            let closeLabel = UILabel(frame: CGRect(x: 180.0, y: 10.0, width: 90.0, height: 25.0))
            closeLabel.tag = 3
            closeLabel.textAlignment = NSTextAlignment.right
            closeLabel.font = UIFont.systemFont(ofSize: 14.0)
            closeLabel.textColor = UIColor.black
            closeLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(closeLabel)
            
            let grossPLLabel = UILabel(frame: CGRect(x: 280.0, y: 10.0, width: 80.0, height: 25.0))
            grossPLLabel.tag = 4
            grossPLLabel.textAlignment = NSTextAlignment.right
            grossPLLabel.font = UIFont.systemFont(ofSize: 14.0)
            grossPLLabel.textColor = UIColor.black
            grossPLLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(grossPLLabel)
        }
        
        let instrumentLabel = cell.textLabel!
        let amountLabel = cell.contentView.viewWithTag(2) as! UILabel
        let closeLabel = cell.contentView.viewWithTag(3) as! UILabel
        let grossPLLabel = cell.contentView.viewWithTag(4) as! UILabel
        
        instrumentLabel.text = instr
        amountLabel.text = amount
        closeLabel.text = close
        grossPLLabel.text = grossPL
        
        return cell
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    override func tableView(_ tableView: UITableView, viewForHeaderInSection section: Int) -> UIView? {
        return headerLabel
    }
    
    func ordersUpdated(summaryTableRows: [SummaryTableRow]) {
        weak var _self = self
        DispatchQueue.main.async {
            _self?.summaryTableRows = summaryTableRows
            _self?.tableView.reloadData()
        }
    }
    
}

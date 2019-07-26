import Foundation
import UIKit

class TradesTableRow : Comparable
{
    public let tradesId: String
    public let instrument: String
    public let amount: Int
    public let buySell: String
    public let pl: Double
    public let stop: Double
    
    public let parsedPL: String
    public let parsedAmount: String
    public let parsedStop: String
    
    private let numberByOrder: Int64

    init(tradesId: String, instrument: String, buySell: String, amount: Int, pl: Double, stop: Double, isTotalRow: Bool) {
        self.tradesId = tradesId
        self.instrument = instrument
        self.buySell = buySell
        self.amount = amount
        self.pl = pl
        self.stop = stop
        self.numberByOrder = NSString(string: tradesId).longLongValue
        
        self.parsedPL = pl.parseToPlaces(places: 1)
        self.parsedAmount = String(amount)
        
        if isTotalRow {
            self.parsedStop = ""
        } else {
            let digits = ForexConnect.sharedInstance().getDigits(instr: instrument)!
            let dStop = stop.parseToPlaces(places: digits)
            self.parsedStop = (stop == 0 ? "" : String(dStop))
        }
    }
    
    static func < (lhs: TradesTableRow, rhs: TradesTableRow) -> Bool {
        return lhs.numberByOrder < lhs.numberByOrder
    }
    
    static func == (lhs: TradesTableRow, rhs: TradesTableRow) -> Bool {
        return lhs.numberByOrder == lhs.numberByOrder
    }
}

class TradesTable : IO2GTableListener, IO2GEachRowListener
{
    var tradesTable: IO2GTradesTable
    var offersTable: IO2GOffersTable
    var tradesUpdateNotificator: (([TradesTableRow]) -> ())?
    var tradesTableRows = [String:TradesTableRow]()
    
    init() {
        let tableManager = ForexConnect.sharedInstance().tableManager!
        tradesTable = (tableManager.getTable(Trades) as? IO2GTradesTable)!
        offersTable = (tableManager.getTable(Offers) as? IO2GOffersTable)!
    }
    
    func load() {
        tradesTable.forEachRow(self)
        tradesTable.subscribeUpdate(Insert, self)
        tradesTable.subscribeUpdate(Update, self)
        tradesTable.subscribeUpdate(Delete, self)
    }
    
    deinit {
        let tableManager = ForexConnect.sharedInstance().tableManager!
        tradesTable = (tableManager.getTable(Trades) as? IO2GTradesTable)!
        tradesTable.unsubscribeUpdate(Insert, self)
        tradesTable.unsubscribeUpdate(Update, self)
        tradesTable.unsubscribeUpdate(Delete, self)
    }
    
    func notify() {
        let rows = getTradesTableRows()
        let sorderArray = rows.sorted { (left, right) -> Bool in
            left.tradesId > right.tradesId
        }
        if let tradesUpdateNotificatorValue = tradesUpdateNotificator {
            tradesUpdateNotificatorValue(sorderArray)
        }
    }

    func onEachRow(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as? IO2GTradeTableRow
        addTrade(row: row!)
        notify()
    }
    
    private func addTrade(row: IO2GTradeTableRow) {
        
        let offerId = row.getOfferID()
        let offerRow = offersTable.findRow(offerId)
        
        let instrument = offerRow!.getInstrument()
        let tradeId = row.getTradeID()
        let amount: Int = Int(row.getAmount())
        let buySell = row.getBuySell()
        let pl = row.getPL()
        let stop = row.getStop()
        let tradesId = row.getTradeID()
    
        let tableRow: TradesTableRow = TradesTableRow(tradesId: tradesId!, instrument: instrument!, buySell: buySell!, amount: amount, pl: pl, stop: stop, isTotalRow: false)
        tradesTableRows[tradeId!] = tableRow
    }
    
    private func removeTrade(tradeId: String) {
        
        if tradesTableRows[tradeId] != nil {
            tradesTableRows.removeValue(forKey: tradeId)
        }
    }
    
    func subscribeTradesUpdates(closure: @escaping ([TradesTableRow]) -> ()) {
        tradesUpdateNotificator = closure;
    }
    
    func unsubscribeTradesUpdates() {
        tradesUpdateNotificator = nil;
    }
    
    func onAdded(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GTradeTableRow;
        addTrade(row: row)
        notify()
    }
    
    func onChanged(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GTradeTableRow;
        removeTrade(tradeId: row.getTradeID())
        addTrade(row: row)
        notify()
    }
    
    func onDeleted(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GTradeTableRow;
        removeTrade(tradeId: row.getTradeID())
        notify()
    }
    
    func onStatusChanged(_ status: O2GTableStatus) {
    }
    
    private func getTradesTableRows() -> [TradesTableRow] {
        let result = tradesTableRows.map { $0.value }
        return result
    }
}


class TradesViewController : UITableViewController
{
    private var tradesTable: TradesTable?
    private var tradesTableRows = [TradesTableRow]()

    let headerLabel: UILabel = UILabel(frame: CGRect(x: 30.0, y: 10.0, width: 600.0, height: 25.0))
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        tradesTable = TradesTable()
        
        headerLabel.font = UIFont.boldSystemFont(ofSize: 18)
        headerLabel.textAlignment = NSTextAlignment.left
        headerLabel.textColor = UIColor.black
        headerLabel.backgroundColor = UIColor.gray
        headerLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
        headerLabel.text = "    Symbol     Amount       S/B       P/L      Stop "
        
        self.tableView.register(UITableViewCell.self, forCellReuseIdentifier: "cell")
        self.tableView.dataSource = self;
        self.tableView.delegate = self;
        self.title = "Trades";
        tradesTable!.subscribeTradesUpdates(closure: tradesUpdated)
        tradesTable!.load()
    }
    
    override func viewDidDisappear(_ animated: Bool) {
        super.viewDidDisappear(animated)

        tradesTable!.unsubscribeTradesUpdates()
        tradesTable = nil
        tradesTableRows.removeAll()
    }
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return 1;
    }
    override func tableView(_ tableView: UITableView, heightForHeaderInSection section: Int) -> CGFloat {
        return 40.0
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        // one row is total PL
        if tradesTableRows.count == 1 {
            return 0;
        } else {
            return tradesTableRows.count
        }
    }
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = self.tableView.dequeueReusableCell(withIdentifier: "cell")! as UITableViewCell
        
        if indexPath.row > tradesTableRows.count {
            return cell
        }
                
        let instr = tradesTableRows[indexPath.row].instrument
        let buySell = tradesTableRows[indexPath.row].buySell
        let amount = tradesTableRows[indexPath.row].parsedAmount
        let pl = tradesTableRows[indexPath.row].parsedPL
        let stop = tradesTableRows[indexPath.row].parsedStop
        
        if cell.contentView.viewWithTag(2) == nil {
            
            let amountLabel = UILabel(frame: CGRect(x: 100.0, y: 10.0, width: 70.0, height: 25.0))
            amountLabel.tag = 2
            amountLabel.textAlignment = NSTextAlignment.right
            amountLabel.font = UIFont.systemFont(ofSize: 16.0)
            amountLabel.textColor = UIColor.black
            amountLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(amountLabel)
            
            let buySellLable = UILabel(frame: CGRect(x: 180.0, y: 10.0, width: 40.0, height: 25.0))
            buySellLable.tag = 3
            buySellLable.textAlignment = NSTextAlignment.right
            buySellLable.font = UIFont.systemFont(ofSize: 16.0)
            buySellLable.textColor = UIColor.black
            buySellLable.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(buySellLable)
            
            let plLable = UILabel(frame: CGRect(x: 230.0, y: 10.0, width: 60.0, height: 25.0))
            plLable.tag = 4
            plLable.textAlignment = NSTextAlignment.right
            plLable.font = UIFont.systemFont(ofSize: 16.0)
            plLable.textColor = UIColor.black
            plLable.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(plLable)
            
            let stopLabel = UILabel(frame: CGRect(x: 280.0, y: 10.0, width: 80.0, height: 25.0))
            stopLabel.tag = 5
            stopLabel.textAlignment = NSTextAlignment.right
            stopLabel.font = UIFont.systemFont(ofSize: 16.0)
            stopLabel.textColor = UIColor.black
            stopLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(stopLabel)
        }
        
        let instrumentLabel = cell.textLabel!
        let amountLabel = cell.contentView.viewWithTag(2) as! UILabel
        let buySellLable = cell.contentView.viewWithTag(3) as! UILabel
        let plLable = cell.contentView.viewWithTag(4) as! UILabel
        let stopLabel = cell.contentView.viewWithTag(5) as! UILabel
        
        instrumentLabel.text = instr
        amountLabel.text = amount
        buySellLable.text = buySell
        stopLabel.text = stop
        plLable.text = pl
        
        return cell
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    override func tableView(_ tableView: UITableView, viewForHeaderInSection section: Int) -> UIView? {
        return headerLabel
    }

    func tradesUpdated(tradesTableRows: [TradesTableRow]) {
        weak var _self = self
        DispatchQueue.main.async {
            // calc total PL
            var summ = 0.0
            for tradeRow in tradesTableRows {
                summ += tradeRow.pl
            }
            // add total row
            let totalRow = TradesTableRow(tradesId: "", instrument: "Total", buySell: "", amount: 0, pl: summ, stop: 0, isTotalRow: true)
            
            var tradesTableRowsWithTotal = [TradesTableRow]()
            tradesTableRowsWithTotal.append(contentsOf: tradesTableRows)
            tradesTableRowsWithTotal.append(totalRow)
            
            _self?.tradesTableRows = tradesTableRowsWithTotal
            _self?.tableView.reloadData()
        }
    }

}














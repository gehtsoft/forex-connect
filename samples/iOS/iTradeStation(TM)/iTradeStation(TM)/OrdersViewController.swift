import Foundation
import UIKit

class OrdersTableRow
{
    public let orderId: Int64
    public let instrument: String
    public let status: String
    public let amount: String
    public let orderType: String
    
    init(orderId: String, instrument: String, amount: String, status: String, orderType: String) {
        self.orderId = (orderId as NSString).longLongValue
        self.instrument = instrument
        self.amount = amount
        self.status = status
        self.orderType = orderType
    }
}

class OrdersTable : IO2GTableListener, IO2GEachRowListener
{
    var ordersTable: IO2GOrdersTable
    var offersTable: IO2GOffersTable
    var ordersUpdateNotificator: (([OrdersTableRow]) -> ())?
    var ordersTableRows = [String:OrdersTableRow]()
    
    init() {
        let tableManager = ForexConnect.getSharedInstance().getTableManager()
        // ORDERS table depends on TRADES table, so TRADES table must be refreshed already
        ordersTable = (tableManager.getTable(Orders) as? IO2GOrdersTable)!
        offersTable = (tableManager.getTable(Offers) as? IO2GOffersTable)!
    }
    
    func load() {
        ordersTable.forEachRow(self)
        ordersTable.subscribeUpdate(Insert, self)
        ordersTable.subscribeUpdate(Update, self)
        ordersTable.subscribeUpdate(Delete, self)
    }
    
    deinit {
        let tableManager = ForexConnect.getSharedInstance().getTableManager()
        ordersTable = (tableManager.getTable(Orders) as? IO2GOrdersTable)!
        ordersTable.unsubscribeUpdate(Insert, self)
        ordersTable.unsubscribeUpdate(Update, self)
        ordersTable.unsubscribeUpdate(Delete, self)
    }
    
    func notify() {
        let rows = getOrdersTableRows()
        let sorderArray = rows.sorted { (left, right) -> Bool in
            left.orderId > right.orderId
        }
        if let ordersUpdateNotificatorValue = ordersUpdateNotificator {
            ordersUpdateNotificatorValue(sorderArray)
        }
    }
    
    func onEachRow(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as? IO2GOrderTableRow
        addOrder(row: row!)
        notify()
    }
    
    private func addOrder(row: IO2GOrderTableRow) {
        
        let offerId = row.getOfferID()
        let offerRow = offersTable.findRow(offerId)
        
        let instrument = offerRow?.getInstrument()
        let ordersId = row.getOrderID()
        let status = row.getStatus()
        let amount = String(row.getAmount())
        let orderType = getContingencyTypeString(contingencyType: Int(row.getContingencyType()))
        
        let tableRow: OrdersTableRow = OrdersTableRow(orderId: ordersId!, instrument: instrument!, amount: amount, status: status!, orderType: orderType)
        ordersTableRows[ordersId!] = tableRow
    }
    
    private func getContingencyTypeString(contingencyType: Int) -> String {
        if contingencyType == 1 {
            return "OCO"
        } else if contingencyType == 2 {
            return "OTO"
        } else if contingencyType == 3 {
            return "ELS"
        }  else if contingencyType == 4 {
            return "SE"
        } else {
            return ""
        }
    }
    
    private func removeOrder(orderId: String) {
        if ordersTableRows[orderId] != nil {
            ordersTableRows.removeValue(forKey: orderId)
        }
    }
    
    func subscribeOrdersUpdates(closure: @escaping ([OrdersTableRow]) -> ()) {
        ordersUpdateNotificator = closure;
    }
    
    func unsubscribeOrdersUpdates() {
        ordersUpdateNotificator = nil;
    }
    
    func onAdded(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GOrderTableRow;
        addOrder(row: row)
        notify()
    }
    
    func onChanged(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GOrderTableRow;
        removeOrder(orderId: row.getOrderID())
        addOrder(row: row)
        notify()
    }
    
    func onDeleted(_ rowID: String!, _ rowData: IO2GRow!) {
        let row = rowData as! IO2GOrderTableRow;
        removeOrder(orderId: row.getOrderID())
        notify()
    }
    
    func onStatusChanged(_ status: O2GTableStatus) {
    }
    
    private func getOrdersTableRows() -> [OrdersTableRow] {
        let result = ordersTableRows.map { $0.value }
        return result
    }
}


class OrdersViewController : UITableViewController
{
    private var ordersTable: OrdersTable?
    private var ordersTableRows = [OrdersTableRow]()
    
    let headerLabel: UILabel = UILabel(frame: CGRect(x: 30.0, y: 10.0, width: 600.0, height: 25.0))
    
    override func viewDidLoad() {
        super.viewDidLoad()

        ordersTable = OrdersTable()
        
        headerLabel.font = UIFont.boldSystemFont(ofSize: 18)
        headerLabel.textAlignment = NSTextAlignment.left
        headerLabel.textColor = UIColor.black
        headerLabel.backgroundColor = UIColor.gray
        headerLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
        headerLabel.text = "    Symbol      Amount       Status     T "
        
        self.tableView.register(UITableViewCell.self, forCellReuseIdentifier: "cell")
        self.tableView.dataSource = self;
        self.tableView.delegate = self;
        self.title = "Orders";
        
        ordersTable!.subscribeOrdersUpdates(closure: ordersUpdated)
        ordersTable!.load()
    }
    
    override func viewDidDisappear(_ animated: Bool) {
        super.viewDidDisappear(animated)
        
        ordersTable!.unsubscribeOrdersUpdates()
        ordersTable = nil
        ordersTableRows.removeAll()
    }
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return 1;
    }
    override func tableView(_ tableView: UITableView, heightForHeaderInSection section: Int) -> CGFloat {
        return 40.0
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return ordersTableRows.count
    }
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = self.tableView.dequeueReusableCell(withIdentifier: "cell")! as UITableViewCell
        
        if indexPath.row > ordersTableRows.count {
            return cell
        }
                
        let instr = ordersTableRows[indexPath.row].instrument
        let status = ordersTableRows[indexPath.row].status
        let amount = ordersTableRows[indexPath.row].amount
        let type = ordersTableRows[indexPath.row].orderType
        
        
        if cell.contentView.viewWithTag(2) == nil {
            
            let amountLabel = UILabel(frame: CGRect(x: 100.0, y: 10.0, width: 70.0, height: 25.0))
            amountLabel.tag = 2
            amountLabel.textAlignment = NSTextAlignment.right
            amountLabel.font = UIFont.systemFont(ofSize: 16.0)
            amountLabel.textColor = UIColor.black
            amountLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(amountLabel)
            
            let statusLabel = UILabel(frame: CGRect(x: 220.0, y: 10.0, width: 20.0, height: 25.0))
            statusLabel.tag = 3
            statusLabel.textAlignment = NSTextAlignment.right
            statusLabel.font = UIFont.systemFont(ofSize: 16.0)
            statusLabel.textColor = UIColor.black
            statusLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(statusLabel)
            
            let orderTypeLabel = UILabel(frame: CGRect(x: 240.0, y: 10.0, width: 60.0, height: 25.0))
            orderTypeLabel.tag = 4
            orderTypeLabel.textAlignment = NSTextAlignment.right
            orderTypeLabel.font = UIFont.systemFont(ofSize: 16.0)
            orderTypeLabel.textColor = UIColor.black
            orderTypeLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(orderTypeLabel)
        }
        
        let instrumentLabel = cell.textLabel!
        let amountLabel = cell.contentView.viewWithTag(2) as! UILabel
        let statusLabel = cell.contentView.viewWithTag(3) as! UILabel
        let orderTypeLabel = cell.contentView.viewWithTag(4) as! UILabel
        
        instrumentLabel.text = instr
        amountLabel.text = amount
        statusLabel.text = status
        orderTypeLabel.text = type
        
        return cell
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    override func tableView(_ tableView: UITableView, viewForHeaderInSection section: Int) -> UIView? {
        return headerLabel
    }
    
    func ordersUpdated(ordersTableRows: [OrdersTableRow]) {
        weak var _self = self
        DispatchQueue.main.async {
            _self?.ordersTableRows = ordersTableRows
            _self?.tableView.reloadData()
        }
    }
    
}

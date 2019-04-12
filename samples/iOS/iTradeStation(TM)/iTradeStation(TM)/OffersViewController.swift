import UIKit

class OffersViewController : UITableViewController {
    
    var headerLabel: UILabel?
    
    var currentHeaderLabel: UIView?
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        self.title = "Rates";
        
        self.tableView.register(OffersTableCellCommEnabled.self, forCellReuseIdentifier: "OffersCellCommEnabled")
        self.tableView.register(OffersTableCellCommDisabled.self, forCellReuseIdentifier: "OffersCellCommDisabled")
        self.tableView.dataSource = self;
        self.tableView.delegate = self;
    }
    
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        let forexConnect = ForexConnect.getSharedInstance()
        let commCalc = forexConnect.commissionsCalc!
        headerLabel = createHeaderLabel(isCommEnabled: commCalc.isCommEnabled)
        forexConnect.subscribeOffersUpdates(closure: offersUpdated)
    }
    
    override func viewDidDisappear(_ animated: Bool) {
        super.viewDidDisappear(animated)
        
        let forexConnect = ForexConnect.getSharedInstance()
        forexConnect.unsubscribeOffersUpdates()
    }
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return 1;
    }
    
    override func tableView(_ tableView: UITableView, viewForHeaderInSection section: Int) -> UIView? {
        return headerLabel
    }
    
    override func tableView(_ tableView: UITableView, heightForHeaderInSection section: Int) -> CGFloat {
        return 40.0
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        let forexConnect = ForexConnect.getSharedInstance()
        return forexConnect.offersCount()
    }
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {

        let forexConnect = ForexConnect.getSharedInstance()
        let IsCommEnabled = forexConnect.commissionsCalc!.isCommEnabled
        let cellIdentifier = (IsCommEnabled ? "OffersCellCommEnabled" : "OffersCellCommDisabled")
        let cell = self.tableView.dequeueReusableCell(withIdentifier: cellIdentifier)! as! OffersTableCell
    
        let instr = forexConnect.getInstrument(index: indexPath.row)
        let bid = forexConnect.getBid(index: indexPath.row)
        let ask = forexConnect.getAsk(index: indexPath.row)
        let bidDirection = forexConnect.getBidDirection(index: indexPath.row)
        let askDirection = forexConnect.getAskDirection(index: indexPath.row)
        let digits = forexConnect.getDigits(index: indexPath.row)
        let commOpen = forexConnect.calcOpenComm(index: indexPath.row)
        let commClose = forexConnect.calcCloseComm(index: indexPath.row)
        let commTotal = forexConnect.calcTotalComm(index: indexPath.row)
        
        cell.setDataContent(instr: instr, bid: bid, ask: ask, bidDirection: bidDirection, askDirection: askDirection, digits: digits, commOpen: commOpen, commClose: commClose, commTotal: commTotal)
        
        return cell
    }
    
    override func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let mainStoryboard = UIStoryboard(name: "Main", bundle: Bundle.main)
        let createOrderViewController = mainStoryboard.instantiateViewController(withIdentifier: "CreateOrderViewController") as! CreateOrderViewController
        let myIndexPath = self.tableView.indexPathForSelectedRow?.row
        createOrderViewController.offerIndex = myIndexPath!
        self.navigationController?.pushViewController(createOrderViewController, animated: true)
    }
    
    func offersUpdated() {
        DispatchQueue.main.async {
            self.tableView.reloadData()
        }
    }
    
    func createHeaderLabel(isCommEnabled: Bool) -> UILabel {
        let headerLabel = UILabel(frame: CGRect(x: 30.0, y: 10.0, width: 600.0, height: 25.0))
        headerLabel.textAlignment = NSTextAlignment.left
        headerLabel.textColor = UIColor.black
        headerLabel.backgroundColor = UIColor.gray
        headerLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
        
        if isCommEnabled {
            headerLabel.text = "       Symbol             Bid             Ask     ComOpen ComClose ComTotal"
            headerLabel.font = UIFont.boldSystemFont(ofSize: 11)
        } else {
            headerLabel.text = "    Symbol             Bid              Ask"
            headerLabel.font = UIFont.boldSystemFont(ofSize: 17)
        }
        return headerLabel
    }
}

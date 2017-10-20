import UIKit


class OffersViewController : UITableViewController {

    let headerLabel: UILabel = UILabel(frame: CGRect(x: 30.0, y: 10.0, width: 600.0, height: 25.0))
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        let forexConnect = ForexConnect.getSharedInstance()
    
        headerLabel.font = UIFont.boldSystemFont(ofSize: 18)
        headerLabel.textAlignment = NSTextAlignment.left
        headerLabel.textColor = UIColor.black
        headerLabel.backgroundColor = UIColor.gray
        headerLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
        headerLabel.text = "    Symbol             Bid              Ask"
        
        self.title = "Rates"
        self.tableView.register(UITableViewCell.self, forCellReuseIdentifier: "cell")
        self.tableView.dataSource = self;
        self.tableView.delegate = self;
        forexConnect.subscribeOffersUpdates(closure: offersUpdated)
    }
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return 1;
    }
    override func tableView(_ tableView: UITableView, heightForHeaderInSection section: Int) -> CGFloat {
        return 40.0
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        let forexConnect = ForexConnect.getSharedInstance()
        return forexConnect.offersCount()
    }
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = self.tableView.dequeueReusableCell(withIdentifier: "cell")! as UITableViewCell
        cell.accessoryType = .disclosureIndicator
        
        let forexConnect = ForexConnect.getSharedInstance()
        
        let instr = forexConnect.getInstrument(index: indexPath.row)
        let bid = forexConnect.getBid(index: indexPath.row)
        let ask = forexConnect.getAsk(index: indexPath.row)
        let bidDirection = forexConnect.getBidDirection(index: indexPath.row)
        let askDirection = forexConnect.getAskDirection(index: indexPath.row)
        let digits = forexConnect.getDigits(index: indexPath.row)
        
        if cell.contentView.viewWithTag(2) == nil {
           
            let bidLabel = UILabel(frame: CGRect(x: 100.0, y: 10.0, width: 80.0, height: 25.0))
            bidLabel.tag = 2
            bidLabel.textAlignment = NSTextAlignment.right
            bidLabel.font = UIFont.systemFont(ofSize: 16.0)
            bidLabel.textColor = UIColor.black
            bidLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(bidLabel)
        
            let askLabel = UILabel(frame: CGRect(x: 190.0, y: 10.0, width: 80.0, height: 25.0))
            askLabel.tag = 3
            askLabel.textAlignment = NSTextAlignment.right
            askLabel.font = UIFont.systemFont(ofSize: 16.0)
            askLabel.textColor = UIColor.black
            askLabel.autoresizingMask = UIViewAutoresizing.flexibleHeight
            cell.contentView.addSubview(askLabel)
        }
        
        let instrumentLabel = cell.textLabel!
        let bidLabel = cell.contentView.viewWithTag(2) as! UILabel
        let askLabel = cell.contentView.viewWithTag(3) as! UILabel
        
        instrumentLabel.text = instr
        bidLabel.text = bid.parseToPlaces(places: digits)
        askLabel.text = ask.parseToPlaces(places: digits)
        
        if  bidDirection > 0 {
            bidLabel.textColor = UIColor.red
        }
        else if bidDirection < 0 {
            bidLabel.textColor = UIColor.blue
        }
        else {
            bidLabel.textColor = UIColor.black
        }
        
        if  askDirection > 0 {
            askLabel.textColor = UIColor.red
        }
        else if askDirection < 0 {
            askLabel.textColor = UIColor.blue
        }
        else {
            askLabel.textColor = UIColor.black
        }
                
        return cell
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    override func tableView(_ tableView: UITableView, viewForHeaderInSection section: Int) -> UIView? {
        return headerLabel
    }

    override func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let mainStoryboard = UIStoryboard(name: "Main", bundle: Bundle.main)
        let chartViewController = mainStoryboard.instantiateViewController(withIdentifier: "ChartViewController") as! ChartViewController
        chartViewController.instrument = ForexConnect.getSharedInstance().getInstrument(index: indexPath.row)
        chartViewController.dateFrom = "11.12.2013 00:00:00"
        chartViewController.dateTo = "12.12.2014 00:00:00"
        chartViewController.timeframe = "H8"
        self.navigationController?.pushViewController(chartViewController, animated: true)
    }
    
    func offersUpdated() {
        DispatchQueue.main.async {
             self.tableView.reloadData()
        }
    }
}



































import Foundation
import UIKit

class TablesViewController : UITableViewController {
    let tableNames = ["Offers", "Orders", "Trades"]
    let tablesControllersStoryboardNames = ["OffersViewController", "OrdersViewController", "TradesViewController"]
    
    override func viewDidLoad() {
        super.viewDidLoad()
        self.title = "Tables"
    }
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return 1;
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return tableNames.count
    }
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = UITableViewCell()
        cell.accessoryType = .disclosureIndicator
        cell.textLabel?.text = tableNames[indexPath.row];
        return cell
    }
    
    override func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let appDelegate = UIApplication.shared.delegate as! AppDelegate
        if appDelegate.isLogged {
            let controllerStoryboardName = tablesControllersStoryboardNames[indexPath.row]
            let mainStoryboard = UIStoryboard(name: "Main", bundle: Bundle.main)
            let nextController = mainStoryboard.instantiateViewController(withIdentifier: controllerStoryboardName)
            self.navigationController?.pushViewController(nextController, animated: true)
        }
    }
}






import Foundation
import UIKit

class ReportViewController : UITableViewController {
    var paramNames = [String]()
    var accountId: String?
    var reportFormat: ReportFormat?
    var language: Language?
    var dateFrom: Date?
    var dateTo: Date?
    
    var forexConnect = ForexConnect.sharedInstance
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        title = "Report Settings"

        accountId = forexConnect.allAccountsId[0] // use first account
        
        dateFrom = Utils.getNowDate(offsetInMonth: -2)
        dateTo = Utils.getNowDate(offsetInMonth: 0)
        reportFormat = .HTML
        language = .enu

        paramNames.append("AccountID")
        paramNames.append("Start date")
        paramNames.append("End date")
        paramNames.append("Report format")
        paramNames.append("Report language")
        
        navigationItem.rightBarButtonItem = UIBarButtonItem(title: "Show report", style: .plain, target: self, action: #selector(onCreateReport))
    }
    
    // MARK: - tableview
    
    override func numberOfSections(in tableView: UITableView) -> Int {
        return 1
    }
    
    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return paramNames.count
    }
    
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = UITableViewCell(style: UITableViewCellStyle.value1, reuseIdentifier: "cell")
        
        if indexPath.row >= paramNames.count {
            return cell
        }
        
        cell.textLabel?.text = paramNames[indexPath.row]
        
        switch indexPath.row {
        case 0: // AccountID
            cell.detailTextLabel!.text = accountId!
            cell.accessoryType = .disclosureIndicator
            break
        case 1: // Start date
            cell.detailTextLabel!.text = Utils.getPrettyStringFromDate(date: dateFrom!)
            cell.accessoryType = .disclosureIndicator
            break
        case 2: // End date
            cell.detailTextLabel!.text = Utils.getPrettyStringFromDate(date: dateTo!)
            cell.accessoryType = .disclosureIndicator
            break
        case 3: // Report format
            cell.detailTextLabel!.text = ReportFormat.toDescription(format: reportFormat!)
            cell.accessoryType = .disclosureIndicator
            break
        case 4: // Language of report
            cell.detailTextLabel!.text = Language.toDescription(language: language!)
            cell.accessoryType = .disclosureIndicator
            break
        default:
            break
        }
        
        return cell
    }
    
    override func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        if indexPath.row >= paramNames.count {
            return
        }
        
        switch indexPath.row {
        case 0: // AccountID
            editAccountId()
            break
        case 1: // Start date
            editDate(isDateFrom: true)
            break
        case 2: // End date
            editDate(isDateFrom: false)
            break
        case 3: // Report format
            editReportFormat()
            break
        case 4: // Language of report
            editLanguage()
            break
        default:
            break
        }
    }
    
    // MARK: - Edit settings
    
    func  editAccountId()
    {
        let controller = navigationController?.storyboard?.instantiateViewController(withIdentifier: "EditAlternativesViewController") as! EditAlternativesViewController
        controller.value = accountId
        controller.alternatives = forexConnect.allAccountsId
        
        controller.completionHandler = { (value: String) in
            self.accountId = value
            self.tableView.reloadData()
        }
        navigationController?.pushViewController(controller, animated: true)
    }
    
    func  editReportFormat()
    {
        let controller = navigationController?.storyboard?.instantiateViewController(withIdentifier: "EditAlternativesViewController") as! EditAlternativesViewController
        controller.value = ReportFormat.toDescription(format: reportFormat!)
        controller.alternatives = ReportFormat.allDescriptions()
        
        controller.completionHandler = { (value: String) in
            self.reportFormat = ReportFormat.fromDescription(format: value)
            self.tableView.reloadData()
        }
        navigationController?.pushViewController(controller, animated: true)
    }
    
    func  editLanguage()
    {
        let controller = navigationController?.storyboard?.instantiateViewController(withIdentifier: "EditAlternativesViewController") as! EditAlternativesViewController
        controller.value = Language.toDescription(language: language!)
        controller.alternatives = Language.allDescriptions()
        
        controller.completionHandler = { (value: String) in
            self.language = Language.fromDescription(language: value)
            self.tableView.reloadData()
        }
        navigationController?.pushViewController(controller, animated: true)
    }
    
    func editDate(isDateFrom: Bool)
    {
        let controller = navigationController?.storyboard?.instantiateViewController(withIdentifier: "EditDateViewController") as! EditDateViewController
        controller.dateFrom = dateFrom
        controller.dateTo = dateTo
        controller.changeDateFrom = isDateFrom
        controller.completionHandler = { (dateFrom: Date, dateTo: Date) in
            self.dateFrom = dateFrom
            self.dateTo = dateTo
            self.tableView.reloadData()
        }
        navigationController?.pushViewController(controller, animated: true)
    }
    
     // MARK: - Create report
    
    @objc func onCreateReport() {
        let reportURL = forexConnect.getReportURL(accountID: accountId!, dateFrom: dateFrom!, dateTo: dateTo!, reportFormat: reportFormat!, language: language!)
        if let reportURLValue = reportURL {
            print("Report URL: \(reportURLValue)")
            UIApplication.shared.openURL(URL(string: reportURLValue)!) // open in browser
        } else {
            let alert = UIAlertView()
            alert.message = "Error: Cannot create report URL"
            alert.addButton(withTitle: "ok")
            alert.show()
        }
    }
}

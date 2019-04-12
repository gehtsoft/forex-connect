import UIKit

class EditDateViewController: UIViewController {
    
    var dateFrom: Date?
    var dateTo: Date?
    var changeDateFrom: Bool?
    @IBOutlet weak var DatePickerTitle: UILabel!
    @IBOutlet weak var datePicker: UIDatePicker!
    
    var completionHandler: ((Date, Date) -> ())?
    
    override func viewDidLoad() {
        super.viewDidLoad()
        if changeDateFrom! {
            datePicker.setDate(dateFrom!, animated: true)
            DatePickerTitle.text = "Choose an start date"
        } else {
            datePicker.setDate(dateTo!, animated: true)
            DatePickerTitle.text = "Choose an end date"
        }

        datePicker.maximumDate = Utils.getNowDate(offsetInMonth: 0)
        datePicker.minimumDate = Utils.getNowDate(offsetInMonth: -240)
    }
    
    @IBAction func onCancel(_ sender: UIButton) {
        navigationController?.popViewController(animated: true)
    }
    
    @IBAction func onSave(_ sender: UIButton) {
        var _dateTo = dateTo
        var _dateFrom = dateFrom
        
        if changeDateFrom! {
            _dateFrom = datePicker.date
        } else {
            _dateTo = datePicker.date
        }
        
        let result = _dateFrom?.compare(_dateTo!)
        if result == ComparisonResult.orderedAscending {
            dateFrom = _dateFrom
            dateTo = _dateTo
           
            navigationController?.popViewController(animated: true)
            
            if let complHendler = completionHandler {
                complHendler(dateFrom!, dateTo!)
            }
        } else {
            showError()
        }
    }
    
    func showError()
    {
        let alert = UIAlertView()
        alert.title = "Error"
        alert.message = "The 'End Date' must be greater than the 'Start Date'."
        alert.addButton(withTitle: "OK")
        alert.show()
    }
}



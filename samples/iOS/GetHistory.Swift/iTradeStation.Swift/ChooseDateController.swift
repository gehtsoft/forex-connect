import Foundation
import UIKit

// ChooseDateController allows choosing a start date, an end date and a timeframe
//
class ChooseDateController : UIViewController {
    var delegate: ChooseDateControllerDelegate?
    var dateFrom = ""
    var dateTo = ""
    var timeframe = ""
    
    private let timeframeIndex = ["m30": 0, "H8": 1, "D1": 2, "W1": 3, "M1": 4]

    @IBOutlet weak var timeframeControl: UISegmentedControl!
    @IBOutlet weak var endDatePiker: UIDatePicker!
    @IBOutlet weak var startDatePiker: UIDatePicker!
    
    override func viewDidLoad() {
        timeframeControl.selectedSegmentIndex = timeframeIndex[timeframe]!
        
        // init datepikers with dates from ChartViewController
        startDatePiker.setDate(parseDate(date: dateFrom), animated: true);
        endDatePiker.setDate(parseDate(date: dateTo), animated: true)
        
        // set maximumDate property to current date
        startDatePiker.maximumDate = Date()
        endDatePiker.maximumDate = Date()
    }
    
    @IBAction func saveButtonClicked(_ sender: UIButton) {
        if startDatePiker.date >= endDatePiker.date {
            showMessage(msg: "Start date must be less than end date")
        } else {
            dateFrom = dateToString(date: startDatePiker.date)
            dateTo = dateToString(date: endDatePiker.date)
            timeframe = timeframeControl.titleForSegment(at: timeframeControl.selectedSegmentIndex)!
            dismiss(animated: true, completion: nil)
            delegate?.onDateChaged(dateTo: dateTo, dateFrom: dateFrom, timeframe: timeframe)
        }
    }
    
    @IBAction func cancelButtonClicked(_ sender: UIButton) {
        dismiss(animated: true, completion: nil)
    }
}

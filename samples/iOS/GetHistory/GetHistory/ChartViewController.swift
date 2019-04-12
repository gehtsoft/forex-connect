import Foundation
import UIKit

// ChartViewController shows a candlestick chart
//
class ChartViewController : UIViewController, PriceHistoryProviderDelegate, ChartViewDelegate, ChooseDateControllerDelegate  {
    var instrument: String?
    var dateFrom: String?
    var dateTo: String?
    var timeframe: String?
    
    var priceHistoryProvider: PricesHistoryProvider?
    var chartHelper: CandlestickChartHelper?
    
    @IBOutlet weak var chartView: CombinedChartView!
    @IBOutlet weak var infolabel: UILabel!
    var pleaseWaitAlert: UIAlertView?

    
    override func viewDidLoad() {
        title = instrument!
        
        chartView?.delegate = self
        chartHelper = CandlestickChartHelper(view: chartView)
        
        // add 'Option' button to  controller's header
        let optionButton = UIButton(type: .custom)
        optionButton.addTarget(self, action: #selector(self.optionButtonClicked(sender:)), for: UIControlEvents.touchUpInside)
        optionButton.frame = CGRect(x: 0, y: 0, width: 70, height: 40)
        let barButton = UIBarButtonItem(customView: optionButton)
        
        let str = "Options"
        let attributedString = NSMutableAttributedString(string: str)
        let foundRange = attributedString.mutableString.range(of: "Options")
        attributedString.addAttribute(NSAttributedStringKey.foregroundColor, value: UIColor.red, range: foundRange)
        optionButton.setAttributedTitle(attributedString, for: .normal)
        self.navigationItem.setRightBarButton(barButton, animated: true)
        
        reloadHistoryData() // load historical prices
    }
    
    // show ChooseDateController
    @objc func optionButtonClicked(sender: UIButton) {
        let mainStoryboard = UIStoryboard(name: "Main", bundle: Bundle.main)
        let dateTimeController = mainStoryboard.instantiateViewController(withIdentifier: "ChooseDateController") as! ChooseDateController
        dateTimeController.dateFrom = dateFrom!
        dateTimeController.dateTo = dateTo!
        dateTimeController.timeframe = timeframe!
        dateTimeController.delegate = self
        self.navigationController?.present(dateTimeController, animated: true, completion: nil)
    }
    
    // fill the table (at bottom of the view) with the information about current timeframe,
    // start date and end date
    func fillInfo() {
        var index = dateFrom?.index((dateFrom?.startIndex)!, offsetBy: 10)
        let shortDateFrom = dateFrom?.substring(to: index!)
        index = dateTo?.index((dateTo?.startIndex)!, offsetBy: 10)
        let shortDateTo = dateTo?.substring(to: index!)
        
        infolabel.text = timeframe! + ", " + shortDateFrom! + " - " + shortDateTo!
    }
    
    // MARK: PriceHistoryProviderDelegate
    
    func onPriceHistryUpdated(historyData: [HistoryData]) {
        pleaseWaitAlert?.dismiss(withClickedButtonIndex: 0, animated: true)
        chartHelper?.setChartDataAndRedrawChart(historyData: historyData)
    }
    
    func onPriceHistryUpdateFailed(error: String) {
        pleaseWaitAlert?.dismiss(withClickedButtonIndex: 0, animated: true)
        showMessage(msg: error)
    }
    
    
    // MARK: DateTimeProviderDelegate
    
    func onDateChaged(dateTo: String, dateFrom: String, timeframe: String) {
        // if any of settings has chaged by user, reload all historical data and then draw chart again
        if dateFrom != self.dateFrom || dateTo != self.dateTo || timeframe != self.timeframe {
            self.dateFrom = dateFrom
            self.dateTo = dateTo
            self.timeframe = timeframe
            reloadHistoryData()
        }
    }
    
    func reloadHistoryData() {
        pleaseWaitAlert = UIAlertView()
        pleaseWaitAlert?.message = "Please, wait..."
        pleaseWaitAlert?.show()
        priceHistoryProvider = PricesHistoryProvider(instrument: instrument!, timeFrom: dateFrom!, timeTo: dateTo!, timeFrame: timeframe!, delegate: self)
        fillInfo()
    }
}

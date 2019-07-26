import Foundation
import UIKit

class CreateOrderViewController : UIViewController, UITextFieldDelegate {
    
    var offerIndex = 0
    var isBuy = false
    var orderType = 0
    var digits = 0
    var refreshTimer: Timer?
    
    let forexConnect = ForexConnect.sharedInstance
    
    @IBOutlet weak var instrumentTitle: UILabel!
    @IBOutlet weak var sellBuyControl: UISegmentedControl!
    @IBOutlet weak var amountField: UITextField!
    @IBOutlet weak var amountSlider: UISlider!
    @IBOutlet weak var rateField: UITextField!
    @IBOutlet weak var rateSlider: UISlider!
    @IBOutlet weak var orderTypeControl: UISegmentedControl!
    
    var baseUnitSize: Int!
    
    override func viewDidLoad() {
        
        UIApplication.shared.statusBarOrientation = .portrait
        super.viewDidLoad()
        
        let instrument = forexConnect.getInstrument(index: offerIndex)
        let session = forexConnect.session
        let loginRules = session.getLoginRules()
        let settingsProvider = loginRules!.getTradingSettingsProvider()
        baseUnitSize = Int(settingsProvider!.getBaseUnitSize(instrument, forexConnect.firstAccount))
        
        amountField.delegate = self
        rateField.delegate = self
        amountField.text = String(baseUnitSize)
        self.title = "Create order"
        instrumentTitle.text = forexConnect.getInstrument(index: offerIndex)
        digits = forexConnect.getDigits(index: offerIndex)
        rateSlider.isEnabled = false
        refreshData()
    }
    
    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
    }
    
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        refreshTimer = Timer.scheduledTimer(timeInterval: 1, target: self, selector: #selector(refreshDataIfNeed), userInfo: nil, repeats: true)
    }
    
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        refreshTimer?.invalidate()
        refreshTimer = nil
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    func textFieldShouldReturn(_ textField: UITextField) -> Bool {
        textField.resignFirstResponder()
        return true
    }
    
    @objc func refreshDataIfNeed() {
        if orderType == 0 {
            refreshData()
        }
    }
    
    func refreshData() {
        var rateFieldValue: Double
        
        if sellBuyControl.selectedSegmentIndex == 0 {
            rateFieldValue =  forexConnect.getBid(index: offerIndex)
            isBuy = false
        } else {
            rateFieldValue = forexConnect.getAsk(index: offerIndex)
            isBuy = true
        }
        
        rateField.text = rateFieldValue.parseToPlaces(places: digits)
        rateSlider.minimumValue = Float(rateFieldValue * 0.95)
        rateSlider.maximumValue = Float(rateFieldValue * 1.05)
        rateSlider.value = Float(rateFieldValue)
    }
    
    @IBAction func sellBuySwitched() {
        refreshData()
    }
    
    @IBAction func okPressed() {
        let amount = Int(amountField.text!)
        let rate = Double(rateField.text!)
        
        let order = Order()
        order.createOrder(offerIndex: offerIndex, isBuy: isBuy, amount: amount!, rate: rate!, orderType: orderType)
        order.wait()
        
        if !order.orderCreatedSuccessfully {
            showError(errorMsg: order.errorMessage)
        } else {
            self.navigationController?.popViewController(animated: true)
        }
    }
    
    @IBAction func amountSliderChenged() {
        let amountMultiplier = lround(Double(amountSlider.value))
        let amount: Int = amountMultiplier * baseUnitSize
        amountField.text = String(amount)
    }
    
    @IBAction func rateSliderChnaged() {
        rateField.text = rateSlider.value.parseToPlaces(places: digits)
    }
    
    @IBAction func cancelPressed() {
        self.navigationController?.popViewController(animated: true)
    }
    
    @IBAction func orderTypeChanged(_ sender: UISegmentedControl) {
        if sender.selectedSegmentIndex == 0 {
            rateSlider.isEnabled = false
        } else {
            rateSlider.isEnabled = true
        }
        orderType = sender.selectedSegmentIndex
        refreshData();
    }
    
    func showError(errorMsg: String?) {
        let alert = UIAlertView()
        alert.title = "Cannot create Order"
        alert.message = (errorMsg == nil) ? "Unknown error" : errorMsg
        alert.addButton(withTitle: "OK")
        alert.show()
    }
}


import Foundation
import UIKit

class CreateOrderViewController : UIViewController, UITextFieldDelegate {
    
    var offerIndex = 0
    var isBuy = false
    var orderType = 0
    var digits = 0
    let forexConnect = ForexConnect.getSharedInstance()
    
    @IBOutlet weak var instrumentTitle: UILabel!
    @IBOutlet weak var sellBuyControl: UISegmentedControl!
    @IBOutlet weak var amountField: UITextField!
    @IBOutlet weak var amountSlider: UISlider!
    @IBOutlet weak var rateField: UITextField!
    @IBOutlet weak var rateSlider: UISlider!
    @IBOutlet weak var orderTypeControl: UISegmentedControl!
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        amountSlider.minimumValue = 0.0;
        amountSlider.maximumValue = 99.0;
        amountSlider.value = 10.0;
        amountField.delegate = self;
        rateField.delegate = self;
        //print("Offer index \(offerIndex)");
        amountField.text = "100";
        self.title = "Create order";
        instrumentTitle.text = forexConnect.getInstrument(index: offerIndex)
        digits = forexConnect.getDigits(index: offerIndex)
        refreshData();
    }
    
    override func viewWillDisappear(_ animated: Bool) {
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    func textFieldShouldReturn(_ textField: UITextField) -> Bool {
        textField.resignFirstResponder()
        return true
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
    
    @IBAction func orderTypeSwitched() {
        orderType = orderTypeControl.selectedSegmentIndex
    }
    
    @IBAction func okPressed() {
        let amount = Int(amountField.text!)
        let rate = Double(rateField.text!)
        forexConnect.createOrder(offerIndex: offerIndex, isBuy: isBuy, amount: amount! * 1000, rate: rate!, orderType: orderType)
        self.navigationController?.popViewController(animated: true)
    }
    
    @IBAction func amountSliderChenged() {
        amountField.text = String(format:"%ld", 10 + lround(Double(amountSlider.value)) * 10)
    }
    
    @IBAction func rateSliderChnaged() {
        rateField.text = rateSlider.value.parseToPlaces(places: digits);
    }
    
    @IBAction func amountFieldChanged() {
        let amountFieldValue = Float(amountField.text!)! / 10;
        amountSlider.setValue(amountFieldValue, animated: true);
    }
    
    @IBAction func rateFieldChanged() {
        let rateFieldValue = Float(rateField.text!);
        rateSlider.minimumValue = rateFieldValue! * 0.95;
        rateSlider.maximumValue = rateFieldValue! * 1.05;
        rateSlider.setValue(rateFieldValue!, animated: true);
    }
    
    @IBAction func cancelPressed() {
        self.navigationController?.popViewController(animated: true)
    }
}






















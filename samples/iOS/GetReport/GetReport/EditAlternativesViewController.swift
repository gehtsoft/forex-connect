import Foundation
import UIKit

class EditAlternativesViewController : UIViewController, UIPickerViewDataSource, UIPickerViewDelegate
{
    var alternatives: [String]?
    var value: String?
    var completionHandler: ((String) -> ())?
    
    @IBOutlet weak var comboBoxView: UIPickerView!
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        comboBoxView.dataSource = self
        comboBoxView.delegate = self
    }
    
    func numberOfComponents(in pickerView: UIPickerView) -> Int {
        return 1
    }
    
    func pickerView(_ pickerView: UIPickerView, numberOfRowsInComponent component: Int) -> Int {
        if let alternativesValue = alternatives {
            return alternativesValue.count
        }
        return 0
    }
    
    
    func pickerView(_ pickerView: UIPickerView, titleForRow row: Int, forComponent component: Int) -> String? {
        if let alternativesValue = alternatives {
            return alternativesValue[row]
        }
        return nil
    }
    
    @IBAction func onCancel(_ sender: UIButton)
    {
        navigationController?.popViewController(animated: true)
    }
    
    @IBAction func onSave(_ sender: UIButton)
    {
        let selectedRowIndex = comboBoxView.selectedRow(inComponent: 0)
        value = alternatives?[selectedRowIndex]
        navigationController?.popViewController(animated: true)
        
        if let complHendler = completionHandler {
            complHendler(value!)
        }
    }
    
    
}


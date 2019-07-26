import Foundation
import UIKit

class LoginViewController : UIViewController, UITextFieldDelegate {
    
    @IBOutlet weak var connEdit: UITextField!
    @IBOutlet weak var hostEdit: UITextField!
    @IBOutlet weak var passEdit: UITextField!
    @IBOutlet weak var loginEdit: UITextField!
    @IBOutlet weak var sessionIdEdit: UITextField!
    @IBOutlet weak var activityIndicator: UIActivityIndicatorView!
    @IBOutlet weak var connectButton: UIButton!
    
    var forexConnect: ForexConnect?
    
    override func viewWillAppear(_ animated: Bool) {
        restoreUserCredentials()
        forexConnect = ForexConnect.getSharedInstance()
        
        forexConnect!.subscribeStatus(closure: onSessionStatusChanged)
        let appDelegate = UIApplication.shared.delegate as! AppDelegate
        
        if appDelegate.isLogged {
            onSessionStatusChanged(status: IO2GSession_Disconnecting)
            forexConnect!.logout()
            appDelegate.isLogged = false
        } else {
            self.title = "Login settings"
        }
    }
    
    override func viewDidLoad() {
        connEdit.delegate = self
        hostEdit.delegate = self
        passEdit.delegate = self
        loginEdit.delegate = self
        sessionIdEdit.delegate = self;
        
        loginEdit.becomeFirstResponder()
    }
    
    func onSessionStatusChanged(status: IO2GSessionStatus_O2GSessionStatus) {
        
        let mainQueue = DispatchQueue.main
        mainQueue.async {
            
            switch status {
            case IO2GSession_Disconnected:
                let appDelegate = UIApplication.shared.delegate as! AppDelegate
                appDelegate.isLogged = false
                self.connectButton.isEnabled = true
                self.activityIndicator.stopAnimating()
                self.title = "Login settings"
                self.navigationController?.popToRootViewController(animated: true)
                break
            case IO2GSession_Connected:
                self.connectButton.isEnabled = true
                self.activityIndicator.stopAnimating()
                self.title = "Login settings"
                break
            case IO2GSession_Connecting:
                self.connectButton.isEnabled = false
                self.activityIndicator.startAnimating()
                self.title = "Connecting..."
                break
            case IO2GSession_Reconnecting:
                self.connectButton.isEnabled = false
                self.activityIndicator.startAnimating()
                self.title = "Reconnecting..."
                break
            case IO2GSession_Disconnecting:
                self.connectButton.isEnabled = false
                self.activityIndicator.startAnimating()
                self.title = "Disconnecting..."
                break
            default:
                break
            }
            self.view.setNeedsDisplay()
        }
    }
    
    func textFieldShouldReturn(_ textField: UITextField) -> Bool {
        
        if textField.isEqual(loginEdit) {
            passEdit.becomeFirstResponder()
        } else if textField.isEqual(passEdit) {
            connEdit.becomeFirstResponder()
        } else if textField.isEqual(connEdit) {
            hostEdit.becomeFirstResponder()
        } else if textField.isEqual(hostEdit) {
            hostEdit.resignFirstResponder();
        } else if textField.isEqual(sessionIdEdit) {
            sessionIdEdit.resignFirstResponder();
        }
        
        return true
    }
    
    func saveUserCredentials() {
        
        let userNameValue = loginEdit.text!
        let connectionValue = connEdit.text!
        let hostValue = hostEdit.text!
        let passValue = passEdit.text!
        let sessionIdValue = sessionIdEdit.text!
        
        let defaults = UserDefaults.standard
        defaults.set(userNameValue, forKey: "userNameValue")
        defaults.set(connectionValue, forKey: "connectionValue")
        defaults.set(hostValue, forKey: "hostValue")
        defaults.set(passValue, forKey: "passValue")
        defaults.set(sessionIdValue, forKey: "sesseionIdValue")
    }
    
    func restoreUserCredentials() {
        
        let defaults = UserDefaults.standard
        let userNameValue = defaults.string(forKey: "userNameValue")
        let connectionValue = defaults.string(forKey: "connectionValue")
        let hostValue = defaults.string(forKey: "hostValue")
        let passValue = defaults.string(forKey: "passValue")
        let sessionIdValue = defaults.string(forKey: "sesseionIdValue")
        
        if connectionValue == nil {
            connEdit.text = "Demo"
            hostEdit.text = "http://fxcorporate.com"
            loginEdit.text = ""
            passEdit.text = ""
            sessionIdEdit.text = ""
        } else {
            connEdit.text = connectionValue
            hostEdit.text = hostValue
            loginEdit.text = userNameValue
            passEdit.text = passValue
            sessionIdEdit.text = sessionIdValue
        }
        
    }
    
    @IBAction func connectBtnClicked(sender: UIButton) {
        
        if (connEdit.text!.isEmpty || hostEdit.text!.isEmpty || passEdit.text!.isEmpty || loginEdit.text!.isEmpty) {
            return
        }
        
        passEdit.resignFirstResponder()
        connEdit.resignFirstResponder()
        hostEdit.resignFirstResponder()
        loginEdit.resignFirstResponder()
        sessionIdEdit.resignFirstResponder()
        
        let userNameValue = loginEdit.text!
        let passwordValue = passEdit.text!
        let connectionValue = connEdit.text!
        let hostValue = hostEdit.text!
        let sessionIdValue = sessionIdEdit.text!
        
        let host = hostValue
        var suffix = ""
        if (!host.hasSuffix("Hosts.jsp")) {
            if (host.hasSuffix("/")) {
                suffix = "Hosts.jsp"
            } else {
                suffix = "/Hosts.jsp"
            }
        }
        
        let loginJob = DispatchQueue.global(qos: .background)
        loginJob.async {
            self.forexConnect!.setLoginData(user: userNameValue, sessionId: sessionIdValue, pwd: passwordValue, url: host+suffix, connection: connectionValue)
            self.completeLoginJob();
        }
    }
    
    func completeLoginJob() {
        forexConnect!.login()
        let isOk = forexConnect!.waitForConnectionCompleted()
        
        let mainQueue = DispatchQueue.main
        mainQueue.async {
            if isOk == false {
                self.showErrorAlert()
            } else {
                let appDelegate = UIApplication.shared.delegate as! AppDelegate
                appDelegate.isLogged = true
                self.saveUserCredentials()
                self.navigationController?.pushViewController(OffersViewController(), animated: true)
            }
        }
    }
    
    func showErrorAlert()  {
        let alert = UIAlertView()
        alert.message = "Login error"
        alert.addButton(withTitle: "ok")
        alert.show()
    }
}

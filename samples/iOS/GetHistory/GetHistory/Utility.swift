import Foundation
import UIKit

extension Float {
    func parseToPlaces(places:Int) -> String {
        let formatStr = String(format: "%%.%df", places);
        return String(format: formatStr, self)
    }
}

extension Double {
    func parseToPlaces(places:Int) -> String {
        let formatStr = String(format: "%%.%df", places);
        return String(format: formatStr, self)
    }
}

// show UIAllertView with message
func showMessage(msg: String)  {
    let alert = UIAlertView()
    alert.message = msg
    alert.addButton(withTitle: "ok")
    alert.show()
}

func parseDate(date: String) -> Date {
    let dateFormatter = DateFormatter()
    dateFormatter.dateFormat = "MM.dd.yyyy hh:mm:ss"
    return dateFormatter.date(from: date)!
}

// convert Date into the string "MM.dd.yyyy hh:mm:ss"
func dateToString(date: Date) -> String {
    let dateFormatter = DateFormatter()
    dateFormatter.dateFormat = "MM.dd.yyyy hh:mm:ss"
    return dateFormatter.string(from: date)
}

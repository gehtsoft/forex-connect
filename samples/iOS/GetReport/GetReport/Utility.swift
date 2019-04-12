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

class Utils {
    static func getNowDate(offsetInMonth: Int) -> Date {
        var calendar = Calendar.current
        calendar.timeZone = TimeZone.current
        var date = calendar.startOfDay(for: Date())
        if offsetInMonth != 0 {
            date = getDateWithOffset(offsetInMonth: offsetInMonth, date: date)
        }
        return date
    }
    
    static func getDateWithOffset(offsetInMonth: Int, date: Date) -> Date {
        var offset = DateComponents()
        offset.month = offsetInMonth
        offset.timeZone = TimeZone.current
        let calendar = Calendar.current
        return calendar.date(byAdding: offset, to: date)!
    }
    
    static  func getPrettyStringFromDate(date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "MMM d, yyyy"
        formatter.timeZone = TimeZone.current
        return formatter.string(from: date)
    }
}

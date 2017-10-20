import Foundation

// HistoryData contains market data for some dates
//
class HistoryData {
    var instrumentStr: String
    var open: Double
    var close: Double
    var hight: Double
    var low: Double
    var dateStr: String
    
    init(instr: String, open: Double, close: Double, hight: Double, low: Double, date: Double) {
        self.instrumentStr = instr
        self.open = open
        self.close = close
        self.hight = hight
        self.low = low
        let systemTime = O2GDateTimeUtils2.windowsTime(fromOleTime: date)
        self.dateStr = String(format: "%hu/%hu/%hu", systemTime.wDay, systemTime.wMonth, systemTime.wYear)
    }
    
    public var debugDescription: String {
        get {
            return instrumentStr + " " + dateStr
        }
    }
}

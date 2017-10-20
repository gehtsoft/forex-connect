import Foundation

// DateTimeDelegate allows notifying ChartViewController when an archive of prices has been loaded.
//
protocol ChooseDateControllerDelegate {
    func onDateChaged(dateTo: String, dateFrom: String, timeframe: String);
}

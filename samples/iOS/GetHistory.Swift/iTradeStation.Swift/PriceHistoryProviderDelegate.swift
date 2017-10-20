import Foundation

// PricesHistoryProvider class uses a delegate to notify
// ChartViewController when prices have been loaded from a server
//
protocol PriceHistoryProviderDelegate {
    func onPriceHistryUpdated(historyData: [HistoryData]);
    func onPriceHistryUpdateFailed(error: String);
}

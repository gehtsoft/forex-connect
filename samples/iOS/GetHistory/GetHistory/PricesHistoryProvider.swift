import Foundation

// PricesHistoryProvider loads an archive of prices from a server,
// then pass it to ChartViewController
//
class PricesHistoryProvider
{
    private var lastError: String?
    private var historyDataArray: [HistoryData]
    
    private var delegate: PriceHistoryProviderDelegate?
    private var responseListener: ResponseListener
    private var session: IO2GSession
    
    init(instrument: String, timeFrom: String, timeTo: String, timeFrame: String, delegate: PriceHistoryProviderDelegate?) {

        lastError = "Unable to load historical prices"
        
        self.delegate = delegate
        
        historyDataArray = [HistoryData]()
        
        session = ForexConnect.getSharedInstance().getSession()
        responseListener = ResponseListener()
        session.subscribeResponse(responseListener)
        
        // convert time from a human readable string to a double OLE time
        let dTimeFrom = O2GDateTimeUtils2.parseOleTime(from: timeFrom)
        let dTimeTo = O2GDateTimeUtils2.parseOleTime(from: timeTo)
        
        var dFirst = dTimeTo
        
        // load historical prices asynchronously
        let asyncQueue = DispatchQueue.global(qos: .default)
        
        // get historical prices from server
        asyncQueue.async {
            repeat
            {
                let request = self.createFetchPriceHistoryRequest(session: self.session, instrument: instrument, dateFrom: dTimeFrom, dateTo: dFirst, timeFrame: timeFrame)
                let requestId = request.getID()
                self.responseListener.setRequestId(requestId: requestId!)
                self.session.send(request)
                self.responseListener.wait()
                let response = self.responseListener.getResponse()
                
                if let resp = response
                {
                    if resp.getType() != MarketDataSnapshot {  break  }
                    let readerFactory = self.session.getResponseReaderFactory()
                    if readerFactory == nil {  break  }
                    let reader = readerFactory?.createMarketDataSnapshotReader(resp)
                    
                    if reader!.size() == 0  {  break  }
    
                    if fabs(dFirst - reader!.getDate(0)) > 0.0001
                    {
                        dFirst = reader!.getDate(0)  // earliest datetime of returned data
                    }
                    else
                    {
                        break
                    }
      
                    self.fillHstoryData(reader: reader!, instrument: instrument)
                    
                }
                else
                {
                    break
                }
            } while dFirst - dTimeFrom > 0.0001
            
            self.notify()
        }
    }
    
    deinit {
        session.unsubscribeResponse(responseListener)
    }
    
    func notify() {
        let mainQueue = DispatchQueue.main

        if self.historyDataArray.count > 0 {
            if let deleg = delegate {
                mainQueue.async { deleg.onPriceHistryUpdated(historyData: self.historyDataArray) }
            }
        } else {
            if let deleg = delegate {
                mainQueue.async { deleg.onPriceHistryUpdateFailed(error: self.lastError!) }
            }
        }
    }
    
    
    // create a market data snapshot request to retrive historical dates from server
    private func createFetchPriceHistoryRequest(session: IO2GSession, instrument: String, dateFrom: Double, dateTo: Double, timeFrame: String) -> IO2GRequest {
        let reqfactory = session.getRequestFactory() as IO2GRequestFactory
        let timeFrameCollection = reqfactory.getTimeFrameCollection() as IO2GTimeframeCollection
        let timeFrame = timeFrameCollection.getByID(timeFrame) as IO2GTimeframe
        let request = reqfactory.createMarketDataSnapshotRequestInstrument(instrument, timeFrame, timeFrame.getQueryDepth()) as IO2GRequest
        reqfactory.fillMarketDataSnapshotRequestTime(request, dateFrom, dateTo, false, PreviousClose)
        return request
    }
    
    // fill array of HistoryData with historycal prices
    private func fillHstoryData(reader: IO2GMarketDataSnapshotResponseReader, instrument: String) {
        let historySize = reader.size()
        let first: Int32 = 0
        
        var earlyPartOfHistData = [HistoryData]()
        
        for var i in first..<historySize {
            
            if !reader.isBar() {
                continue;
            }
            
            let open = reader.getAskOpen(i)
            let close = reader.getAskClose(i)
            let hight = reader.getAskHigh(i)
            let low = reader.getAskLow(i)
            let date = reader.getDate(i)
            
            let histData = HistoryData(instr: instrument, open: open, close: close, hight: hight, low: low, date: date)
            earlyPartOfHistData.append(histData)
        }
        
        earlyPartOfHistData.append(contentsOf: historyDataArray)
        historyDataArray = earlyPartOfHistData
    }
}
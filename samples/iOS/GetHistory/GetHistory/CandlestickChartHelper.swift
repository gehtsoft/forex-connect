import UIKit

class CandlestickChartHelper {
    let chartView: CombinedChartView
    
    init(view: CombinedChartView) {
        self.chartView = view
    }
    
    func setChartDataAndRedrawChart(historyData: [HistoryData]) {
        self.chartView.chartDescription?.enabled = false;
        
        self.chartView.maxVisibleCount = 15;
        self.chartView.pinchZoomEnabled = false;
        self.chartView.drawGridBackgroundEnabled = false;
        
        let xAxis = self.chartView.xAxis;
        xAxis.labelPosition = XAxis.LabelPosition.bottom;
        xAxis.drawGridLinesEnabled = false;
        
        let leftAxis = self.chartView.leftAxis;
        leftAxis.labelCount = 7;
        leftAxis.drawGridLinesEnabled = false;
        leftAxis.drawAxisLineEnabled = false;
        
        let rightAxis = self.chartView.rightAxis;
        rightAxis.enabled = false;
        
        self.chartView.legend.enabled = false;
        setChartData(historyData: historyData)
    }
    
    private func setChartData(historyData: [HistoryData])  {
        var yVals1 = [CandleChartDataEntry]()
        var dates = [String]()
        
        var index = 0
        for var data in historyData {
            yVals1.append(CandleChartDataEntry(x: Double(index), shadowH: data.hight, shadowL: data.low, open: data.open, close: data.close))
            index += 1
            dates.append(data.dateStr)
        }
        
        let xAxis = self.chartView.xAxis;
        xAxis.valueFormatter = IndexAxisValueFormatter(values: dates)
        xAxis.setLabelCount(5, force: false)
        xAxis.granularity = 1
        
        let dataSet = CandleChartDataSet(values: yVals1, label: "Data Set")
        dataSet.axisDependency = YAxis.AxisDependency.left
        dataSet.setColor(UIColor(white:80/255.0, alpha:1.0))
        dataSet.drawIconsEnabled = false
        
        dataSet.shadowColor = UIColor.darkGray;
        dataSet.shadowWidth = 0.7;
        dataSet.decreasingColor = UIColor.red;
        dataSet.decreasingFilled = true;
        dataSet.increasingColor = UIColor(red:122/255.0, green:242/255.0, blue:84/255.0, alpha:1.0)
        dataSet.increasingFilled = false;
        dataSet.neutralColor = UIColor.blue;
        
        let data = CandleChartData(dataSet: dataSet)
        chartView.data = data
        
        chartView.notifyDataSetChanged()
        chartView.setNeedsDisplay()
    }
}


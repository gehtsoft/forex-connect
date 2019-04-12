package fxtsmobile.com.fxconnect.model;

import java.util.ArrayList;
import java.util.List;

public class HistoricPricesRepository {

    private HistoricPricesRepository() {}

    private static HistoricPricesRepository mInstance;

    public static HistoricPricesRepository getInstance() {
        if (mInstance == null) {
            mInstance = new HistoricPricesRepository();
        }
        return mInstance;
    }

    private List<CandleChartItem> chartData = new ArrayList<>();
    private List<VolumeItem> tableData = new ArrayList<>();

    public List<CandleChartItem> getChartData() {
        return chartData;
    }

    public void setChartData(List<CandleChartItem> chartData) {
        this.chartData = chartData;
    }

    public List<VolumeItem> getTableData() {
        return tableData;
    }

    public void setTableData(List<VolumeItem> tableData) {
        this.tableData = tableData;
    }
}

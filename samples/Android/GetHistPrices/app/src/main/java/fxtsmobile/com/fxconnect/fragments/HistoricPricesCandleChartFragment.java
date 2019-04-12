package fxtsmobile.com.fxconnect.fragments;

import android.graphics.Color;
import android.graphics.Paint;
import android.os.Bundle;
import android.support.annotation.Nullable;
import android.support.v4.app.Fragment;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.RadioButton;

import com.github.mikephil.charting.charts.CandleStickChart;
import com.github.mikephil.charting.components.AxisBase;
import com.github.mikephil.charting.components.XAxis;
import com.github.mikephil.charting.data.CandleData;
import com.github.mikephil.charting.data.CandleDataSet;
import com.github.mikephil.charting.data.CandleEntry;
import com.github.mikephil.charting.formatter.IAxisValueFormatter;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;
import java.util.Locale;

import fxtsmobile.com.fxconnect.R;
import fxtsmobile.com.fxconnect.model.BarItem;
import fxtsmobile.com.fxconnect.model.CandleChartItem;
import fxtsmobile.com.fxconnect.model.HistoricPricesRepository;

public class HistoricPricesCandleChartFragment extends Fragment {
    private static final String DATE_TIME_FORMAT = "HH:mm";
    private static final int TYPE_ASK = 0;
    private static final int TYPE_BID = 1;

    private CandleStickChart candleStickChart;
    private RadioButton askRadioButton;
    private RadioButton bidRadioButton;

    public static HistoricPricesCandleChartFragment newInstance() {
        return new HistoricPricesCandleChartFragment();
    }

    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.fragment_historic_prices_candle_chart, container, false);

        candleStickChart = view.findViewById(R.id.candleStickChart);
        askRadioButton = view.findViewById(R.id.askRadioButton);
        bidRadioButton = view.findViewById(R.id.bidRadioButton);

        setupRadioButtons();

        return view;
    }

    private void setupRadioButtons() {
        askRadioButton.setOnClickListener(radioButtonClickListener);
        bidRadioButton.setOnClickListener(radioButtonClickListener);
        askRadioButton.toggle();
        askRadioButton.callOnClick();
    }

    View.OnClickListener radioButtonClickListener = new View.OnClickListener() {
        @Override
        public void onClick(View view) {
            boolean checked = ((RadioButton) view).isChecked();

            if (!checked) {
                return;
            }

            List<CandleEntry> entries = new ArrayList<>();
            String label = "";

            switch(view.getId()) {
                case R.id.askRadioButton:
                    entries = getEntries(TYPE_ASK);
                    label = getString(R.string.historic_prices_chart_ask);
                    break;
                case R.id.bidRadioButton:
                    entries = getEntries(TYPE_BID);
                    label = getString(R.string.historic_prices_chart_bid);
                    break;
            }

            createCandleChart(entries, label);
        }
    };

    private void createCandleChart(List<CandleEntry> entries, String label) {
        if (entries.isEmpty()) {
            candleStickChart.clear();
            return;
        }

        CandleDataSet candleDataSet = getCandleDataSet(entries, label);
        CandleData candleData = new CandleData(candleDataSet);

        candleStickChart.getDescription().setEnabled(false);
        candleStickChart.setData(candleData);
        candleStickChart.invalidate();

        XAxis xAxis = candleStickChart.getXAxis();
        xAxis.setValueFormatter(axisXDateTimeValueFormatter);
    }

    private List<CandleEntry> getEntries(int type) {
        List<CandleChartItem> chartData = HistoricPricesRepository.getInstance().getChartData();
        List<CandleEntry> entries = new ArrayList<>();

        for (int i = 0; i < chartData.size(); i++) {
            CandleChartItem candleChartItem = chartData.get(i);
            BarItem bidItem = getBarItem(type, candleChartItem);
            CandleEntry candleEntry = getCandleEntry(i, bidItem);
            entries.add(candleEntry);
        }

        return entries;
    }

    private BarItem getBarItem(int type, CandleChartItem candleChartItem) {
        switch (type) {
            case TYPE_ASK:
                return candleChartItem.getAskItem();
            case TYPE_BID:
                return candleChartItem.getBidItem();
        }

        throw new IllegalArgumentException("Type not found");
    }

    private CandleEntry getCandleEntry(int position, BarItem barItem) {
        return new CandleEntry(position, (float)barItem.getHigh(), (float)barItem.getLow(), (float)barItem.getOpen(), (float)barItem.getClose());
    }

    private CandleDataSet getCandleDataSet(List<CandleEntry> entries, String label) {
        CandleDataSet candleDataSet = new CandleDataSet(entries, label);
        candleDataSet.setColor(Color.BLACK);
        candleDataSet.setShadowColor(Color.DKGRAY);
        candleDataSet.setShadowWidth(1f);
        candleDataSet.setDecreasingColor(Color.RED);
        candleDataSet.setDecreasingPaintStyle(Paint.Style.FILL);
        candleDataSet.setIncreasingColor(Color.rgb(122, 242, 84));
        candleDataSet.setIncreasingPaintStyle(Paint.Style.FILL);
        candleDataSet.setNeutralColor(Color.BLUE);
        candleDataSet.setValueTextColor(Color.RED);
        candleDataSet.setDrawValues(false);

        return candleDataSet;
    }

    private String getChartItemDate(Calendar calendar) {
        SimpleDateFormat timeFormat = new SimpleDateFormat(DATE_TIME_FORMAT, Locale.getDefault());
        return timeFormat.format(calendar.getTime());
    }

    private IAxisValueFormatter axisXDateTimeValueFormatter = new IAxisValueFormatter() {
        @Override
        public String getFormattedValue(float value, AxisBase axis) {
            int position = (int)value;
            List<CandleChartItem> chartData = HistoricPricesRepository.getInstance().getChartData();
            CandleChartItem candleChartItem = chartData.get(position);
            Calendar calendar = candleChartItem.getCalendar();
            return getChartItemDate(calendar);
        }
    };

    @Override
    public void onDestroyView() {
        candleStickChart.clear();
        super.onDestroyView();
    }
}

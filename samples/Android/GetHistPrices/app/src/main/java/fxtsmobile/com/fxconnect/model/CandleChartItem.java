package fxtsmobile.com.fxconnect.model;

import java.util.Calendar;

public class CandleChartItem {
    private Calendar calendar;
    private BarItem askItem;
    private BarItem bidItem;

    public CandleChartItem(BarItem askItem, BarItem bidItem, Calendar calendar) {
        this.askItem = askItem;
        this.bidItem = bidItem;
        this.calendar = calendar;
    }

    public BarItem getAskItem() {
        return askItem;
    }

    public BarItem getBidItem() {
        return bidItem;
    }

    public Calendar getCalendar() {
        return calendar;
    }

}

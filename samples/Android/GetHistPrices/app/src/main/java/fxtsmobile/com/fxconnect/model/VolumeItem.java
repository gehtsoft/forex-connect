package fxtsmobile.com.fxconnect.model;

import java.util.Calendar;

public class VolumeItem {
    private Calendar calendar;
    private int volume;

    public VolumeItem(Calendar calendar, int volume) {
        this.calendar = calendar;
        this.volume = volume;
    }

    public Calendar getCalendar() {
        return calendar;
    }

    public int getVolume() {
        return volume;
    }

}

package fxtsmobile.com.fxconnect.model;

public class BarItem {

    private double low;
    private double close;
    private double body;
    private double open;
    private double high;

    public BarItem(double low, double close, double body, double open, double high) {
        this.low = low;
        this.close = close;
        this.body = body;
        this.open = open;
        this.high = high;
    }

    public double getLow() {
        return low;
    }

    public double getClose() {
        return close;
    }

    public double getBody() {
        return body;
    }

    public double getOpen() {
        return open;
    }

    public double getHigh() {
        return high;
    }

}

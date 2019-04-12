/* Copyright 2019 FXCM Global Services, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use these files except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
package getliveprices.pricedata;

import java.util.Calendar;

/**
 * Implementation of the offer interface
 */
public class Offer implements IOffer {
    private String mInstrument;
    private Calendar mLastUpdate;
    private double mBid;
    private double mAsk;
    private int mMinuteVolume;
    private int mDigits;

    @Override
    public Calendar getLastUpdate() {
        return mLastUpdate;
    }

    @Override
    public double getBid() {
        return mBid;
    }

    @Override
    public void setBid(double value) {
        mBid = value;
    }

    @Override
    public double getAsk() {
        return mAsk;
    }

    @Override
    public void setAsk(double value) {
        mAsk = value;
    }

    @Override
    public int getMinuteVolume() {
        return mMinuteVolume;
    }

    @Override
    public void setMinuteVolume(int value) {
        mMinuteVolume = value;
    }

    @Override
    public int getDigits() {
        return mDigits;
    }

    @Override
    public String getInstrument() {
        return mInstrument;
    }

    @Override
    public void setInstrument(String value) {
        mInstrument = value;
    }

    @Override
    public void setLastUpdate(Calendar value) {
        mLastUpdate = value;
    }

    @Override
    public void setDigits(int value) {
        mDigits = value;
    }

    @Override
    public IOffer clone() {
        return new Offer(mInstrument, mLastUpdate, mBid, mAsk, mMinuteVolume, mDigits);
    }

    /**
     * Constructor
     * 
     * @param instrument
     *            The name of the instrument
     * @param lastUpdate
     *            The date and time of the last update in UTC timezone
     * @param bid
     *            The latest bid price
     * @param ask
     *            The latest ask price
     * @param minuteVolume
     *            The accumulated minute
     * @param digits
     *            The number of significant digits
     */
    public Offer(String instrument, Calendar lastUpdate, double bid, double ask, int minuteVolume,
        int digits) {
        mInstrument = instrument;
        mLastUpdate = lastUpdate;
        mBid = bid;
        mAsk = ask;
        mMinuteVolume = minuteVolume;
        mDigits = digits;
    }

}
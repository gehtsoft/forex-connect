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
 * Implementation of the period interface
 */
class Period implements IPeriod {
    private Calendar mTime;
    private Bar mAsk;
    private Bar mBid;
    private int mVolume;

    /**
     * Implementation of bar interface
     */
    class Bar implements IBar {
        /**
         * Prices.
         */
        private double mOpen, mHigh, mLow, mClose;

        /**
         * Constructor of a historical bar
         * 
         * @param open
         *            The open price
         * @param high
         *            The high price
         * @param low
         *            The low price
         * @param close
         *            The close price
         */
        Bar(double open, double high, double low, double close) {
            mOpen = open;
            mHigh = high;
            mLow = low;
            mClose = close;
        }

        /**
         * Constructor of a new bar
         * 
         * @param open
         *            The open price of the bar
         */
        Bar(double open) {
            mOpen = mHigh = mLow = mClose = open;
        }

        @Override
        public void setOpen(double value) {
            mOpen = value;
        }
        
        @Override
        public double getOpen() {
            return mOpen;
        }

        @Override
        public double getHigh() {
            return mHigh;
        }

        @Override
        public void setHigh(double value) {
            mHigh = value;
        }

        @Override
        public double getLow() {
            return mLow;
        }

        @Override
        public void setLow(double value) {
            mLow = value;
        }

        @Override
        public double getClose() {
            return mClose;
        }

        @Override
        public void setClose(double value) {
            mClose = value;
        }
    }

    /**
     * Gets the bid bar implementation (required to modify alive bars)
     */
    public Bar get_Ask() {
        return mAsk;
    }

    /**
     * Gets the ask bar implementation (required to modify alive bars)
     */
    public Bar get_Bid() {
        return mBid;
    }

    /**
     * Constructor of an alive period
     * 
     * @param start
     *            The date/time when the period starts
     * @param bid
     *            The first bid price of the period
     * @param ask
     *            The first ask price of the period
     * @param volume
     *            The volume
     */
    Period(Calendar start, double bid, double ask, int volume) {
        mTime = start;
        mBid = new Bar(bid);
        mAsk = new Bar(ask);
        mVolume = volume;
    }

    /**
     * Constructor of a historical period
     * 
     * @param time
     *            The date/time when the period starts
     * @param bidOpen
     *            The open bid price of the period
     * @param bidHigh
     *            The high bid price of the period
     * @param bidLow
     *            The low bid price of the period
     * @param bidClose
     *            The close bid price of the period
     * @param askOpen
     *            The open ask price of the period
     * @param askHigh
     *            The high ask price of the period
     * @param askLow
     *            The low ask price of the period
     * @param askClose
     *            The close ask price of the period
     * @param volume
     *            The minute volume of the period
     */
    Period(Calendar time, double bidOpen, double bidHigh, double bidLow, double bidClose,
        double askOpen, double askHigh, double askLow, double askClose, int volume) {
        mTime = time;
        mBid = new Bar(bidOpen, bidHigh, bidLow, bidClose);
        mAsk = new Bar(askOpen, askHigh, askLow, askClose);
        mVolume = volume;
    }

    @Override
    public Calendar getDate() {
        return mTime;
    }

    @Override
    public IBar getAsk() {
        return mAsk;
    }

    @Override
    public IBar getBid() {
        return mBid;
    }

    @Override
    public int getVolume() {
        return mVolume;
    }

    @Override
    public void setVolume(int value) {
        mVolume = value;
    }
}
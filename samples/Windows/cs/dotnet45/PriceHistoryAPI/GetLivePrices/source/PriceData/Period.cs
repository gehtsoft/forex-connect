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
using System;
using System.Collections.Generic;
using System.Text;

namespace GetLivePrices
{
    /// <summary>
    /// Implementation of the period interface
    /// </summary>
    class Period : IPeriod
    {
        /// <summary>
        /// Implementation of bar interface
        /// </summary>
        internal class Bar : IBar
        {
            /// <summary>
            /// Prices.
            /// </summary>
            private double mOpen, mHigh, mLow, mClose;

            #region IBar members

            public double Open
            {
                get
                {
                    return mOpen;
                }
            }

            public double High
            {
                get
                {
                    return mHigh;
                }
                set
                {
                    mHigh = value;
                }
            }

            public double Low
            {
                get
                {
                    return mLow;
                }
                set
                {
                    mLow = value;
                }
            }

            public double Close
            {
                get
                {
                    return mClose;
                }
                set
                {
                    mClose = value;
                }
            }

            #endregion

            /// <summary>
            /// Constructor of a historical bar
            /// </summary>
            /// <param name="open">The open price</param>
            /// <param name="high">The high price</param>
            /// <param name="low">The low price</param>
            /// <param name="close">The close price</param>
            internal Bar(double open, double high, double low, double close)
            {
                mOpen = open;
                mHigh = high;
                mLow = low;
                mClose = close;
            }

            /// <summary>
            /// Constructor of a new bar
            /// </summary>
            /// <param name="open">The open price of the bar</param>
            internal Bar(double open)
            {
                mOpen = mHigh = mLow = mClose = open;
            }
        }

        #region IPeriod Members

        public DateTime Time
        {
            get
            {
                return mTime;
            }
        }
        private DateTime mTime;
        
        public IBar Ask
        {
            get
            {
                return mAsk;
            }
        }
        private Bar mAsk;

        public IBar Bid
        {
            get
            {
                return mBid;
            }
        }
        private Bar mBid;

        public int Volume
        {
            get
            {
                return mVolume;
            }
            set
            {
                mVolume = value;
            }
        }
        int mVolume;

        #endregion

        /// <summary>
        /// Gets the bid bar implementation (required to modify alive bars)
        /// </summary>
        internal Bar _Ask
        {
            get
            {
                return mAsk;
            }
        }

        /// <summary>
        /// Gets the ask bar implementation (required to modify alive bars)
        /// </summary>
        internal Bar _Bid
        {
            get
            {
                return mBid;
            }
        }

        /// <summary>
        /// Constructor of an alive period
        /// </summary>
        /// <param name="time">The date/time when the period starts</param>
        /// <param name="bid">The first bid price of the period</param>
        /// <param name="ask">The first ask price of the  period</param>
        /// <param name="volume">The volume</param>
        internal Period(DateTime time, double bid, double ask, int volume)
        {
            mTime = time;
            mBid = new Bar(bid);
            mAsk = new Bar(ask);
            mVolume = volume;
        }

        /// <summary>
        /// Constructor of a historical period
        /// </summary>
        /// <param name="time">The date/time when the period starts</param>
        /// <param name="bidOpen">The open bid price of the period</param>
        /// <param name="bidHigh">The high bid price of the period</param>
        /// <param name="bidLow">The low bid price of the period</param>
        /// <param name="bidClose">The close bid price of the period</param>
        /// <param name="askOpen">The open ask price of the period</param>
        /// <param name="askHigh">The high ask price of the period</param>
        /// <param name="askLow">The low ask price of the period</param>
        /// <param name="askClose">The close ask price of the period</param>
        /// <param name="volume">The minute volume of the period</param>
        internal Period(DateTime time, double bidOpen, double bidHigh, double bidLow, double bidClose,
                                       double askOpen, double askHigh, double askLow, double askClose,
                                       int volume)
        {
            mTime = time;
            mBid = new Bar(bidOpen, bidHigh, bidLow, bidClose);
            mAsk = new Bar(askOpen, askHigh, askLow, askClose);
            mVolume = volume;
        }
    }
}

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
    /// Implementation of the offer interface
    /// </summary>
    class Offer : IOffer
    {
        #region IOffers members

        public string Instrument
        {
            get 
            {
                return mInstrument;
            }
        }
        private string mInstrument;

        public DateTime LastUpdate
        {
            get 
            {
                return mLastUpdate;
            }
            set
            {
                mLastUpdate = value;
            }
        }
        private DateTime mLastUpdate;

        public double Bid
        {
            get 
            {
                return mBid;
            }
            set
            {
                mBid = value;
            }
        }
        private double mBid;

        public double Ask
        {
            get 
            {
                return mAsk;
            }
            set
            {
                mAsk = value;
            }
        }
        private double mAsk;

        public int MinuteVolume
        {
            get 
            {
                return mMinuteVolume;
            }
            set
            {
                mMinuteVolume = value;
            }
        }
        private int mMinuteVolume;

        public int Digits
        {
            get
            {
                return mDigits;
            }
        }
        private int mDigits;

        public IOffer Clone()
        {
            return new Offer(mInstrument, mLastUpdate, mBid, mAsk, mMinuteVolume, mDigits);
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instrument">The name of the instrument</param>
        /// <param name="lastUpdate">The date and time of the last update in UTC timezone</param>
        /// <param name="bid">The latest bid price</param>
        /// <param name="ask">The lastest ask price</param>
        /// <param name="minuteVolume">The accumulated minute</param>
        /// <param name="digits">The number of significant digits</param>
        internal Offer(string instrument, DateTime lastUpdate, double bid, double ask, int minuteVolume, int digits)
        {
            mInstrument = instrument;
            mLastUpdate = lastUpdate;
            mBid = bid;
            mAsk = ask;
            mMinuteVolume = minuteVolume;
            mDigits = digits;
        }
    }
}

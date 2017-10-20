using System;
using System.Collections.Generic;
using System.Text;

namespace GetOffers
{
    class Offer
    {
        public DateTime Date
        {
            get
            {
                return mDate;
            }
            set
            {
                mDate = value;
            }
        }
        private DateTime mDate;

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

        public string OfferID
        {
            get
            {
                return mOfferID;
            }
            set
            {
                mOfferID = value;
            }
        }
        private string mOfferID;

        public string Instrument
        {
            get
            {
                return mInstrument;
            }
            set
            {
                mInstrument = value;
            }
        }
        private string mInstrument;

        public int Precision
        {
            get
            {
                return mPrecision;
            }
            set
            {
                mPrecision = value;
            }
        }
        private int mPrecision;

        public double PipSize
        {
            get
            {
                return mPipSize;
            }
            set
            {
                mPipSize = value;
            }
        }
        private double mPipSize;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="sOfferID"></param>
        /// <param name="sInstrument"></param>
        /// <param name="iPrecision"></param>
        /// <param name="dPipSize"></param>
        /// <param name="date"></param>
        /// <param name="dBid"></param>
        /// <param name="dAsk"></param>
        public Offer(string sOfferID, string sInstrument, int iPrecision, double dPipSize, DateTime date, double dBid, double dAsk)
        {
            mOfferID = sOfferID;
            mInstrument = sInstrument;
            mPrecision = iPrecision;
            mPipSize = dPipSize;
            mDate = date;
            mBid = dBid;
            mAsk = dAsk;
        }
    }

    class OfferCollection : IEnumerable<Offer>
    {
        private List<Offer> mOffers;
        private Dictionary<string, Offer> mIDsAndOffers;
        private Object mSyncObj;

        /// <summary>
        /// ctor
        /// </summary>
        public OfferCollection()
        {
            mOffers = new List<Offer>();
            mIDsAndOffers = new Dictionary<string,Offer>();
            mSyncObj = new Object();
        }

        /* Add offer to collection */
        public void AddOffer(Offer offer)
        {
            lock (mSyncObj)
            {
                if (!mIDsAndOffers.ContainsKey(offer.OfferID))
                {
                    mIDsAndOffers.Add(offer.OfferID, offer);
                }
                else
                {
                    mIDsAndOffers[offer.OfferID] = offer;
                }
                mOffers.Clear();
                mOffers.AddRange(mIDsAndOffers.Values);
            }
        }

        /* Find offer by id */
        public bool FindOffer(string sOfferID, out Offer offer)
        {
            bool result = false;
            lock (mSyncObj)
            {
                offer = null;
                if (mIDsAndOffers.ContainsKey(sOfferID))
                {
                    offer = mIDsAndOffers[sOfferID];
                    result = true;
                }
            }
            return result;
        }

        /* Get offer by index */
        public Offer GetOffer(int index)
        {
            Offer offer = null;
            lock (mSyncObj)
            {
                if (index >= 0 && index < mOffers.Count)
                    offer = mOffers[index];
            }
            return offer;
        }

        /* Get number of offers */
        public int Size()
        {
            int size = 0;
            lock (mSyncObj)
            {
                size = mOffers.Count;
            }
            return size;
        }

        /* Clear collection */
        public void Clear()
        {
            lock (mSyncObj)
            {
                mOffers.Clear();
                mIDsAndOffers.Clear();
            }
        }

        #region IEnumerable<Offer> Members

        public IEnumerator<Offer> GetEnumerator()
        {
            return mOffers.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mOffers.GetEnumerator();
        }

        #endregion
    }
}

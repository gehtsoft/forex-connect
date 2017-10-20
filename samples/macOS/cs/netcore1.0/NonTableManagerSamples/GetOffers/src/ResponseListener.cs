using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace GetOffers
{
    class ResponseListener : IO2GResponseListener
    {
        private O2GSession mSession;
        private string mRequestID;
        private string mInstrument;
        private O2GResponse mResponse;
        private EventWaitHandle mSyncResponseEvent;
        private OfferCollection mOffers;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public ResponseListener(O2GSession session)
        {
            mSession = session;
            mRequestID = string.Empty;
            mInstrument = string.Empty;
            mResponse = null;
            mSyncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            mOffers = new OfferCollection();
        }

        public void SetRequestID(string sRequestID)
        {
            mResponse = null;
            mRequestID = sRequestID;
        }

        public void SetInstrument(string sInstrument)
        {
            mInstrument = sInstrument;
        }

        public bool WaitEvents()
        {
            return mSyncResponseEvent.WaitOne(30000);
        }

        public O2GResponse GetResponse()
        {
            return mResponse;
        }

        #region IO2GResponseListener Members

        public void onRequestCompleted(string sRequestId, O2GResponse response)
        {
            if (mRequestID.Equals(response.RequestID))
            {
                mResponse = response;
                mSyncResponseEvent.Set();
            }
        }

        public void onRequestFailed(string sRequestID, string sError)
        {
            if (mRequestID.Equals(sRequestID))
            {
                Console.WriteLine("Request failed: " + sError);
                mSyncResponseEvent.Set();
            }
        }

        public void onTablesUpdates(O2GResponse response)
        {
            if (response.Type == O2GResponseType.TablesUpdates)
            {
                PrintOffers(mSession, response, mInstrument);
            }
        }

        #endregion

        /// <summary>
        /// Store offers data from response and print it
        /// </summary>
        /// <param name="session"></param>
        /// <param name="response"></param>
        /// <param name="sInstrument"></param>
        public void PrintOffers(O2GSession session, O2GResponse response, string sInstrument)
        {
            O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
            if (readerFactory == null)
            {
                throw new Exception("Cannot create response reader factory");
            }
            O2GOffersTableResponseReader responseReader = readerFactory.createOffersTableReader(response);
            for (int i = 0; i < responseReader.Count; i++)
            {
                O2GOfferRow offerRow = responseReader.getRow(i);
                Offer offer;
                if (mOffers.FindOffer(offerRow.OfferID, out offer))
                {
                    if (offerRow.isTimeValid && offerRow.isBidValid && offerRow.isAskValid)
                    {
                        offer.Date = offerRow.Time;
                        offer.Bid = offerRow.Bid;
                        offer.Ask = offerRow.Ask;
                    }
                }
                else
                {
                    offer = new Offer(offerRow.OfferID, offerRow.Instrument,
                                     offerRow.Digits, offerRow.PointSize, offerRow.Time,
                                     offerRow.Bid, offerRow.Ask);
                    mOffers.AddOffer(offer);
                }
                if (string.IsNullOrEmpty(sInstrument) || offerRow.Instrument.Equals(sInstrument))
                {
                    Console.WriteLine("{0}, {1}, Bid={2}, Ask={3}", offer.OfferID, offer.Instrument, offer.Bid, offer.Ask);
                }
            }
        }

    }
}


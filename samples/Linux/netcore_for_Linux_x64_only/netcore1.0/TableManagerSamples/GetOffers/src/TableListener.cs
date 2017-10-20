using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using fxcore2;

namespace GetOffers
{
    class TableListener : IO2GTableListener
    {
        private string mInstrument;
        private OfferCollection mOffers;

        /// <summary>
        /// ctor
        /// </summary>
        public TableListener()
        {
            mOffers = new OfferCollection();
        }

        public void SetInstrument(string sInstrument)
        {
            mInstrument = sInstrument;
        }

        #region IO2GTableListener Members

        // Implementation of IO2GTableListener interface public method onAdded
        public void onAdded(string sRowID, O2GRow rowData)
        {
        }

        // Implementation of IO2GTableListener interface public method onChanged
        public void onChanged(string sRowID, O2GRow rowData)
        {
            if (rowData.TableType == O2GTableType.Offers)
            {
                PrintOffer((O2GOfferTableRow)rowData, mInstrument);
            }
        }

        // Implementation of IO2GTableListener interface public method onDeleted
        public void onDeleted(string sRowID, O2GRow rowData)
        {
        }

        public void onStatusChanged(O2GTableStatus status)
        {
        }

        #endregion

        public void SubscribeEvents(O2GTableManager manager)
        {
            O2GOffersTable offersTable = (O2GOffersTable)manager.getTable(O2GTableType.Offers);
            offersTable.subscribeUpdate(O2GTableUpdateType.Update, this);
        }

        public void UnsubscribeEvents(O2GTableManager manager)
        {
            O2GOffersTable offersTable = (O2GOffersTable)manager.getTable(O2GTableType.Offers);
            offersTable.unsubscribeUpdate(O2GTableUpdateType.Update, this);
        }

        public void PrintOffers(O2GOffersTable offers, string sInstrument)
        {
            O2GOfferTableRow offerRow = null;
            O2GTableIterator iterator = new O2GTableIterator();
            while (offers.getNextRow(iterator, out offerRow))
            {
                PrintOffer(offerRow, sInstrument);
            }
        }

        public void PrintOffer(O2GOfferTableRow offerRow, string sInstrument)
        {
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

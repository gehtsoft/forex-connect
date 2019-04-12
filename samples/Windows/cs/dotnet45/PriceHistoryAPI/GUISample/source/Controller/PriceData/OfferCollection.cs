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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GUISample
{
    /// <summary>
    /// Implementation of the offers collection
    /// </summary>
    class OfferCollection : IOfferCollection
    {
        #region Data fields

        /// <summary>
        /// Storage for the offers
        /// </summary>
        private List<Offer> mOffers = new List<Offer>();

        /// <summary>
        /// The dictionary to search the offer by the offer id
        /// </summary>
        private Dictionary<string, Offer> mIdToOffer = new Dictionary<string, Offer>();

        /// <summary>
        /// The dictionary to search the offer by the instrument name
        /// </summary>
        private Dictionary<string, Offer> mInstrumentToOffer = new Dictionary<string, Offer>();

        #endregion

        #region IOfferCollection members

        public int Count
        {
            get
            {
                return mOffers.Count;
            }
        }

        public IOffer this[int index]
        {
            get
            {
                return mOffers[index];
            }
        }

        public Offer this[string key]
        {
            get
            {
                Offer offer = null;

                if (mInstrumentToOffer.TryGetValue(key, out offer))
                    return offer;
                if (mIdToOffer.TryGetValue(key, out offer))
                    return offer;
                return null;
            }
        }

        public IEnumerator<IOffer> GetEnumerator()
        {
            return new EnumeratorHelper<Offer, IOffer>(mOffers.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mOffers.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Adds a new offer
        /// </summary>
        /// <param name="offerid">OfferId</param>
        /// <param name="instrument">The name of the instrument</param>
        /// <param name="lastChange">The date/time of the last change of the offer</param>
        /// <param name="bid">The current bid price</param>
        /// <param name="ask">The current ask price</param>
        /// <param name="minuteVolume">The current accumulated minute tick volume</param>
        /// <param name="digits">The precision</param>
        public void Add(string offerid, string instrument, DateTime lastChange, double bid, double ask, int minuteVolume, int digits)
        {
            Offer offer;
            mOffers.Add(offer = new Offer(instrument, lastChange, bid, ask, minuteVolume, digits));
            mIdToOffer[offerid] = offer;
            mInstrumentToOffer[instrument] = offer;
        }

        /// <summary>
        /// Removes all offers from the collection
        /// </summary>
        public void Clear()
        {
            mOffers.Clear();
            mIdToOffer.Clear();
            mInstrumentToOffer.Clear();
        }
    }
}

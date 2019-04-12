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

namespace GetLivePrices
{
    /// <summary>
    /// The observer for live prices. It listens for periods collection updates
    /// and prints them.
    /// </summary>
    class PeriodCollectionUpdateObserver
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="periods">The period collection</param>
        public PeriodCollectionUpdateObserver(PeriodCollection periods)
        {
            mPeriods = periods;

            if (mPeriods.IsAlive)
                mPeriods.OnCollectionUpdate += IPeriodCollection_Updated;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~PeriodCollectionUpdateObserver()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Unsubscribes the object from the tick updates.
        /// </summary>
        public void Unsubscribe()
        {
            if (mPeriods.IsAlive)
                mPeriods.OnCollectionUpdate -= IPeriodCollection_Updated;
        }

        /// <summary>
        /// Price collection event: When a period in the collection is changed
        /// </summary>
        /// <param name="collection">The period collection</param>
        /// <param name="index">The index of element of the collection</param>
        void IPeriodCollection_Updated(IPeriodCollection collection, int index)
        {
            IPeriod period = collection[index];
            Console.WriteLine("Price updated: DateTime={0}, BidOpen={1}, BidHigh={2}, BidLow={3}, BidClose={4}, AskOpen={5}, AskHigh={6}, AskLow={7}, AskClose={8}, Volume={9}",
                    period.Time, period.Bid.Open, period.Bid.High, period.Bid.Low, period.Bid.Close,
                    period.Ask.Open, period.Ask.High, period.Ask.Low, period.Ask.Close, period.Volume);
        }

        /// <summary>
        /// The periods collection
        /// </summary>
        private PeriodCollection mPeriods;
    }
}

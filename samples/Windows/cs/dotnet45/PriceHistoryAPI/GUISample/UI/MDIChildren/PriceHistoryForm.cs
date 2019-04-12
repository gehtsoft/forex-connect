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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GUISample
{
    /// <summary>
    /// The form displaying a collection of the historical prices
    /// </summary>
    public partial class PriceHistoryForm : Form
    {
        IPeriodCollection mCollection;
        string mNumberFormat;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="collection">The collection of the prices to be displayed</param>
        /// <param name="controller">The Price API controller</param>
        internal PriceHistoryForm(IPeriodCollection collection, PriceAPIController controller)
        {
            InitializeComponent();
            mCollection = collection;

            // find the offer for the collection instrument and 
            // make the proper numeric format to display the prices
            // of the collection.
            foreach (IOffer offer in controller.Offers)
                if (offer.Instrument == collection.Instrument)
                {
                    mNumberFormat = "0." + new string('0', offer.Digits);
                }

            if (mNumberFormat == null)
                mNumberFormat = "0.00";

            // add items to the list view (for the real application virtual list view
            // will be more effective solution)
            for (int i = 0; i < collection.Count; i++)
                AddItem(i);

            // set the window title to the collection parameters
            this.Text = string.Format("History:{0}[{1}]{2}", collection.Instrument, collection.Timeframe, collection.IsAlive ? " (live)" : "");

            // and subscribe for the collection update event if the collection is subscribed
            // for the price updates, so it will be changed every time when a new tick is received
            if (collection.IsAlive)
                mCollection.OnCollectionUpdate += this.IPeriodCollection_Updated;
        }

        /// <summary>
        /// Formats one collection item and adds it into the listview
        /// </summary>
        /// <param name="index"></param>
        private void AddItem(int index)
        {
            ListViewItem item = listViewHistory.Items.Add("");
            for (int i = 0; i < listViewHistory.Columns.Count - 1; i++)
                item.SubItems.Add("");
            fillItem(index, item);
        }

        /// <summary>
        /// Fills the item with the data
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void fillItem(int index, ListViewItem item)
        {
            if (index >= 0 && index < mCollection.Count)
            {
                IPeriod period = mCollection[index];
                item.Text = period.Time.ToString("MM/dd/yyyy HH:mm");
                item.SubItems[1].Text = period.Bid.Open.ToString(mNumberFormat);
                item.SubItems[2].Text = period.Bid.High.ToString(mNumberFormat);
                item.SubItems[3].Text = period.Bid.Low.ToString(mNumberFormat);
                item.SubItems[4].Text = period.Bid.Close.ToString(mNumberFormat);
                item.SubItems[5].Text = period.Ask.Open.ToString(mNumberFormat);
                item.SubItems[6].Text = period.Ask.High.ToString(mNumberFormat);
                item.SubItems[7].Text = period.Ask.Low.ToString(mNumberFormat);
                item.SubItems[8].Text = period.Ask.Close.ToString(mNumberFormat);
                item.SubItems[9].Text = period.Volume.ToString();
            }
        }

        /// <summary>
        /// Price collection event: When a period in the collection is changed
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        void IPeriodCollection_Updated(IPeriodCollection collection, int index)
        {
            // The event may be sent asynchronously
            if (this.InvokeRequired)
                this.Invoke(new IPeriodCollection_Updated(IPeriodCollection_Updated), null, index);
            else
            {
                // update corrsponding list view item
                while (listViewHistory.Items.Count < mCollection.Count)
                    AddItem(listViewHistory.Items.Count);
                fillItem(index, listViewHistory.Items[index]);
            }
        }

        private void PriceHistoryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mCollection.IsAlive)
                mCollection.OnCollectionUpdate -= this.IPeriodCollection_Updated;
        }
    }
}

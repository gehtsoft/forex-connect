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
    /// The form to display the current price of the offers
    /// </summary>
    public partial class OffersForm : Form
    {
        PriceAPIController mController;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="controller">Price API controller</param>
        internal OffersForm(PriceAPIController controller)
        {
            InitializeComponent();
            mController = controller;
            //subscribe for the offer update event of the controller
            mController.OnPriceUpdate += PriceAPIController_PriceUpdate;
            listViewOffers.VirtualListSize = mController.Offers.Count;
        }

        /// <summary>
        /// Price controller event listener: An offer is updated
        /// </summary>
        /// <param name="offer"></param>
        private void PriceAPIController_PriceUpdate(IOffer offer)
        {
            //the event may be called from another thread
            if (InvokeRequired)
                Invoke(new PriceAPIController_PriceUpdate(PriceAPIController_PriceUpdate), offer);
            else
            {
                //find the offer changed and force listview to be redrawn for that item
                for (int i = 0; i < mController.Offers.Count; i++)
                    if (offer.Instrument == mController.Offers[i].Instrument)
                    {
                        listViewOffers.RedrawItems(i, i, false);
                        break;
                    }
            }
        }

        /// <summary>
        /// The cache of the numeric format. The key is the number of significant digits after 
        /// the decimal point. 
        /// </summary>
        Dictionary<int, string> mFormats = new Dictionary<int, string>();

        /// <summary>
        /// ListView Event: Retreive an item of a virtual box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewOffers_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            IOfferCollection offers = mController.Offers;
            int index = e.ItemIndex;
            string priceFormat;

            if (index >= 0 && index < offers.Count)
            {
                IOffer offer = offers[index];
                ListViewItem item = new ListViewItem();
                if (!mFormats.TryGetValue(offer.Digits, out priceFormat))
                {
                    priceFormat = "0." + new string('0', offer.Digits);
                    mFormats[offer.Digits] = priceFormat;
                }
                
                item.Text = offer.Instrument;
                item.SubItems.Add(offer.LastUpdate.ToString("MM/dd/yyyy HH:mm:ss"));
                item.SubItems.Add(offer.Bid.ToString(priceFormat));
                item.SubItems.Add(offer.Ask.ToString(priceFormat));
                e.Item = item;
            }
        }

        private void OffersForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mController.OnPriceUpdate -= PriceAPIController_PriceUpdate;
        }
    }
}

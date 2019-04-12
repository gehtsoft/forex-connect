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
    /// The form to choose Quotes Manager data to be removed
    /// </summary>
    public partial class RemoveQuotesForm : Form
    {
        /// <summary>
        /// The collection of all data
        /// </summary>
        IQMDataCollection mOrgCollection;
        /// <summary>
        /// The data chosen to be removed
        /// </summary>
        List<IQMData> mToRemove = new List<IQMData>();

        /// <summary>
        /// Gets the list of the data chosen to be removed
        /// </summary>
        public List<IQMData> ToRemove
        {
            get
            {
                return mToRemove;
            }
        }

        public RemoveQuotesForm(IQMDataCollection collection)
        {
            InitializeComponent();
            mOrgCollection = collection;
            // fills the list view with all the data stored in the cache.
            foreach (IQMData data in collection)
            {
                ListViewItem item = listViewData.Items.Add(data.Instrument);
                item.SubItems.Add(data.Timeframe);
                item.SubItems.Add(data.Year.ToString());
                item.SubItems.Add(data.Size.ToString());
            }
        }


        private void RemoveQuotesForm_Load(object sender, EventArgs e)
        {
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            // store the data checked in the list view (i.e. chosen to be removed)
            // into a new collection.
            int idx = 0;
            foreach (ListViewItem item in listViewData.Items)
            {
                if (item.Checked)
                    mToRemove.Add(mOrgCollection[idx]);
                idx++;
            }
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

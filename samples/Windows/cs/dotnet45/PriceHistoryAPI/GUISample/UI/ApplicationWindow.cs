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
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace GUISample
{
    public partial class ApplicationWindow : Form
    {
        #region Data fields, initialization and de-initalization

        /// <summary>
        /// The ForexConnect configuration
        /// </summary>
        ForexConnectConfig mForexConnectConfig;
        /// <summary>
        /// The price history communicator configuration
        /// </summary>
        PriceHistoryConfig mPriceHistoryConfig;
        /// <summary>
        /// The Price API controller (is used to get offers and offers updates and to retreive historical data)
        /// </summary>
        PriceAPIController mController = new PriceAPIController();
        /// <summary>
        /// The Quotes Manager controller (is used to move the data from the cache)
        /// </summary>
        RemoveQuotesController mRemoveController = new RemoveQuotesController();
        /// <summary>
        /// The flag used to detect the moment when offer window must be shown
        /// </summary>
        private bool mPrevReadyState = false;

        /// <summary>
        /// Construtor
        /// </summary>
        public ApplicationWindow()
        {
            InitializeComponent();

            mForexConnectConfig = ForexConnectConfigFactory.Create();
            mPriceHistoryConfig = PriceHistoryConfigFactory.Create();

            mController.OnErrorEvent += this.PriceAPIController_Error;
            mController.OnCollectionLoaded += this.PriceAPIController_CollectionLoaded;
            mController.OnStateChange += this.PriceAPIController_StateChange;
            
            mRemoveController.OnErrorEvent += this.PriceAPIController_Error;
            mRemoveController.OnListPreparedEvent += this.RemoveQuotesController_Prepared;
            mRemoveController.OnQuotesRemovedEvent += this.RemoveQuotesController_Removed;
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ApplicationWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            mController.OnErrorEvent -= this.PriceAPIController_Error;
            mController.OnCollectionLoaded -= this.PriceAPIController_CollectionLoaded;
            mController.OnStateChange -= this.PriceAPIController_StateChange;

            mRemoveController.OnErrorEvent -= this.PriceAPIController_Error;
            mRemoveController.OnListPreparedEvent -= this.RemoveQuotesController_Prepared;
            mRemoveController.OnQuotesRemovedEvent -= this.RemoveQuotesController_Removed;

            mController.release();

            // save settings
            ForexConnectConfigFactory.Save(mForexConnectConfig);
            PriceHistoryConfigFactory.Save(mPriceHistoryConfig);
            Properties.Settings.Default.Save();
        }
        #endregion

        #region MDI commands

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }
    
        #endregion

        #region Login/Logout

        private void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoginForm form = new LoginForm(mForexConnectConfig, mPriceHistoryConfig);
            if (mController.IsReady)
                mController.release();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                string cache = Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName, form.Cache);
                mController.initialize(form.User, form.Password, form.Url, form.Connection, cache);
            }
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mController.IsReady)
                mController.release();

        }

        private void PriceAPIController_StateChange(bool isReady)
        {
            if (this.InvokeRequired)
                this.Invoke(new PriceAPIController_StateChange(PriceAPIController_StateChange), isReady);
            else
            {
                mRemoveController.QuotesManager = mController.QuotesManager;
                toolStripStatusReady.Text = mController.IsReady ? "ready" : "not ready";
                if (!mPrevReadyState && mController.IsReady)
                {
                    OffersForm offers = new OffersForm(mController);
                    offers.MdiParent = this;
                    offers.Show();
                }
                else if (mPrevReadyState && !mController.IsReady)
                {
                    foreach (Form form in MdiChildren)
                        form.Close();
                }
                mPrevReadyState = mController.IsReady;
            }
        }

        #endregion

        #region Error handling

        /// <summary>
        /// Price API and Quotes Manager error handler
        /// </summary>
        /// <param name="error"></param>
        private void PriceAPIController_Error(string error)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new PriceAPIController_Error(PriceAPIController_Error), error);
            else
                MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Get history command

        private void getHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Get history parameters
            RequestHistoryForm request = new RequestHistoryForm(mController);
            if (request.ShowDialog(this) == DialogResult.OK)
            {
                // and request the history from the controller
                if (request.IsUpToNow)
                    mController.RequestAndSubscribeHistory(request.Instrument, request.Timeframe, request.From);
                else
                    mController.RequestHistory(request.Instrument, request.Timeframe, request.From, request.To);
                // ...the history as it is received will be sent to PriceAPIController_CollectionLoaded
            }
        }

        /// <summary>
        /// PriceHisitoryAPI controller event: A history is received from the server
        /// </summary>
        /// <param name="collection"></param>
        private void PriceAPIController_CollectionLoaded(IPeriodCollection collection)
        {
            // The event will be sent from another thread
            if (this.InvokeRequired)
                this.BeginInvoke(new PriceAPIController_CollectionLoaded(PriceAPIController_CollectionLoaded), collection);
            else
            {
                // Create a from which displays the collection
                PriceHistoryForm form = new PriceHistoryForm(collection, mController);
                form.MdiParent = this;
                form.Show();
            }
        }

        #endregion

        #region Remove Quotes from cache Command

        /// <summary>
        /// The command handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeQuotesFromCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mRemoveController.GetListOfData();
            // ...The result will be passed to RemoveQuotesController_Prepared event
        }

        /// <summary>
        /// RemoveQuotes controller event: Cache content description is prepared
        /// </summary>
        /// <param name="collection"></param>
        private void RemoveQuotesController_Prepared(IQMDataCollection collection)
        {
            // In some cases the event might be received from the other thread
            if (this.InvokeRequired)
                this.BeginInvoke(new RemoveQuotesController_Prepared(RemoveQuotesController_Prepared), collection);
            else
            {
                // Show the form to choose the data to remove
                RemoveQuotesForm form = new RemoveQuotesForm(collection);
                if (form.ShowDialog(this) == DialogResult.OK && form.ToRemove.Count > 0)
                    // and if any data is chosen - remove then
                    mRemoveController.RemoveData(form.ToRemove);
                    // ...the result will be passed to RemoveQuotesController_Removed event
            }
        }

        /// <summary>
        /// Remove quotes controller event: when remove command is completed
        /// </summary>
        private void RemoveQuotesController_Removed()
        {
            // Most probably the event will be sent from another thread.
            if (this.InvokeRequired)
                this.BeginInvoke(new RemoveQuotesController_Removed(RemoveQuotesController_Removed));
            else
            {
                // Just notify that the operation is completed.
                MessageBox.Show(this, "Quotes Removed");
            }
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Event: When the File menu is being opened
        /// 
        /// Update commands state depending on the status of controllers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileMenu_DropDownOpening(object sender, EventArgs e)
        {
            if (mController.IsReady)
            {
                loginToolStripMenuItem.Enabled = false;
                logoutToolStripMenuItem.Enabled = true;
                getHistoryToolStripMenuItem.Enabled = true;
            }
            else
            {
                loginToolStripMenuItem.Enabled = true;
                logoutToolStripMenuItem.Enabled = false;
                getHistoryToolStripMenuItem.Enabled = false;
            }

            removeQuotesFromCacheToolStripMenuItem.Enabled = mRemoveController.CanDo;
        }

        #endregion
    }
}

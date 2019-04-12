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
    /// The dialog from to request the login parameters and price history API cache location
    /// </summary>
    public partial class LoginForm : Form
    {
        /// <summary>
        /// The user name
        /// </summary>
        public string User
        {
            get
            {
                return textBoxUserName.Text;
            }
        }

        /// <summary>
        /// The password
        /// </summary>
        public string Password
        {
            get
            {
                return textBoxPassword.Text;
            }
        }

        /// <summary>
        /// The connection URL (Host.jsp MUST be specified)
        /// </summary>
        public string Url
        {
            get
            {
                return textBoxUrl.Text;
            }
        }

        /// <summary>
        /// The connection name (e.g. Demo or Real)
        /// </summary>
        public string Connection
        {
            get
            {
                return textBoxConnection.Text;
            }
        }

        /// <summary>
        /// The path for cache location.
        /// </summary>
        public string Cache
        {
            get
            {
                return textBoxCache.Text;
            }
        }

        public LoginForm(ForexConnectConfig forexConnectConfig, PriceHistoryConfig priceHistoryConfig)
        {
            InitializeComponent();

            mForexConnectConfig = forexConnectConfig;
            mPriceHistoryConfig = priceHistoryConfig;

            textBoxCache.Text = mPriceHistoryConfig.CachePath;
            textBoxUrl.Text = mForexConnectConfig.Url;
            textBoxUserName.Text = mForexConnectConfig.UserName;
            textBoxConnection.Text = mForexConnectConfig.Connection;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            mPriceHistoryConfig.CachePath = textBoxCache.Text;
            mForexConnectConfig.Url = textBoxUrl.Text;
            mForexConnectConfig.UserName = textBoxUserName.Text;
            mForexConnectConfig.Connection = textBoxConnection.Text;
        }

        private ForexConnectConfig mForexConnectConfig;
        private PriceHistoryConfig mPriceHistoryConfig;
    }
}

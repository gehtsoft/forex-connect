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
using System.Text;

namespace GUISample
{
    /// <summary>
    /// ForexConnect Parameters
    /// </summary>
    public class ForexConnectConfig
    {
        /// <summary>
        /// The user name for the trading connection
        /// </summary>
        public string UserName
        {
            get
            {
                return mUserName;
            }
            set
            {
                mUserName = value;
            }
        }
        private string mUserName;

        /// <summary>
        /// The connection URL for the trading connection
        /// </summary>
        public string Url
        {
            get
            {
                return mUrl;
            }
            set
            {
                mUrl = Url;
            }
        }
        private string mUrl;

        /// <summary>
        /// The connection name for the trading connection
        /// </summary>
        public string Connection
        {
            get
            {
                return mConnection;
            }
            set 
            {
                mConnection = value;
            }
        }
        private string mConnection;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="url">The connection URL</param>
        /// <param name="connection">The connection name</param>
        internal ForexConnectConfig(string userName, string url, string connection)
        {
            mUserName = userName;
            mUrl = url;
            mConnection = connection;
        }
    }
}

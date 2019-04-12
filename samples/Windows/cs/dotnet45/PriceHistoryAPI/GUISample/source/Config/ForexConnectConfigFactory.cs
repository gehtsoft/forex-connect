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
    /// Factory of the Forex Connect configuration
    /// Uses application's Settings to persist parameters
    /// </summary>
    sealed class ForexConnectConfigFactory 
    {
        /// <summary>
        /// Creates the configuration
        /// </summary>
        /// <returns>The configuration</returns>
        public static ForexConnectConfig Create()
        {
            return new ForexConnectConfig(
                Properties.Settings.Default.ForexConnectConfig_UserName,
                Properties.Settings.Default.ForexConnectConfig_Url, 
                Properties.Settings.Default.ForexConnectConfig_Connection);
        }

        /// <summary>
        /// Saves the configuration
        /// </summary>
        /// <param name="config">The configuration</param>
        public static void Save(ForexConnectConfig config)
        {
            Properties.Settings.Default.ForexConnectConfig_UserName = config.UserName;
            Properties.Settings.Default.ForexConnectConfig_Url = config.Url;
            Properties.Settings.Default.ForexConnectConfig_Connection = config.Connection;
        }
    }
}

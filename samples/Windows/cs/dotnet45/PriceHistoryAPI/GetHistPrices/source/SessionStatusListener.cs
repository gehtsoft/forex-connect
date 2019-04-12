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
using System.Threading;

using fxcore2;

namespace GetHistPrices
{
    /// <summary>
    /// Listener of the ForexConnect session status.
    /// </summary>
    class SessionStatusListener : IO2GSessionStatus
    {
        /// <summary>
        /// The ForexConnect trading session ID.
        /// </summary>
        private string mSessionID;
        /// <summary>
        /// The ForexConnect trading session PIN.
        /// </summary>
        private string mPin;
        /// <summary>
        /// The ForexConnect trading session login status flag.
        /// </summary>
        private bool mConnected;
        /// <summary>
        /// The ForexConnect trading session login status flag.
        /// </summary>
        private bool mDisconnected;
        /// <summary>
        /// The ForexConnect trading session error status flag.
        /// </summary>
        private bool mError;
        /// <summary>
        /// The handle of the ForexConnect trading session.
        /// </summary>
        private O2GSession mSession;
        /// <summary>
        /// The event of the ForexConnect trading session.
        /// </summary>
        private EventWaitHandle mSyncSessionEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sSessionID"></param>
        /// <param name="sPin"></param>
        public SessionStatusListener(O2GSession session, string sSessionID, string sPin)
        {
            mSession = session;
            mSessionID = sSessionID;
            mPin = sPin;
            Reset();
            mSyncSessionEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        /// <summary>
        /// Gets connection status.
        /// </summary>
        public bool Connected
        {
            get
            {
                return mConnected;
            }
        }

        /// <summary>
        /// Gets connection status.
        /// </summary>
        public bool Disconnected
        {
            get
            {
                return mDisconnected;
            }
        }

        /// <summary>
        /// Resets connection status.
        /// </summary>
        public void Reset()
        {
            mConnected = false;
            mDisconnected = false;
        }

        /// <summary>
        /// Waits for a session event.
        /// </summary>
        public bool WaitEvents()
        {
            return mSyncSessionEvent.WaitOne(30000);
        }

        /// <summary>
        /// Listener: When Trading session status is changed.
        /// </summary>
        /// <param name="status"></param>
        public void onSessionStatusChanged(O2GSessionStatusCode status)
        {
            Console.WriteLine("Status: " + status.ToString());
            switch (status)
            {
                case O2GSessionStatusCode.TradingSessionRequested:
                    if (string.IsNullOrEmpty(mSessionID))
                    {
                        Console.WriteLine("Argument for trading session ID is missing");
                    }
                    else
                    {
                        mSession.setTradingSession(mSessionID, mPin);
                    }
                    break;
                case O2GSessionStatusCode.Connected:
                    mConnected = true;
                    mDisconnected = false;
                    mSyncSessionEvent.Set();
                    break;
                case O2GSessionStatusCode.Disconnected:
                    mConnected = false;
                    mDisconnected = true;
                    mSyncSessionEvent.Set();
                    break;
            }
        }

        /// <summary>
        /// Listener: When Trading session login failed.
        /// </summary>
        /// <param name="error"></param>
        public void onLoginFailed(string error)
        {
            Console.WriteLine("Login error: " + error);
            mSyncSessionEvent.Set();
        }
    }
}

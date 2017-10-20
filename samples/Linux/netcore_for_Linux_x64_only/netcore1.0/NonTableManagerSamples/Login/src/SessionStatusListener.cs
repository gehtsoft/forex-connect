using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace Login
{
    class SessionStatusListener : IO2GSessionStatus
    {
        private string mSessionID;
        private string mPin;
        private bool mConnected;
        private bool mDisconnected;
        private bool mError;
        private O2GSession mSession;
        private EventWaitHandle mSyncSessionEvent;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public SessionStatusListener(O2GSession session, string sSessionID, string sPin)
        {
            mSession = session;
            mSessionID = sSessionID;
            mPin = sPin;
            Reset();
            mSyncSessionEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public bool Connected
        {
            get
            {
                return mConnected;
            }
        }

        public bool Disconnected
        {
            get
            {
                return mDisconnected;
            }
        }

        public bool Error
        {
            get
            {
                return mError;
            }
        }

        public void Reset()
        {
            mConnected = false;
            mDisconnected = false;
            mError = false;
        }

        public bool WaitEvents()
        {
            return mSyncSessionEvent.WaitOne(30000);
        }

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

        public void onLoginFailed(string error)
        {
            Console.WriteLine("Login error: " + error);
            mError = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;
using ArgParser;

namespace ShowCommissionDetails
{
    class SessionStatusListener : IO2GSessionStatus
    {
        private bool mConnected;
        private bool mDisconnected;
        private bool mError;
        private O2GSession mSession;
        private EventWaitHandle mSyncSessionEvent;
        private LoginParams mLoginParams;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="session"></param>
        public SessionStatusListener(O2GSession session, LoginParams loginParams)
        {
            mSession = session;
            mLoginParams = loginParams;
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
                    O2GSessionDescriptorCollection descs = mSession.getTradingSessionDescriptors();
                    Console.WriteLine("Session descriptors:");
                    Console.WriteLine("id, name, description, requires pin");
                    foreach (O2GSessionDescriptor desc in descs)
                    {
                        Console.WriteLine("'{0}' '{1}' '{2}' {3}", desc.Id, desc.Name, desc.Description, desc.RequiresPin);
                    }
                    Console.WriteLine();

                    string sSessionID;
                    if (string.IsNullOrEmpty(mLoginParams.SessionID))
                    {
                        Console.WriteLine("Please enter trading session ID and press \'Enter\'");
                        sSessionID = Console.ReadLine();
                    }
                    else
                    {
                        sSessionID = mLoginParams.SessionID;
                    }

                    mSession.setTradingSession(sSessionID.Trim(), mLoginParams.Pin);
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace PrintTableListener
{
    class TableManagerListener : IO2GTableManagerListener
    {
        private EventWaitHandle mEventWaitHandler;
        O2GTableManagerStatus mLastStatus;
        private bool mLoaded;
        private bool mError;

        public bool IsLoaded
        {
            get { return mLoaded; }
        }

        public bool HasError
        {
            get { return mError; }
        }

        // ctor
        public TableManagerListener()
        {
            Reset();
            mEventWaitHandler = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public void Reset()
        {
            mLastStatus = O2GTableManagerStatus.TablesLoading;
            mLoaded = false;
            mError = false;
        }

        public bool WaitEvents()
        {
            if (mLastStatus == O2GTableManagerStatus.TablesLoading)
                return mEventWaitHandler.WaitOne(10000);
            return true;
        }

        public void onStatusChanged(O2GTableManagerStatus status, O2GTableManager manager)
        {
            mLastStatus = status;
            switch (status)
            {
                case O2GTableManagerStatus.TablesLoaded:
                    mLoaded = true;
                    mError = false;
                    mEventWaitHandler.Set();
                    break;
                case O2GTableManagerStatus.TablesLoadFailed:
                    mLoaded = false;
                    mError = true;
                    mEventWaitHandler.Set();
                    break;
            }
        }
    }
}

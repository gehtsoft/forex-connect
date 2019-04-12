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

using Candleworks.QuotesMgr;

namespace GUISample
{
    delegate void RemoveQuotesController_Error(string error);
    delegate void RemoveQuotesController_Prepared(IQMDataCollection collection);
    delegate void RemoveQuotesController_Removed();

    /// <summary>
    /// The controller for Quotes Manager operator.
    /// We use it in our example only to remove quotes from the cache.
    /// </summary>
    class RemoveQuotesController : IUpdateInstrumentsCallback, IRemoveQuotesCallback
    {
        #region Data fields

        private QuotesManager mMgr = null;
        private bool mRunning = false;
        private int mRemoved;
        private int mToRemove;

        #endregion

        #region Controller interface

        /// <summary>
        /// Sets the quote manager instance
        /// </summary>
        public QuotesManager QuotesManager
        {
            set
            {
                mMgr = value;
            }
        }

        /// <summary>
        /// Checks whether Quotes Manager operation can be performed.
        /// It can be performed if the Quotes Manager is set and there is no running operation right now.
        /// </summary>
        public bool CanDo
        {
            get
            {
                return mMgr != null && !mRunning;
            }
        }

        public event RemoveQuotesController_Error OnErrorEvent;
        public event RemoveQuotesController_Prepared OnListPreparedEvent;
        public event RemoveQuotesController_Removed OnQuotesRemovedEvent;

        /// <summary>
        /// Gets the list of the data stored in the cache.
        /// The result will be sent to OnListPreparedEvent
        /// </summary>
        public void GetListOfData()
        {
            if (mMgr.areInstrumentsUpdated())
            {
                PrepareListOfInstruments();
            }
            else
            {
                mRunning = true;
                UpdateInstrumentsTask task = mMgr.createUpdateInstrumentsTask(this);
                mMgr.executeTask(task);
            }
        }
        
        /// <summary>
        /// Prepares the collection of the instruments stored in the Quotes Manager and sends it
        /// to the application via OnListPreparedEvent 
        /// </summary>
        private void PrepareListOfInstruments()
        {
            QMDataCollection collection = new QMDataCollection();
            BaseTimeframes timeframes = mMgr.getBaseTimeframes();
            Instruments instruments = mMgr.getInstruments();

            for (int i = 0; i < instruments.size(); i++)
            for (int j = 0; j < timeframes.size(); j++)
            {
                Instrument instrument = instruments.get(i);
                int y1, y2;
                y1 = instrument.getOldestQuoteDate(timeframes.get(j)).Year;
                y2 = instrument.getLatestQuoteDate(timeframes.get(j)).Year;
                if (y2 >= y1)
                {
                    for (int y = y1; y <= y2; y++)
                    {
                        //for (int j = 0; j < timeframes.size(); j++)
                        {
                            long size = mMgr.getDataSize(instrument.getName(), timeframes.get(j), y);
                            if (size > 0)
                                collection.Add(new QMData(instrument.getName(), timeframes.get(j), y, size));
                        }
                    }
                }
            }

            if (OnListPreparedEvent != null)
                OnListPreparedEvent(collection);
        }

        /// <summary>
        /// Removes the data from cache.
        /// </summary>
        /// <param name="list"></param>
        public void RemoveData(IEnumerable<IQMData> list)
        {
            List<Task> tasks = new List<Task>();
            foreach (IQMData data in list)
            {
                RemoveQuotesTask task = mMgr.createRemoveQuotesTask(data.Instrument, data.Timeframe, this);
                task.addYear(data.Year);
                tasks.Add(task);
            }

            if (tasks.Count > 0)
            {
                mRunning = true;
                mRemoved = 0;
                mToRemove = tasks.Count;
                foreach (Task task in tasks)
                    mMgr.executeTask(task);
            }

        }

        #endregion

        #region IUpdateInstrumentsCallback members

        public void onTaskCanceled(UpdateInstrumentsTask task)
        {
            mRunning = false;
        }

        public void onTaskCompleted(UpdateInstrumentsTask task)
        {
            mRunning = false;
            PrepareListOfInstruments();
        }

        public void onTaskFailed(UpdateInstrumentsTask task, QuotesManagerError error)
        {
            mRunning = false;
            if (OnErrorEvent != null)
                OnErrorEvent(String.Format("{0}({1}) : {2}", error.Code, error.SubCode, error.Message));
        }

        #endregion

        #region IRemoveQuotesCallback members

        public void onTaskCanceled(RemoveQuotesTask task)
        {
            mRemoved++;
            mRunning = mRemoved < mToRemove;

            if (!mRunning && OnQuotesRemovedEvent != null)
                OnQuotesRemovedEvent();
        }

        public void onTaskCompleted(RemoveQuotesTask task)
        {
            mRemoved++;
            mRunning = mRemoved < mToRemove;
            if (!mRunning && OnQuotesRemovedEvent != null)
                OnQuotesRemovedEvent();
        }

        public void update(RemoveQuotesTask task, QuotesManagerError error)
        {
            if (error != null && OnErrorEvent != null)
                OnErrorEvent(String.Format("{0}({1}) : {2}", error.Code, error.SubCode, error.Message));
        }

        #endregion
    }
}

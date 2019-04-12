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
using Candleworks.QuotesMgr;

namespace RemoveQuotes
{
    /// <summary>
    /// The listener for QuotesManager instruments update task.
    /// </summary>
    class UpdateInstrumentsListener : IUpdateInstrumentsCallback
    {
        /// <summary>
        /// The event of the QuotesManager.
        /// </summary>
        private EventWaitHandle mEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateInstrumentsListener()
        {
            mEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }
        
        /// <summary>
        /// Waits for a QuotesManaget event.
        /// </summary>
        public void WaitEvents()
        {
            mEvent.WaitOne(30000);
        }
        
        #region IUpdateInstrumentsCallback members

        /// <summary>
        /// Listener: when the instruments update task is cancelled.
        /// </summary>
        /// <param name="task"></param>
        public void onTaskCanceled(UpdateInstrumentsTask task)
        {
            Console.WriteLine("Update instruments task was cancelled.");
            mEvent.Set();
        }

        /// <summary>
        /// Listener: when the remove task is completed.
        /// </summary>
        /// <param name="task"></param>
        public void onTaskCompleted(UpdateInstrumentsTask task)
        {
            Console.WriteLine("Update instruments task was completed.");
            mEvent.Set();
        }

        /// <summary>
        /// Listener: when the remove task is failed.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="error"></param>
        public void onTaskFailed(UpdateInstrumentsTask task, QuotesManagerError error)
        {
            if (error != null)
            {
                string errorMessage = String.Format("{0}({1}) : {2}", error.Code, error.SubCode, error.Message);
                Console.WriteLine("Error occurred : {0}", errorMessage);
                mEvent.Set();
            }
        }
        
        #endregion
    }

}

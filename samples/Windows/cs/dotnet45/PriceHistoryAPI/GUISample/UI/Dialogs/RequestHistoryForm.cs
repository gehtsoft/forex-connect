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

using fxcore2;
using Candleworks.QuotesMgr;

namespace GUISample
{
    /// <summary>
    /// The dialog to request the price history parameters
    /// </summary>
    public partial class RequestHistoryForm : Form
    {
        /// <summary>
        /// Gets the name of the instrument
        /// </summary>
        public string Instrument
        {
            get
            {
                return comboBoxInstrument.SelectedItem.ToString();
            }
        }

        /// <summary>
        /// Get the name of the timeframe
        /// </summary>
        public O2GTimeframe Timeframe
        {
            get
            {
                return (comboBoxTimeframe.SelectedItem as TimeframeItem).Timeframe;
            }
        }

        /// <summary>
        /// Gets the date/time from (the oldest data to be loaded)
        /// </summary>
        public DateTime From
        {
            get
            {
                return dateTimePickerFrom.Value;
            }
        }

        /// <summary>
        /// Gets the flag indicating that the user chosen to load the data up to the current moment
        /// and have it subscribed for all updates
        /// </summary>
        public bool IsUpToNow
        {
            get
            {
                return dateTimePickerTo.Checked ? false : true;
            }
        }

        /// <summary>
        /// Gets the date/time to load the data to (is meaningful only is IsUpToNow == false)
        /// </summary>
        public DateTime To
        {
            get
            {
                return dateTimePickerTo.Value;
            }
        }

        /// <summary>
        /// The class is used to fill timeframe list
        /// </summary>
        class TimeframeItem
        {
            /// <summary>
            /// The name of the time frame
            /// </summary>
            private O2GTimeframe mTimeframe;

            /// <summary>
            /// Length of the candle
            /// </summary>
            private TimeSpan mLength;

            CandlePeriod.Unit mTimeframeUnit = CandlePeriod.Unit.Minute;
            int mTimeframeLength = 1;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="timeframe">ForexConnect timeframe descriptor</param>
            internal TimeframeItem(O2GTimeframe timeframe)
            {
                mTimeframe = timeframe;
                DateTime start = DateTime.Now.AddDays(-1), end = DateTime.Now;

                // parse the timeframe ID to get Quotes Manager timeframe descriptor
                if (!CandlePeriod.parsePeriod(timeframe.ID, ref mTimeframeUnit, ref mTimeframeLength))
                    throw new ArgumentException("Invalide timeframe", "timeframe");

                // get a candle in that timeframe to get it length
                CandlePeriod.getCandle(DateTime.Now, ref start, ref end, mTimeframeUnit, mTimeframeLength, 0, 0);
                mLength = end.Subtract(start);
            }

            public override string ToString()
            {
                return mTimeframe.ID;
            }

            /// <summary>
            /// Gets the length of a timeframe candle
            /// </summary>
            internal TimeSpan Length
            {
                get
                {
                    return mLength;
                }
            }

            /// <summary>
            /// Gets the default date/time to fill From box of the form.
            /// It is 300 candles back from now
            /// </summary>
            internal DateTime From
            {
                get
                {
                    return this.To.AddMinutes(-mLength.TotalMinutes * 300);
                }
            }

            /// <summary>
            /// Gets the default date/time to fill To box of the the form.
            /// It is date/time when the most recent candle of that timeframe started
            /// </summary>
            internal DateTime To
            {
                get
                {
                    DateTime s, e;
                    s = e = DateTime.Now;
                    CandlePeriod.getCandle(DateTime.UtcNow, ref s, ref e, mTimeframeUnit, mTimeframeLength, 0, 0);
                    return s;
                }
            }

            /// <summary>
            /// Gets O2G timeframe descriptor
            /// </summary>
            internal O2GTimeframe Timeframe
            {
                get
                {
                    return mTimeframe;
                }
            }
        }
    
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="controller">PriceHistory API controller</param>
        internal RequestHistoryForm(PriceAPIController controller)
        {
            InitializeComponent();

            // Fills the list of the offers available
            for (int i = 0; i < controller.Offers.Count; i++)
                comboBoxInstrument.Items.Add(controller.Offers[i].Instrument);
            if (comboBoxInstrument.Items.Count > 0)
                comboBoxInstrument.SelectedIndex = 0;

            // Fills the list of the timeframes available except tick timeframes (as
            // ticks are not supported by PriceHistory API)
            O2GTimeframeCollection timeframes = controller.Timeframes;
            for (int i = 0; i < timeframes.Count; i++)
            {
                O2GTimeframe tf = timeframes[i];
                if (tf.Unit == O2GTimeframeUnit.Tick)
                    continue;
                TimeframeItem item = new TimeframeItem(tf);
                comboBoxTimeframe.Items.Add(item);
            }

            if (comboBoxTimeframe.Items.Count > 0)
                comboBoxTimeframe.SelectedIndex = 0;

            // Initialize the range for date/time controls
            dateTimePickerFrom.MinDate = new DateTime(1980, 1, 1);
            dateTimePickerFrom.MaxDate = DateTime.Now;
            dateTimePickerTo.MinDate = new DateTime(1980, 1, 1);
        }

        /// <summary>
        /// Event: When the user chosen another timeframe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxTimeframe_SelectedIndexChanged(object sender, EventArgs e)
        {
            // update date/time in From and To boxes
            TimeframeItem item = comboBoxTimeframe.SelectedItem as TimeframeItem;
            if (item != null)
            {
                dateTimePickerFrom.Value = item.From;
                if (dateTimePickerTo.Checked)
                    dateTimePickerTo.Value = item.To;
            }
        }
    }
}

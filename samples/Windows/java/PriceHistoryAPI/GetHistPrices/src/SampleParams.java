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
package common;

import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.TimeZone;
import java.util.SimpleTimeZone;

import com.candleworks.quotesmgr.OpenPriceCandlesMode;

public class SampleParams {
    public static final String INSTRUMENT_NOT_SPECIFIED = "'Instrument' is not specified (/i|-i|/instrument|--instrument)";
    public static final String TIMEFRAME_NOT_SPECIFIED = "'Timeframe' is not specified (/timeframe|--timeframe)";

    // Getters

    public String getInstrument() {
        return mInstrument;
    }

    private String mInstrument;

    public String getTimeframe() {
        return mTimeframe;
    }
    private String mTimeframe;

    public Calendar getDateFrom() {
        return mDateFrom;
    }

    private Calendar mDateFrom;

    public Calendar getDateTo() {
        return mDateTo;
    }

    private Calendar mDateTo;

    public int getQuotesCount() {
        return mQuotesCount;
    }

    private int mQuotesCount;

    public OpenPriceCandlesMode getOpenPriceCandlesMode() {
        return mOpenPriceCandlesMode;
    }

    private OpenPriceCandlesMode mOpenPriceCandlesMode;

    // Setters

    public void setDateFrom(Calendar dtFrom) {
        mDateFrom = dtFrom;
    }

    public void setDateTo(Calendar dtTo) {
        mDateTo = dtTo;
    }

    // ctor
    public SampleParams(String[] args) {
        // Get parameters with short keys
        mInstrument = getArgument(args, "i");

        // If parameters with short keys are not specified, get parameters with long keys
        if (mInstrument.isEmpty()) {
            mInstrument = getArgument(args, "instrument");
        }

        String sDateFrom = "";
        String sDateTo = "";

        // Get parameters with long keys

        mTimeframe = getArgument(args, "timeframe");
        sDateFrom = getArgument(args, "datefrom");
        mDateFrom = Calendar.getInstance(TimeZone.getTimeZone("UTC"));
        sDateTo = getArgument(args, "dateto");
        mDateTo = Calendar.getInstance(TimeZone.getTimeZone("UTC"));

        String sQuotesCount = getArgument(args, "count");
        mQuotesCount = -1;
        try {
            mQuotesCount = Integer.parseInt(sQuotesCount);
            if (mQuotesCount <= 0) {
                mQuotesCount = -1;
            }
        } catch (NumberFormatException nfe) {
            mQuotesCount = -1;
        }

        String sOpenPriceCandlesMode = getArgument(args, "openpricecandlesmode");
        mOpenPriceCandlesMode = sOpenPriceCandlesMode.equals("firsttick") ?
            OpenPriceCandlesMode.OpenPriceFirstTick : OpenPriceCandlesMode.OpenPricePrevClose;

        // Convert types

        DateFormat df = new SimpleDateFormat("MM.dd.yyyy HH:mm:ss");
        df.setTimeZone(new SimpleTimeZone(SimpleTimeZone.UTC_TIME, "UTC"));
        try {
            mDateFrom.setTime(df.parse(sDateFrom));
        } catch (Exception ex) {
            mDateFrom = null;
        }
        try {
            mDateTo.setTime(df.parse(sDateTo));
        } catch (Exception ex) {
            mDateTo = null;
        }
    }

    private String getArgument(String[] args, String sKey) {
        for (int i = 0; i < args.length; i++) {
            int iDelimOffset = 0;
            if (args[i].startsWith("--")) {
                iDelimOffset = 2;
            } else if (args[i].startsWith("-") || args[i].startsWith("/")) {
                iDelimOffset = 1;
            }

            if (args[i].substring(iDelimOffset).equals(sKey) && (args.length > i + 1)) {
                return args[i + 1];
            }
        }
        return "";
    }
}

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

public class SampleParams {
    public static final String INSTRUMENT_NOT_SPECIFIED = "'Instrument' is not specified (/i|-i|/instrument|--instrument)";
    public static final String YEAR_NOT_SPECIFIED = "'Year' is not specified (/year|--year)";

    // Getters

    public String getInstrument() {
        return mInstrument;
    }

    private String mInstrument;

    public int getYear() {
        return mYear;
    }

    private int mYear;

    // ctor
    public SampleParams(String[] args) {
        // Get parameters with short keys
        mInstrument = getArgument(args, "i");

        // If parameters with short keys are not specified, get parameters with long keys
        if (mInstrument.isEmpty()) {
            mInstrument = getArgument(args, "instrument");
        }

        String sYear = getArgument(args, "y");
        if (sYear.isEmpty()) {
            sYear = getArgument(args, "year");
        }

        try {
            mYear = Integer.parseInt(sYear);
            if (mYear <= 0) {
                mYear = 0;
            }
        } catch (NumberFormatException nfe) {
            mYear = -1;
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

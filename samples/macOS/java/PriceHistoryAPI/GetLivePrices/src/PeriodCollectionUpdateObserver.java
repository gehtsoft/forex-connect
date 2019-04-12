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
package getliveprices;

import java.text.SimpleDateFormat;
import java.util.TimeZone;

import getliveprices.pricedata.IPeriod;
import getliveprices.pricedata.IPeriodCollection;

/**
 * The observer for live prices.
 * It listens for periods collection updates.
 */
public class PeriodCollectionUpdateObserver implements IPeriodCollection.IListener {

    private static SimpleDateFormat mDateFormat = new SimpleDateFormat( "MM.dd.yyyy HH:mm:ss");
    static {
        mDateFormat.setTimeZone(TimeZone.getTimeZone("UTC"));
    }

    /**
     * The periods collection
     */
    private IPeriodCollection mPeriods;

    /**
     * Constructor.
     * 
     * @param periods
     *            The period collection
     */
    public PeriodCollectionUpdateObserver(IPeriodCollection periods) {
        mPeriods = periods;

        if (mPeriods.isAlive()) {
            mPeriods.addListener(this);
        }
    }

    /**
     * Unsubscribes the object from the tick updates.
     */
    public void unsubscribe() {
        if (mPeriods.isAlive())  {
            mPeriods.removeListener(this);
        }
    }

    @Override
    public void onCollectionUpdate(IPeriodCollection collection, int index) {
        IPeriod period = mPeriods.get(index);

        String msg = String
            .format(
                "Price updated: DateTime=%s, BidOpen=%s, BidHigh=%s, BidLow=%s, BidClose=%s, AskOpen=%s, AskHigh=%s, AskLow=%s, AskClose=%s, Volume=%s",
                mDateFormat.format(period.getDate().getTime()), period.getBid().getOpen(), period
                    .getBid().getHigh(), period.getBid().getLow(), period.getBid().getClose(),
                period.getAsk().getOpen(), period.getAsk().getHigh(), period.getAsk().getLow(),
                period.getAsk().getClose(), period.getVolume());

        System.out.println(msg);
    }
}

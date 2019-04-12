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
package getliveprices.pricedata;

import java.util.Calendar;

/**
 * The interface for the price information in the time period
 */
public interface IPeriod {

    /**
     * Gets date/time when the period starts (In UTC time zone)
     */
    Calendar getDate();

    /**
     * Gets ask bar
     */
    IBar getAsk();

    /**
     * Gets bid bar
     */
    IBar getBid();

    /**
     * Gets tick volume
     */
    int getVolume();

    /**
     * Sets tick volume
     */
    void setVolume(int value);
}

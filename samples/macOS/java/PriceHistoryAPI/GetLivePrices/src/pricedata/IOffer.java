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
 * Interface to the offer
 */
public interface IOffer {

    /**
     * Gets the instrument name
     */
    String getInstrument();

    /**
     * Sets the instrument name
     */
    void setInstrument(String value);

    /**
     * Gets the date/time (in UTC time zone) when the offer was updated the last
     * time
     */
    Calendar getLastUpdate();

    /**
     * Sets the date/time (in UTC time zone) when the offer was updated the last
     * time
     */
    void setLastUpdate(Calendar value);

    /**
     * Gets the latest offer bid price
     */
    double getBid();

    /**
     * Sets the latest offer bid price
     */
    void setBid(double value);

    /**
     * Gets the latest offer ask price
     */
    double getAsk();

    /**
     * Sets the latest offer ask price
     */
    void setAsk(double value);

    /**
     * Gets the offer accumulated last minute volume
     */
    int getMinuteVolume();

    /**
     * Sets the offer accumulated last minute volume
     */
    void setMinuteVolume(int value);

    /**
     * Gets the number of significant digits after decimal point
     */
    int getDigits();

    /**
     * Sets the number of significant digits after decimal point
     */
    void setDigits(int value);

    /**
     * Makes a copy of the offer // / @return
     */
    IOffer clone();
}

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

/**
 * A price bar (candle)
 */
public interface IBar {
    /**
     * Open (the first price) of the time period
     */
    double getOpen();

    /**
     * Set Open (the first price) of the time period
     */
    void setOpen(double value);

    /**
     * High (the greatest price) of the time period
     */
    double getHigh();

    /**
     * Set High (the greatest price) of the time period
     */
    void setHigh(double value);

    /**
     * Low (the smallest price) of the time period
     */
    double getLow();

    /**
     * Set Low (the smallest price) of the time period
     */
    void setLow(double value);

    /**
     * Close (the latest price) of the time period
     */
    double getClose();

    /**
     * Set Close (the latest price) of the time period
     */
    void setClose(double value);
}